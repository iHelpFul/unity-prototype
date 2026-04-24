using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    private sealed class PendingSkillCast
    {
        public PlayerSkillDefinition Definition;
        public EnemyHealth LockedTarget;
        public AttackPayload Payload;
        public int ResolvedSkillLevel;
        public CommittedEnemyHitPacket[] CommittedHitPackets;
    }

    private PlayerCharacter character;
    private Transform visual;
    private PlayerAnimationController animationController;
    private GameBootstrap bootstrap;
    private readonly PlayerHitApplicationService hitApplicationService = new PlayerHitApplicationService();

    private PlayerCombatModule combatModule;
    private ProjectileSystem projectileSystem;
    private PlayerProgressionModule progression;
    private PlayerTargetingService targetingService;
    private PendingSkillCast pendingSkillCast;
    private Coroutine queuedSkillHitsRoutine;

    private LayerMask enemyLayer;
    private float skillFrontDotThreshold;
    private float skillAreaForwardOffsetFactor;
    private float skillAreaRadiusFactor;
    private float skillAreaMinRadius;
    private float lockedSkillTargetGraceRange;
    private float attackLowerHeightAllowance;

    public int ComboIndex => combatModule != null ? combatModule.ComboIndex : 0;
    public bool IsAttacking => combatModule != null && combatModule.IsAttacking;
    public float AttackAnimationSpeed => combatModule != null ? combatModule.AttackAnimationSpeed : 1f;
    public int CurrentComboCounter => combatModule != null ? combatModule.CurrentComboCounter : 0;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        Transform visualTransform,
        PlayerAnimationController playerAnimationController,
        LayerMask targetEnemyLayer,
        float frontDotThreshold,
        float areaForwardOffsetFactor,
        float areaRadiusFactor,
        float areaMinRadius,
        float targetGraceRange,
        float lowerHeightAllowance)
    {
        character = ownerCharacter;
        visual = visualTransform != null ? visualTransform : transform;
        animationController = playerAnimationController;
        enemyLayer = targetEnemyLayer;
        skillFrontDotThreshold = frontDotThreshold;
        skillAreaForwardOffsetFactor = areaForwardOffsetFactor;
        skillAreaRadiusFactor = areaRadiusFactor;
        skillAreaMinRadius = areaMinRadius;
        lockedSkillTargetGraceRange = targetGraceRange;
        attackLowerHeightAllowance = lowerHeightAllowance;

        combatModule ??= new PlayerCombatModule();
        projectileSystem ??= GetComponent<ProjectileSystem>();
        if (projectileSystem == null)
            projectileSystem = gameObject.AddComponent<ProjectileSystem>();

        CreateTargetingService();
        RefreshProgression();
        SyncCombatProfile();
    }

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
        RefreshProgression();
        SyncCombatProfile();
    }

    public void SyncCombatProfile()
    {
        combatModule ??= new PlayerCombatModule();

        if (character != null)
            combatModule.SetBasicAttackProfile(character.GetBasicAttackProfile());
    }

    public void Tick(float deltaTime)
    {
        if (combatModule == null || character == null)
            return;

        bool wasAttacking = combatModule.IsAttacking;
        if (character.IsStunned
            && (character.ActionStateController == null
                || !character.ActionStateController.CanContinueCombatWhileStunned))
        {
            return;
        }

        combatModule.Tick(deltaTime);

        if (wasAttacking
            && !combatModule.IsAttacking)
        {
            if (pendingSkillCast != null)
            {
                CancelPendingSkillState(false);
                return;
            }

            if (queuedSkillHitsRoutine == null)
                CompleteCurrentActionState();
        }
    }

    public void HandleJumpPressed()
    {
        if (combatModule == null || !combatModule.IsAttacking)
            return;

        // Once a skill has started but has not committed yet, jump should not cancel it.
        if (character != null
            && character.ActionStateController != null
            && !character.ActionStateController.CanJumpCancelCurrentAction)
            return;

        if (queuedSkillHitsRoutine != null
            && character != null
            && character.ActionStateController != null
            && character.ActionStateController.IsSkillCommitted)
        {
            combatModule.EndAttack();
            CompleteCurrentActionState();
            return;
        }

        CancelPendingSkillState();
        combatModule.EndAttack();
        CompleteCurrentActionState();
    }

    public void OnHitFrame()
    {
        if (character == null || !character.IsLocalPlayer)
            return;

        if (character.ActionStateController != null
            && !character.ActionStateController.CanProcessAttackHitFrame)
        {
            return;
        }

        if (pendingSkillCast != null)
        {
            ExecutePendingSkillCast();
            return;
        }

        PlayerBasicAttackProfile basicAttackProfile = character.GetBasicAttackProfile();
        PublishPresentationCue(
            basicAttackProfile != null ? basicAttackProfile.PresentationCueSet : null,
            CombatCuePhase.Release,
            character.transform.position);

        if (ShouldUseBasicAttackProjectile(basicAttackProfile))
        {
            SpawnBasicAttackProjectile(basicAttackProfile);
            return;
        }

        bool landedHit = TryHitEnemies();

        if (landedHit)
        {
            GainMomentumFromBasicAttack();
            combatModule?.RegisterSuccessfulBasicHit();
        }
    }

    public void OnComboWindow()
    {
        if (character == null || !character.IsLocalPlayer || combatModule == null)
            return;

        if (character.ActionStateController != null
            && !character.ActionStateController.CanProcessComboWindow)
        {
            return;
        }

        combatModule.OnComboWindow();
    }

    public void OnAttackEnd()
    {
        if (character == null || !character.IsLocalPlayer)
            return;

        pendingSkillCast = null;
        animationController?.ClearSkillAnimationOverride();
        combatModule?.EndAttack();
        CompleteCurrentActionState();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AttackPressedEvent>(OnAttack);
        EventBus.Subscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<PlayerHitEvent>(OnPlayerHit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AttackPressedEvent>(OnAttack);
        EventBus.Unsubscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<PlayerHitEvent>(OnPlayerHit);
        CancelPendingSkillState();
        character?.ActionStateController?.ResetState();
    }

    private void OnAttack(AttackPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character == null
            || character.IsDead
            || character.IsStunned
            || combatModule == null
            || character.ActionStateController == null
            || !character.ActionStateController.CanRequestBasicAttack)
        {
            return;
        }

        bool wasAttacking = combatModule.IsAttacking;
        combatModule.RequestAttack();
        if (!wasAttacking && combatModule.IsAttacking)
        {
            character.ActionStateController.BeginBasicAttack(GetBasicAttackRecoveryDuration());
            PlayerBasicAttackProfile profile = character.GetBasicAttackProfile();
            PublishPresentationCue(
                profile != null ? profile.PresentationCueSet : null,
                CombatCuePhase.CastStart,
                character.transform.position);
        }
    }

    private void OnSkillSlotPressed(SkillSlotPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character == null
            || character.IsDead
            || character.IsStunned
            || combatModule == null
            || character.ActionStateController == null
            || !character.ActionStateController.CanStartSkill)
        {
            return;
        }

        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;
        if (skillSession == null)
            return;

        if (!TryResolveAssignedSkillDefinition(e.SlotIndex, out PlayerSkillDefinition definition))
        {
            NotifySystemMessage($"No skill assigned to slot {e.SlotIndex}.");
            return;
        }

        if (definition.JobType != character.CurrentJob)
        {
            NotifySystemMessage($"{definition.DisplayName} belongs to {GameBootstrap.FormatJobName(definition.JobType)}.");
            return;
        }

        if (definition.SkillType != PlayerSkillType.ActiveAttack)
        {
            NotifySystemMessage($"{definition.DisplayName} is not wired yet.");
            return;
        }

        if (combatModule.IsAttacking)
            return;

        float remainingCooldown = skillSession.GetRemainingSkillCooldown(definition.SkillId);
        if (remainingCooldown > 0f)
        {
            NotifySystemMessage($"{definition.DisplayName} cooldown {remainingCooldown:0.0}s");
            return;
        }

        int resolvedSkillLevel = ResolveSkillLevel(definition.SkillId);
        int resolvedManaCost = definition.GetResolvedManaCost(resolvedSkillLevel);
        float resolvedCooldown = definition.GetResolvedCooldown(resolvedSkillLevel);

        if (!character.HasEnoughMP(resolvedManaCost))
        {
            NotifySystemMessage($"Not enough MP for {definition.DisplayName}.");
            return;
        }

        if (!combatModule.TryStartSkillAttack(definition))
            return;

        if (!character.TrySpendMP(resolvedManaCost))
        {
            animationController?.ClearSkillAnimationOverride();
            combatModule.EndAttack();
            return;
        }

        skillSession.StartSkillCooldown(definition.SkillId, resolvedCooldown);
        CancelPendingSkillState();
        pendingSkillCast = new PendingSkillCast
        {
            Definition = definition,
            LockedTarget = null,
            ResolvedSkillLevel = resolvedSkillLevel
        };
        character.ActionStateController.BeginSkillPreCommit(definition.GetResolvedRecoveryTime(resolvedSkillLevel));
        animationController?.PlaySkillAnimation(definition);
        PublishPresentationCue(definition.PresentationCueSet, CombatCuePhase.CastStart, character.transform.position);
    }

    private bool TryHitEnemies()
    {
        if (combatModule == null || targetingService == null || character == null)
            return false;

        PlayerBasicAttackProfile profile = character.GetBasicAttackProfile();
        AttackPayload payload = BuildBasicAttackPayload();
        if (profile == null || payload == null)
            return false;

        Transform facingTransform = visual != null ? visual : transform;
        float resolvedRange = GetResolvedBasicAttackRange(profile, payload);
        Vector3 origin = GetBasicAttackOrigin(facingTransform, resolvedRange);
        List<EnemyHealth> enemies = targetingService.GetEnemiesInSphere(origin, resolvedRange);
        int maxTargets = combatModule.MaxBasicTargets;
        int hitsApplied = 0;
        float impactDuration = GetBasicAttackImpactDuration(profile);

        foreach (EnemyHealth enemy in enemies)
        {
            if (!targetingService.IsWithinAllowedAttackHeight(enemy, origin.y))
                continue;

            bool landedHit = ApplyHitToEnemy(enemy, impactDuration, true, null, payload);
            if (!landedHit)
                continue;

            hitsApplied++;

            if (profile.TargetingKind == CombatTargetingKind.SingleTarget
                || profile.TargetingKind == CombatTargetingKind.ForwardProjectile
                || hitsApplied >= maxTargets)
            {
                break;
            }
        }

        return hitsApplied > 0;
    }

    private void ExecutePendingSkillCast()
    {
        PendingSkillCast skillCast = pendingSkillCast;
        pendingSkillCast = null;

        if (skillCast?.Definition == null)
        {
            CompleteCurrentActionState();
            return;
        }

        skillCast.Payload ??= BuildSkillPayload(skillCast.Definition, skillCast.ResolvedSkillLevel);
        ConsumeMomentumForSkill(skillCast.Payload);
        character.ActionStateController?.CommitSkill();
        PublishPresentationCue(skillCast.Definition.PresentationCueSet, CombatCuePhase.Release, character.transform.position);

        switch (skillCast.Definition.CombatTargetingKind)
        {
            case CombatTargetingKind.Area:
                if (ExecuteAreaSkillHit(skillCast.Definition, skillCast.ResolvedSkillLevel, skillCast.Payload))
                    GainMomentumFromSkill(skillCast.Definition, skillCast.ResolvedSkillLevel);
                break;

            case CombatTargetingKind.ForwardProjectile:
                if (ExecuteProjectileSkillCast(skillCast.Definition, skillCast.ResolvedSkillLevel, skillCast.Payload))
                    GainMomentumFromSkill(skillCast.Definition, skillCast.ResolvedSkillLevel);
                break;

            default:
                if (ExecuteFrontSingleTargetSkillHit(skillCast))
                    GainMomentumFromSkill(skillCast.Definition, skillCast.ResolvedSkillLevel);
                break;
        }
    }

    private bool ExecuteAreaSkillHit(PlayerSkillDefinition definition, int skillLevel, AttackPayload payload)
    {
        if (targetingService == null)
            return false;

        List<EnemyHealth> targets = targetingService.FindSkillAreaTargets(definition, skillLevel);
        if (targets.Count == 0)
            return false;

        int hitCount = definition.GetResolvedHitCount(skillLevel);
        bool commitDeath = hitCount <= 1;
        ApplyHitsToTargets(targets, definition, payload, 0.06f, commitDeath);

        if (hitCount > 1)
        {
            CancelQueuedSkillHits(false);
            queuedSkillHitsRoutine = StartCoroutine(PerformRepeatedAreaSkillHits(definition, skillLevel, hitCount - 1, payload));
        }

        return true;
    }

    private bool ExecuteProjectileSkillCast(PlayerSkillDefinition definition, int skillLevel, AttackPayload payload)
    {
        if (definition == null || targetingService == null)
            return false;

        CancelQueuedSkillHits(false);

        if (definition.ProjectileBehaviorKind != ProjectileBehaviorKind.SequenceLocked)
            return ExecuteFreeProjectileSkillCast(definition, skillLevel, payload);

        EnemyHealth lockedTarget = targetingService.FindFrontSingleTarget(definition, skillLevel);
        int projectileCount = definition.GetResolvedProjectileCount(skillLevel);
        float projectileInterval = GetResolvedProjectileShotInterval(definition, skillLevel);
        CommittedEnemyHitPacket[] committedPackets = lockedTarget != null
            ? BuildCommittedHitPackets(payload, lockedTarget, projectileCount, PlayerSkillProjectileImpactDuration, projectileInterval)
            : null;

        SpawnProjectileShot(
            definition,
            skillLevel,
            lockedTarget,
            0,
            projectileCount,
            payload,
            GetCommittedHitPacket(committedPackets, 0));

        if (projectileCount <= 1)
            return true;

        queuedSkillHitsRoutine = StartCoroutine(PerformQueuedProjectileShots(
            definition,
            skillLevel,
            lockedTarget,
            projectileCount,
            payload,
            committedPackets));

        return true;
    }

    private bool ExecuteFreeProjectileSkillCast(PlayerSkillDefinition definition, int skillLevel, AttackPayload payload)
    {
        if (definition == null || character == null)
            return false;

        int projectileCount = definition.GetResolvedProjectileCount(skillLevel);
        if (projectileCount <= 0)
            return false;

        SpawnProjectileShot(
            definition,
            skillLevel,
            null,
            0,
            projectileCount,
            payload,
            null);

        if (projectileCount <= 1)
            return true;

        queuedSkillHitsRoutine = StartCoroutine(PerformQueuedProjectileShots(
            definition,
            skillLevel,
            null,
            projectileCount,
            payload,
            null));

        return true;
    }

    private IEnumerator PerformQueuedProjectileShots(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth lockedTarget,
        int projectileCount,
        AttackPayload payload,
        CommittedEnemyHitPacket[] committedPackets)
    {
        float interval = GetResolvedProjectileShotInterval(definition, skillLevel);

        for (int projectileIndex = 1; projectileIndex < projectileCount; projectileIndex++)
        {
            yield return new WaitForSeconds(interval);

            if (character == null || character.IsDead)
                break;

            SpawnProjectileShot(
                definition,
                skillLevel,
                lockedTarget,
                projectileIndex,
                projectileCount,
                payload,
                GetCommittedHitPacket(committedPackets, projectileIndex));
        }

        queuedSkillHitsRoutine = null;
        CompleteCurrentActionState();
    }

    private void SpawnProjectileShot(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth lockedTarget,
        int projectileIndex,
        int projectileCount,
        AttackPayload payload,
        CommittedEnemyHitPacket? committedHitPacket)
    {
        Transform facingTransform = visual != null ? visual : transform;
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

    private bool ExecuteFrontSingleTargetSkillHit(PendingSkillCast skillCast)
    {
        if (targetingService == null)
            return false;

        EnemyHealth target = targetingService.ResolveLockedSkillTarget(
            skillCast.Definition,
            skillCast.LockedTarget,
            true,
            skillCast.ResolvedSkillLevel);
        if (target == null)
            return false;

        if (TryExecuteAuthoritativeSkillSequence(skillCast.Definition, skillCast.ResolvedSkillLevel, target))
        {
            skillCast.LockedTarget = target;
            return true;
        }

        int totalHits = Mathf.Max(1, skillCast.Definition.GetResolvedHitCount(skillCast.ResolvedSkillLevel));
        float hitInterval = Mathf.Max(0.01f, skillCast.Definition.GetResolvedHitInterval(skillCast.ResolvedSkillLevel));
        skillCast.CommittedHitPackets ??= BuildCommittedHitPackets(
            skillCast.Payload,
            target,
            totalHits,
            DirectSkillImpactDuration,
            hitInterval);
        ApplyCommittedHitPacket(target, skillCast.Definition, skillCast.CommittedHitPackets, 0);
        skillCast.LockedTarget = target;

        int remainingHits = Mathf.Max(0, totalHits - 1);
        if (remainingHits <= 0)
            return true;

        CancelQueuedSkillHits(false);
        queuedSkillHitsRoutine = StartCoroutine(PerformRepeatedSingleTargetSkillHits(
            skillCast.Definition,
            skillCast.ResolvedSkillLevel,
            target,
            remainingHits,
            skillCast.CommittedHitPackets));

        return true;
    }

    private IEnumerator PerformRepeatedSingleTargetSkillHits(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth initialTarget,
        int remainingHits,
        CommittedEnemyHitPacket[] committedPackets)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.GetResolvedHitInterval(skillLevel)));

            if (character == null || character.IsDead || targetingService == null)
                break;

            EnemyHealth target = targetingService.ResolveLockedSkillTarget(definition, initialTarget, false, skillLevel);
            if (target == null)
                break;

            ApplyCommittedHitPacket(target, definition, committedPackets, hitIndex + 1);
        }

        queuedSkillHitsRoutine = null;
        CompleteCurrentActionState();
    }

    private bool TryExecuteAuthoritativeSkillSequence(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth target)
    {
        if (!SupportsAuthoritativeSkillSequence(definition, skillLevel) || target == null || character == null)
            return false;

        float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
        return MultiplayerPrototypeEnemyCoordinator.TryRequestSkillSequence(
            target,
            direction,
            character,
            definition.SkillId);
    }

    private IEnumerator PerformRepeatedAreaSkillHits(PlayerSkillDefinition definition, int skillLevel, int remainingHits, AttackPayload payload)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.GetResolvedHitInterval(skillLevel)));

            if (character == null || character.IsDead || targetingService == null)
                break;

            List<EnemyHealth> targets = targetingService.FindSkillAreaTargets(definition, skillLevel);
            if (targets.Count == 0)
                break;

            bool commitDeath = hitIndex >= remainingHits - 1;
            ApplyHitsToTargets(targets, definition, payload, 0.045f, commitDeath);
        }

        queuedSkillHitsRoutine = null;
        CompleteCurrentActionState();
    }

    private void ApplyHitsToTargets(
        List<EnemyHealth> targets,
        PlayerSkillDefinition definition,
        AttackPayload payload,
        float impactDuration = 0.06f,
        bool commitDeath = true)
    {
        foreach (EnemyHealth target in targets)
            ApplyHitToEnemy(target, impactDuration, commitDeath, definition, payload);
    }

    private int CalculateSkillDamage(PlayerSkillDefinition definition)
    {
        AttackPayload payload = BuildSkillPayload(definition);
        return payload != null
            ? AttackPayloadBuilder.RollResolvedDamage(payload)
            : 1;
    }

    private bool ApplyHitToEnemy(
        EnemyHealth enemy,
        float impactDuration,
        bool commitDeath = true,
        PlayerSkillDefinition skillDefinition = null,
        AttackPayload payload = null)
    {
        if (character == null)
            return false;

        return hitApplicationService.ApplyHit(
            enemy,
            character,
            transform.position.x,
            0f,
            ResolveLocalFallbackDamage(skillDefinition),
            impactDuration,
            commitDeath,
            skillDefinition != null ? skillDefinition.SkillId : string.Empty,
            payload);
    }

    private void SpawnSkillProjectile(
        PlayerSkillDefinition definition,
        int skillLevel,
        Vector3 spawnPosition,
        Vector3 direction,
        EnemyHealth lockedTarget,
        bool commitDeathOnHit,
        AttackPayload payload,
        CommittedEnemyHitPacket? committedHitPacket = null)
    {
        if (definition == null || character == null)
            return;

        int resolvedDamage = payload != null
            ? AttackPayloadBuilder.RollResolvedDamage(payload)
            : CalculateSkillDamage(definition);
        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        ProjectileProfile projectileProfile = definition.GetResolvedProjectileProfile(skillLevel);
        PresentationCueSet projectileCueSet = ResolveProjectileCueSet(projectileProfile, definition.PresentationCueSet);
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
            definition.GetResolvedProjectileRadius(skillLevel),
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
            definition.ProjectileBehaviorKind,
            ProjectileProfileUtility.ResolveImpactAreaRadius(projectileProfile),
            ProjectileProfileUtility.ResolveMaxImpactAreaTargets(projectileProfile, payload),
            projectileCueSet);
    }

    private int ResolveLocalFallbackDamage(PlayerSkillDefinition skillDefinition)
    {
        if (skillDefinition != null)
        {
            AttackPayload payload = BuildSkillPayload(skillDefinition);
            if (payload != null)
                return AttackPayloadBuilder.RollResolvedDamage(payload);
        }

        if (combatModule == null || character == null)
            return 1;

        return Mathf.Max(1, combatModule.CalculateBasicDamage(character.GetCombatSnapshot()));
    }

    private AttackPayload BuildBasicAttackPayload()
    {
        if (character == null || combatModule == null)
            return null;

        PlayerBasicAttackProfile profile = character.GetBasicAttackProfile();
        if (profile == null)
            return null;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        return AttackPayloadBuilder.BuildBasicAttackPayload(
            character,
            snapshot,
            profile,
            combatModule.CurrentComboCounter);
    }

    private bool ShouldUseBasicAttackProjectile(PlayerBasicAttackProfile profile)
    {
        if (profile == null)
            return false;

        return profile.ExecutionKind == CombatExecutionKind.Projectile
            || profile.ExecutionKind == CombatExecutionKind.MagicProjectile
            || profile.TargetingKind == CombatTargetingKind.ForwardProjectile;
    }

    private void SpawnBasicAttackProjectile(PlayerBasicAttackProfile profile)
    {
        if (profile == null || character == null)
            return;

        AttackPayload payload = BuildBasicAttackPayload();
        if (payload == null)
            return;

        ProjectileProfile projectileProfile = profile.DefaultProjectileProfile;
        PresentationCueSet projectileCueSet = ResolveProjectileCueSet(projectileProfile, profile.PresentationCueSet);
        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        Transform facingTransform = visual != null ? visual : transform;
        Vector3 direction = facingTransform.forward.sqrMagnitude > 0.0001f
            ? facingTransform.forward.normalized
            : transform.forward;
        Vector3 spawnPosition = facingTransform.position
            + direction * ProjectileProfileUtility.ResolveSpawnForwardOffset(projectileProfile)
            + Vector3.up * ProjectileProfileUtility.ResolveSpawnUpOffset(projectileProfile);
        float resolvedProjectileSpeed = ProjectileProfileUtility.ResolveSpeed(projectileProfile);
        if (snapshot.ProjectileSpeedModifier > 0f)
            resolvedProjectileSpeed *= snapshot.ProjectileSpeedModifier;

        SpawnCombatProjectile(
            payload.ActionId,
            projectileProfile,
            spawnPosition,
            direction,
            null,
            true,
            payload,
            null,
            resolvedProjectileSpeed,
            ProjectileProfileUtility.ResolveRadius(projectileProfile),
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
            ProjectileBehaviorKind.Free,
            ProjectileProfileUtility.ResolveImpactAreaRadius(projectileProfile),
            ProjectileProfileUtility.ResolveMaxImpactAreaTargets(projectileProfile, payload),
            projectileCueSet);
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
        float hitRadius,
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
        ProjectileBehaviorKind projectileBehaviorKind,
        float impactAreaRadius,
        int maxImpactAreaTargets,
        PresentationCueSet presentationCueSet)
    {
        GameObject projectilePrefab = ResolveProjectilePrefab(projectileProfile);
        projectileSystem ??= GetComponent<ProjectileSystem>();
        if (projectileSystem == null)
            projectileSystem = gameObject.AddComponent<ProjectileSystem>();

        projectileSystem.Launch(new ProjectileLaunchRequest
        {
            ActionId = actionId,
            Owner = character,
            PresentationSource = character != null ? character.transform : transform,
            ProjectilePrefab = projectilePrefab,
            LockedTarget = lockedTarget,
            EnemyLayer = enemyLayer,
            ExplicitDamage = explicitDamage,
            SpawnPosition = spawnPosition,
            Direction = direction,
            TravelSpeed = travelSpeed,
            HitRadius = hitRadius,
            MaxLifetime = maxLifetime,
            ResolvedTravelDistance = resolvedTravelDistance,
            VisualScale = visualScale,
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
            BehaviorKind = projectileBehaviorKind,
            ImpactAreaRadius = impactAreaRadius,
            MaxImpactAreaTargets = maxImpactAreaTargets,
            PresentationCueSet = presentationCueSet
        });
    }

    private static bool SupportsAuthoritativeSkillSequence(PlayerSkillDefinition definition, int skillLevel)
    {
        return MultiplayerPrototypeRuntime.IsEnabled
            && definition != null
            && definition.GetResolvedHitCount(skillLevel) > 1
            && definition.CombatTargetingKind == CombatTargetingKind.SingleTarget;
    }

    private void PublishPresentationCue(
        PresentationCueSet cueSet,
        CombatCuePhase phase,
        Vector3 worldPosition,
        Transform target = null)
    {
        if (cueSet == null || character == null)
            return;

        CombatPresentationDispatcher.PublishCuePhase(
            cueSet,
            phase,
            worldPosition,
            character.transform,
            target);
    }

    private static PresentationCueSet ResolveProjectileCueSet(
        ProjectileProfile projectileProfile,
        PresentationCueSet fallbackCueSet)
    {
        return projectileProfile != null && projectileProfile.PresentationCueSet != null
            ? projectileProfile.PresentationCueSet
            : fallbackCueSet;
    }

    private GameObject ResolveProjectilePrefab(ProjectileProfile projectileProfile)
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

    private void NotifySystemMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EventBus.Publish(new GameplayNotificationEvent
        {
            Target = character,
            CharacterId = character != null ? character.CharacterId : string.Empty,
            Category = GameplayNotificationCategory.System,
            Message = message
        });
    }

    private void CancelPendingSkillState(bool cancelQueuedHits = true)
    {
        pendingSkillCast = null;
        animationController?.ClearSkillAnimationOverride();

        if (cancelQueuedHits)
            CancelQueuedSkillHits();

        if (character != null
            && character.ActionStateController != null
            && character.ActionStateController.HasPendingSkillCommit)
        {
            character.ActionStateController.ResetState();
        }
    }

    private void CancelQueuedSkillHits(bool clearReference = true)
    {
        if (queuedSkillHitsRoutine != null)
            StopCoroutine(queuedSkillHitsRoutine);

        if (clearReference)
            queuedSkillHitsRoutine = null;
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (character == null || e.Killer != character)
            return;

        progression?.AddExp(e.ExpReward);
    }

    private void OnPlayerHit(PlayerHitEvent e)
    {
        if (character == null
            || !PlayerRuntimeIdentityUtility.MatchesCharacter(
                character,
                character.CharacterId,
                e.Target,
                e.CharacterId))
        {
            return;
        }

        if (character.ActionStateController == null || !character.ActionStateController.CanBeInterruptedByHit)
            return;

        character.ActionStateController.InterruptCurrentAction();
    }

    private bool MatchesInputPlayer(PlayerCharacter player, string characterId)
    {
        return character != null
            && PlayerRuntimeIdentityUtility.MatchesCharacter(
                character,
                character.CharacterId,
                player,
                characterId);
    }

    private bool TryResolveAssignedSkillDefinition(int slotIndex, out PlayerSkillDefinition definition)
    {
        definition = null;

        if (character == null)
            return false;

        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;
        definition = skillSession != null ? skillSession.GetAssignedSkillDefinition(slotIndex) : null;
        if (definition != null)
            return true;

        if (skillSession == null)
            return false;

        IReadOnlyList<PlayerSkillDefinition> defaultSkills =
            PlayerJobCombatProfiles.GetDefaultSkillsForJob(character.CurrentJob);

        for (int index = 0; index < defaultSkills.Count; index++)
        {
            PlayerSkillDefinition fallbackDefinition = defaultSkills[index];
            if (fallbackDefinition == null)
                continue;

            if (fallbackDefinition.DefaultSlotIndex != slotIndex)
                continue;

            if (fallbackDefinition.SkillType != PlayerSkillType.ActiveAttack)
                continue;

            skillSession.TryUnlockSkill(fallbackDefinition.SkillId, 1);
            skillSession.TryAssignSkillToSlot(fallbackDefinition.SkillId, slotIndex);
            definition = skillSession.GetAssignedSkillDefinition(slotIndex);

            if (definition != null)
            {
                NotifySystemMessage($"{definition.DisplayName} assigned to slot {slotIndex}.");
                return true;
            }
        }

        return false;
    }

    private void CreateTargetingService()
    {
        targetingService = new PlayerTargetingService(
            transform,
            visual,
            enemyLayer,
            skillFrontDotThreshold,
            skillAreaForwardOffsetFactor,
            skillAreaRadiusFactor,
            skillAreaMinRadius,
            lockedSkillTargetGraceRange,
            attackLowerHeightAllowance);
    }

    private void RefreshProgression()
    {
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        PlayerRuntimeData data = character != null && character.IsLocalPlayer && characterSession != null
            ? characterSession.PlayerData
            : null;

        progression = new PlayerProgressionModule(data, character, bootstrap);
    }

    private AttackPayload BuildSkillPayload(PlayerSkillDefinition definition, int skillLevel = -1)
    {
        if (definition == null || character == null)
            return null;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        if (skillLevel <= 0)
            skillLevel = ResolveSkillLevel(definition.SkillId);

        return AttackPayloadBuilder.BuildSkillPayload(character, snapshot, definition, skillLevel);
    }

    private static readonly float DirectSkillImpactDuration = 0.06f;
    private static readonly float PlayerSkillProjectileImpactDuration = 0.04f;

    private static CommittedEnemyHitPacket[] BuildCommittedHitPackets(
        AttackPayload payload,
        EnemyHealth target,
        int packetCount,
        float impactDuration,
        float packetInterval)
    {
        if (payload == null || target == null)
            return null;

        return CombatResolver.ResolveCommittedSequenceAgainstEnemy(
            payload,
            target,
            Mathf.Max(1, packetCount),
            impactDuration,
            packetInterval,
            ResolveEnemyReactionLockTail(impactDuration));
    }

    private static float ResolveEnemyReactionLockTail(float impactDuration)
    {
        return Mathf.Max(0.14f, impactDuration * 2f);
    }

    private static float GetResolvedProjectileShotInterval(PlayerSkillDefinition definition, int skillLevel)
    {
        if (definition == null)
            return 0.08f;

        return Mathf.Max(0.04f, definition.GetResolvedHitInterval(skillLevel) > 0f
            ? definition.GetResolvedHitInterval(skillLevel)
            : 0.08f);
    }

    private static CommittedEnemyHitPacket? GetCommittedHitPacket(
        CommittedEnemyHitPacket[] committedPackets,
        int index)
    {
        if (committedPackets == null || index < 0 || index >= committedPackets.Length)
            return null;

        return committedPackets[index];
    }

    private bool ApplyCommittedHitPacket(
        EnemyHealth target,
        PlayerSkillDefinition definition,
        CommittedEnemyHitPacket[] committedPackets,
        int packetIndex)
    {
        if (character == null || target == null || committedPackets == null)
            return false;

        if (packetIndex < 0 || packetIndex >= committedPackets.Length)
            return false;

        return hitApplicationService.ApplyCommittedHit(
            target,
            character,
            transform.position.x,
            0f,
            committedPackets[packetIndex],
            definition != null ? definition.PresentationCueSet : null);
    }

    private void GainMomentumFromBasicAttack()
    {
        if (character == null)
            return;

        PlayerBasicAttackProfile profile = character.GetBasicAttackProfile();
        if (profile == null)
            return;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int totalMomentumGain = Mathf.Max(0, profile.MomentumGainOnValidHit + snapshot.MomentumGainBonus);
        character.GainCombatMomentum(totalMomentumGain);
    }

    private void GainMomentumFromSkill(PlayerSkillDefinition definition, int skillLevel)
    {
        if (character == null || definition == null)
            return;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int totalMomentumGain = Mathf.Max(0, definition.GetResolvedMomentumGain(skillLevel) + snapshot.MomentumGainBonus);
        character.GainCombatMomentum(totalMomentumGain);
    }

    private void ConsumeMomentumForSkill(AttackPayload payload)
    {
        if (character == null || payload == null)
            return;

        character.ConsumeCombatMomentum(payload.MomentumToConsume);
    }

    private int ResolveSkillLevel(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return 1;

        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;
        if (skillSession == null)
            return 1;

        IReadOnlyList<PlayerSkillEntry> unlockedSkills = skillSession.GetUnlockedSkills();
        for (int index = 0; index < unlockedSkills.Count; index++)
        {
            PlayerSkillEntry skillEntry = unlockedSkills[index];
            if (skillEntry == null || skillEntry.SkillId != skillId)
                continue;

            return Mathf.Max(1, skillEntry.SkillLevel);
        }

        return 1;
    }

    private void CompleteCurrentActionState()
    {
        if (character == null
            || character.ActionStateController == null
            || pendingSkillCast != null
            || queuedSkillHitsRoutine != null)
        {
            return;
        }

        character.ActionStateController.CompleteCurrentAction();
    }

    private float GetBasicAttackRecoveryDuration()
    {
        PlayerBasicAttackProfile profile = character != null ? character.GetBasicAttackProfile() : null;
        return profile != null ? profile.RecoveryTime : 0f;
    }

    private float GetResolvedBasicAttackRange(PlayerBasicAttackProfile profile, AttackPayload payload)
    {
        if (payload != null && payload.ResolvedRange > 0f)
            return payload.ResolvedRange;

        if (profile != null && profile.BaseRange > 0f)
            return profile.BaseRange;

        return 0.1f;
    }

    private static Vector3 GetBasicAttackOrigin(Transform facingTransform, float resolvedRange)
    {
        float forwardOffset = Mathf.Clamp(resolvedRange * 0.5f, 0.35f, 1f);
        return facingTransform.position + facingTransform.forward * forwardOffset;
    }

    private static float GetBasicAttackImpactDuration(PlayerBasicAttackProfile profile)
    {
        if (profile == null)
            return 0.05f;

        if (profile.ActiveTime > 0f)
            return Mathf.Clamp(profile.ActiveTime * 0.5f, 0.03f, 0.12f);

        return 0.05f;
    }

}
