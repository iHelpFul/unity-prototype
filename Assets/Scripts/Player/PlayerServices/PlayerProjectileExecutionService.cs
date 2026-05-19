using UnityEngine;

public sealed class PlayerProjectileExecutionService
{
    private PlayerCharacter owner;
    private Transform ownerTransform;
    private Transform visualTransform;
    private PlayerTargetingService targetingService;
    private ProjectileSystem projectileSystem;
    private LayerMask enemyLayer;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        Transform sourceTransform,
        Transform resolvedVisualTransform,
        PlayerTargetingService resolvedTargetingService,
        ProjectileSystem resolvedProjectileSystem,
        LayerMask resolvedEnemyLayer)
    {
        owner = ownerCharacter;
        ownerTransform = sourceTransform != null ? sourceTransform : ownerCharacter != null ? ownerCharacter.transform : null;
        visualTransform = resolvedVisualTransform != null ? resolvedVisualTransform : ownerTransform;
        targetingService = resolvedTargetingService;
        projectileSystem = resolvedProjectileSystem;
        enemyLayer = resolvedEnemyLayer;
    }

    public float GetResolvedProjectileShotInterval(PlayerSkillDefinition definition, int skillLevel)
    {
        if (definition == null)
            return 0.08f;

        return Mathf.Max(0.04f, definition.GetResolvedHitInterval(skillLevel) > 0f
            ? definition.GetResolvedHitInterval(skillLevel)
            : 0.08f);
    }

    public void SpawnProjectileShot(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth lockedTarget,
        int projectileIndex,
        int projectileCount,
        AttackPayload payload,
        CommittedEnemyHitPacket? committedHitPacket)
    {
        if (definition == null)
            return;

        Transform facingTransform = visualTransform != null ? visualTransform : ownerTransform;
        if (facingTransform == null)
            return;

        float spreadAngle = Mathf.Max(0f, definition.ProjectileSpreadAngle);
        float lateralSpacing = projectileCount > 1 ? 0.16f : 0f;
        Vector3 spawnBasePosition = facingTransform.position
            + facingTransform.forward * Mathf.Max(0f, definition.GetResolvedProjectileSpawnForwardOffset(skillLevel))
            + Vector3.up * definition.GetResolvedProjectileSpawnUpOffset(skillLevel);

        float normalizedIndex = projectileCount == 1
            ? 0.5f
            : projectileIndex / (float)(projectileCount - 1);

        float yawOffset = projectileCount == 1
            ? 0f
            : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, normalizedIndex);

        float lateralOffset = projectileCount == 1
            ? 0f
            : Mathf.Lerp(-lateralSpacing * 0.5f, lateralSpacing * 0.5f, normalizedIndex);

        Vector3 spawnPosition = spawnBasePosition + facingTransform.right * lateralOffset;
        Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * facingTransform.forward;

        if (lockedTarget != null && targetingService != null)
        {
            Vector3 targetDirection = targetingService.GetEnemyTargetPoint(lockedTarget) - spawnPosition;
            if (targetDirection.sqrMagnitude > 0.0001f)
                direction = targetDirection.normalized;
        }

        bool commitDeathOnHit = projectileIndex >= projectileCount - 1;
        SpawnSkillProjectile(
            definition,
            skillLevel,
            spawnPosition,
            direction,
            lockedTarget,
            commitDeathOnHit,
            payload,
            committedHitPacket);
    }

    public void SpawnSkillProjectile(
        PlayerSkillDefinition definition,
        int skillLevel,
        Vector3 spawnPosition,
        Vector3 direction,
        EnemyHealth lockedTarget,
        bool commitDeathOnHit,
        AttackPayload payload,
        CommittedEnemyHitPacket? committedHitPacket = null)
    {
        if (definition == null || owner == null)
            return;

        int resolvedDamage = payload != null
            ? AttackPayloadBuilder.RollResolvedDamage(payload)
            : 1;
        PlayerCombatSnapshot snapshot = owner.GetCombatSnapshot();
        ProjectileProfile projectileProfile = definition.GetResolvedProjectileProfile(skillLevel);
        PresentationCueSet projectileCueSet = ResolveProjectileCueSet(
            projectileProfile,
            definition.ResolvePresentationCueSet(payload != null ? payload.ActionKind : AttackPayloadActionKind.Skill));
        float resolvedProjectileSpeed = definition.GetResolvedProjectileSpeed(skillLevel);
        if (snapshot.ProjectileSpeedModifier > 0f)
            resolvedProjectileSpeed *= snapshot.ProjectileSpeedModifier;

        SpawnCombatProjectile(
            definition.SkillId,
            projectileProfile,
            spawnPosition,
            direction,
            lockedTarget,
            commitDeathOnHit,
            payload,
            committedHitPacket,
            resolvedProjectileSpeed,
            definition.GetResolvedProjectileHitBoxRange(skillLevel),
            definition.GetResolvedProjectileHitBox(skillLevel),
            definition.GetResolvedProjectileLifetime(skillLevel),
            payload != null ? Mathf.Max(0f, payload.ResolvedRange) : definition.GetResolvedRange(skillLevel),
            definition.GetResolvedProjectileVisualScale(skillLevel),
            MultiplayerPrototypeRuntime.IsEnabled ? 0 : resolvedDamage,
            ProjectileProfileUtility.ResolveTravelStyle(projectileProfile),
            ProjectileProfileUtility.ResolveHitMode(projectileProfile),
            ProjectileProfileUtility.ResolveMaxTargets(projectileProfile, payload),
            ProjectileProfileUtility.ResolveStopOnFirstHit(projectileProfile, payload),
            ProjectileProfileUtility.ResolveArcHeight(projectileProfile),
            ProjectileProfileUtility.ResolveHomingRadius(projectileProfile),
            ProjectileProfileUtility.ResolveHomingTurnRate(projectileProfile),
            ProjectileProfileUtility.ResolveImpactAreaRadius(projectileProfile),
            ProjectileProfileUtility.ResolveMaxImpactAreaTargets(projectileProfile, payload),
            projectileCueSet);
    }

    public void SpawnBasicAttackProjectile(
        PlayerBasicAttackProfile profile,
        AttackPayload payload,
        System.Action onFirstSuccessfulHit = null)
    {
        if (profile == null || owner == null || payload == null)
            return;

        ProjectileProfile projectileProfile = profile.DefaultProjectileProfile;
        PresentationCueSet projectileCueSet = ResolveProjectileCueSet(projectileProfile, profile.PresentationCueSet);
        PlayerCombatSnapshot snapshot = owner.GetCombatSnapshot();
        Transform facingTransform = visualTransform != null ? visualTransform : ownerTransform;
        if (facingTransform == null)
            return;

        EnemyHealth lockedTarget = null;
        if (profile.ProjectileLaunchMode == ProjectileLaunchMode.Locked && targetingService != null)
        {
            lockedTarget = targetingService.ResolveLockedTargetInHitBox(
                payload.ResolvedHitBox,
                payload.ResolvedRange,
                null,
                true);
        }

        Vector3 direction = facingTransform.forward.sqrMagnitude > 0.0001f
            ? facingTransform.forward.normalized
            : ownerTransform != null ? ownerTransform.forward : Vector3.forward;
        Vector3 spawnPosition = facingTransform.position
            + direction * ProjectileProfileUtility.ResolveSpawnForwardOffset(projectileProfile)
            + Vector3.up * ProjectileProfileUtility.ResolveSpawnUpOffset(projectileProfile);
        if (lockedTarget != null && targetingService != null)
        {
            Vector3 targetDirection = targetingService.GetEnemyTargetPoint(lockedTarget) - spawnPosition;
            if (targetDirection.sqrMagnitude > 0.0001f)
                direction = targetDirection.normalized;
        }
        float resolvedProjectileSpeed = ProjectileProfileUtility.ResolveSpeed(projectileProfile);
        if (snapshot.ProjectileSpeedModifier > 0f)
            resolvedProjectileSpeed *= snapshot.ProjectileSpeedModifier;

        SpawnCombatProjectile(
            payload.ActionId,
            projectileProfile,
            spawnPosition,
            direction,
            lockedTarget,
            true,
            payload,
            null,
            resolvedProjectileSpeed,
            ProjectileProfileUtility.ResolveCollisionRange(projectileProfile),
            ProjectileProfileUtility.ResolveCollisionHitBox(projectileProfile),
            ProjectileProfileUtility.ResolveLifetime(projectileProfile, payload, resolvedProjectileSpeed),
            Mathf.Max(0f, payload.ResolvedRange),
            ProjectileProfileUtility.ResolveVisualScale(projectileProfile),
            0,
            ProjectileProfileUtility.ResolveTravelStyle(projectileProfile),
            ProjectileProfileUtility.ResolveHitMode(projectileProfile),
            ProjectileProfileUtility.ResolveMaxTargets(projectileProfile, payload),
            ProjectileProfileUtility.ResolveStopOnFirstHit(projectileProfile, payload),
            ProjectileProfileUtility.ResolveArcHeight(projectileProfile),
            ProjectileProfileUtility.ResolveHomingRadius(projectileProfile),
            ProjectileProfileUtility.ResolveHomingTurnRate(projectileProfile),
            ProjectileProfileUtility.ResolveImpactAreaRadius(projectileProfile),
            ProjectileProfileUtility.ResolveMaxImpactAreaTargets(projectileProfile, payload),
            projectileCueSet,
            onFirstSuccessfulHit);
    }

    private static PresentationCueSet ResolveProjectileCueSet(
        ProjectileProfile projectileProfile,
        PresentationCueSet fallbackCueSet)
    {
        return projectileProfile != null && projectileProfile.PresentationCueSet != null
            ? projectileProfile.PresentationCueSet
            : fallbackCueSet;
    }

    private void SpawnCombatProjectile(
        string actionId,
        ProjectileProfile projectileProfile,
        Vector3 spawnPosition,
        Vector3 direction,
        EnemyHealth lockedTarget,
        bool commitDeathOnHit,
        AttackPayload payload,
        CommittedEnemyHitPacket? committedHitPacket,
        float travelSpeed,
        float collisionRange,
        CombatHitBoxDefinition collisionHitBox,
        float maxLifetime,
        float resolvedTravelDistance,
        float visualScale,
        int explicitDamage,
        ProjectileTravelStyle projectileTravelStyle,
        ProjectileHitMode projectileHitMode,
        int maxTargets,
        bool stopOnFirstValidHit,
        float arcHeight,
        float homingRadius,
        float homingTurnRate,
        float impactAreaRadius,
        int maxImpactAreaTargets,
        PresentationCueSet presentationCueSet,
        System.Action onFirstSuccessfulHit = null)
    {
        if (projectileSystem == null)
            return;

        projectileSystem.Launch(new ProjectileLaunchRequest
        {
            ActionId = actionId,
            Owner = owner,
            PresentationSource = owner != null ? owner.transform : ownerTransform,
            ProjectilePrefab = ResolveProjectilePrefab(projectileProfile),
            LockedTarget = lockedTarget,
            EnemyLayer = enemyLayer,
            ExplicitDamage = explicitDamage,
            SpawnPosition = spawnPosition,
            Direction = direction,
            TravelSpeed = travelSpeed,
            CollisionRange = collisionRange,
            CollisionHitBox = collisionHitBox,
            MaxLifetime = maxLifetime,
            ResolvedTravelDistance = resolvedTravelDistance,
            VisualScale = visualScale,
            VisualRotationMode = ProjectileProfileUtility.ResolveVisualRotationMode(projectileProfile),
            InvertVisualRotationOffsetWhenFacingOppositeSide = ProjectileProfileUtility.ResolveInvertVisualRotationOffsetWhenFacingOppositeSide(projectileProfile),
            VisualRotationOffsetEuler = ProjectileProfileUtility.ResolveVisualRotationOffsetEuler(projectileProfile),
            CommitDeathOnHit = commitDeathOnHit,
            CommittedHitPacket = committedHitPacket,
            Payload = payload,
            TravelStyle = projectileTravelStyle,
            HitMode = projectileHitMode,
            MaxTargets = maxTargets,
            StopOnFirstValidHit = stopOnFirstValidHit,
            ArcHeight = arcHeight,
            HomingRadius = homingRadius,
            HomingTurnRate = homingTurnRate,
            ImpactAreaRadius = impactAreaRadius,
            MaxImpactAreaTargets = maxImpactAreaTargets,
            PresentationCueSet = presentationCueSet,
            OnFirstSuccessfulHit = onFirstSuccessfulHit
        });
    }

    private static GameObject ResolveProjectilePrefab(ProjectileProfile projectileProfile)
    {
        if (projectileProfile != null && projectileProfile.ProjectilePrefab != null)
            return projectileProfile.ProjectilePrefab;

        if (projectileProfile != null)
        {
            Debug.LogWarning(
                $"ProjectileProfile '{projectileProfile.name}' does not have a Projectile Prefab assigned. " +
                "ProjectileSystem will fall back to a runtime-only projectile object.",
                projectileProfile);
        }

        return null;
    }
}
