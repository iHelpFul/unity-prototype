using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    private sealed class PendingSkillCast
    {
        public PlayerSkillDefinition Definition;
        public EnemyHealth LockedTarget;
    }

    private PlayerCharacter character;
    private Transform visual;
    private PlayerAnimationController animationController;
    private GameBootstrap bootstrap;
    private readonly PlayerHitApplicationService hitApplicationService = new PlayerHitApplicationService();

    private PlayerCombatModule combatModule;
    private PlayerProgressionModule progression;
    private PlayerTargetingService targetingService;
    private PendingSkillCast pendingSkillCast;
    private Coroutine queuedSkillHitsRoutine;

    private float attackRange;
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
        float basicAttackRange,
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
        attackRange = basicAttackRange;
        enemyLayer = targetEnemyLayer;
        skillFrontDotThreshold = frontDotThreshold;
        skillAreaForwardOffsetFactor = areaForwardOffsetFactor;
        skillAreaRadiusFactor = areaRadiusFactor;
        skillAreaMinRadius = areaMinRadius;
        lockedSkillTargetGraceRange = targetGraceRange;
        attackLowerHeightAllowance = lowerHeightAllowance;

        combatModule ??= new PlayerCombatModule();
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
        if (combatModule == null || character == null || character.IsStunned)
            return;

        combatModule.Tick(deltaTime);
    }

    public void HandleJumpPressed()
    {
        if (combatModule == null || !combatModule.IsAttacking)
            return;

        CancelPendingSkillState();
        combatModule.EndAttack();
    }

    public void OnHitFrame()
    {
        if (character == null || !character.IsLocalPlayer)
            return;

        if (pendingSkillCast != null)
        {
            ExecutePendingSkillCast();
            return;
        }

        bool landedHit = TryHitEnemies();

        if (landedHit)
            combatModule?.RegisterSuccessfulBasicHit();
    }

    public void OnComboWindow()
    {
        if (character == null || !character.IsLocalPlayer || combatModule == null)
            return;

        combatModule.OnComboWindow();
    }

    public void OnAttackEnd()
    {
        if (character == null || !character.IsLocalPlayer)
            return;

        pendingSkillCast = null;
        animationController?.ClearSkillAnimationOverride();
        combatModule?.EndAttack();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AttackPressedEvent>(OnAttack);
        EventBus.Subscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AttackPressedEvent>(OnAttack);
        EventBus.Unsubscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        CancelPendingSkillState();
    }

    private void OnAttack(AttackPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character == null || character.IsDead || character.IsStunned || combatModule == null)
            return;

        combatModule.RequestAttack();
    }

    private void OnSkillSlotPressed(SkillSlotPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character == null || character.IsDead || character.IsStunned || combatModule == null)
            return;

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

        if (!character.HasEnoughMP(definition.ManaCost))
        {
            NotifySystemMessage($"Not enough MP for {definition.DisplayName}.");
            return;
        }

        if (!combatModule.TryStartSkillAttack(definition))
            return;

        if (!character.TrySpendMP(definition.ManaCost))
        {
            animationController?.ClearSkillAnimationOverride();
            combatModule.EndAttack();
            return;
        }

        skillSession.StartSkillCooldown(definition.SkillId, definition.Cooldown);
        CancelPendingSkillState();
        pendingSkillCast = new PendingSkillCast
        {
            Definition = definition,
            LockedTarget = null
        };
        animationController?.PlaySkillAnimation(definition);
    }

    private bool TryHitEnemies()
    {
        if (combatModule == null || targetingService == null)
            return false;

        Transform facingTransform = visual != null ? visual : transform;
        Vector3 origin = facingTransform.position + facingTransform.forward * 1f;
        List<EnemyHealth> enemies = targetingService.GetEnemiesInSphere(origin, attackRange);
        int maxTargets = combatModule.MaxBasicTargets;
        int hitsApplied = 0;

        foreach (EnemyHealth enemy in enemies)
        {
            if (!targetingService.IsWithinAllowedAttackHeight(enemy, origin.y))
                continue;

            ApplyHitToEnemy(enemy, 0.05f);
            hitsApplied++;

            if (hitsApplied >= maxTargets)
                break;
        }

        return hitsApplied > 0;
    }

    private void ExecutePendingSkillCast()
    {
        PendingSkillCast skillCast = pendingSkillCast;
        pendingSkillCast = null;

        if (skillCast?.Definition == null)
            return;

        switch (skillCast.Definition.TargetingMode)
        {
            case PlayerSkillTargetingMode.MeleeArea:
                ExecuteAreaSkillHit(skillCast.Definition);
                break;

            case PlayerSkillTargetingMode.ForwardProjectile:
                ExecuteProjectileSkillCast(skillCast.Definition);
                break;

            default:
                ExecuteFrontSingleTargetSkillHit(skillCast);
                break;
        }
    }

    private void ExecuteAreaSkillHit(PlayerSkillDefinition definition)
    {
        if (targetingService == null)
            return;

        List<EnemyHealth> targets = targetingService.FindSkillAreaTargets(definition);
        if (targets.Count == 0)
            return;

        int hitCount = Mathf.Max(1, definition.HitCount);
        bool commitDeath = hitCount <= 1;
        ApplyHitsToTargets(targets, definition, 0.06f, commitDeath);

        if (hitCount > 1)
        {
            CancelQueuedSkillHits(false);
            queuedSkillHitsRoutine = StartCoroutine(PerformRepeatedAreaSkillHits(definition, hitCount - 1));
        }
    }

    private void ExecuteProjectileSkillCast(PlayerSkillDefinition definition)
    {
        if (definition == null || targetingService == null)
            return;

        CancelQueuedSkillHits(false);

        EnemyHealth lockedTarget = targetingService.FindFrontSingleTarget(definition);
        int projectileCount = Mathf.Max(1, definition.ProjectileCount);

        SpawnProjectileShot(definition, lockedTarget, 0, projectileCount);

        if (projectileCount <= 1)
            return;

        queuedSkillHitsRoutine = StartCoroutine(PerformQueuedProjectileShots(
            definition,
            lockedTarget,
            projectileCount));
    }

    private IEnumerator PerformQueuedProjectileShots(
        PlayerSkillDefinition definition,
        EnemyHealth lockedTarget,
        int projectileCount)
    {
        float interval = Mathf.Max(0.04f, definition.HitInterval > 0f ? definition.HitInterval : 0.08f);

        for (int projectileIndex = 1; projectileIndex < projectileCount; projectileIndex++)
        {
            yield return new WaitForSeconds(interval);

            if (character == null || character.IsDead || character.IsStunned)
                break;

            SpawnProjectileShot(definition, lockedTarget, projectileIndex, projectileCount);
        }

        queuedSkillHitsRoutine = null;
    }

    private void SpawnProjectileShot(
        PlayerSkillDefinition definition,
        EnemyHealth lockedTarget,
        int projectileIndex,
        int projectileCount)
    {
        Transform facingTransform = visual != null ? visual : transform;
        float spreadAngle = Mathf.Max(0f, definition.ProjectileSpreadAngle);
        float lateralSpacing = projectileCount > 1 ? 0.16f : 0f;
        Vector3 spawnBasePosition = facingTransform.position
            + facingTransform.forward * Mathf.Max(0f, definition.ProjectileSpawnForwardOffset)
            + Vector3.up * definition.ProjectileSpawnUpOffset;

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
        SpawnSkillProjectile(definition, spawnPosition, direction, lockedTarget, commitDeathOnHit);
    }

    private void ExecuteFrontSingleTargetSkillHit(PendingSkillCast skillCast)
    {
        if (targetingService == null)
            return;

        EnemyHealth target = targetingService.ResolveLockedSkillTarget(skillCast.Definition, skillCast.LockedTarget, true);
        if (target == null)
            return;

        if (TryExecuteAuthoritativeSkillSequence(skillCast.Definition, target))
        {
            skillCast.LockedTarget = target;
            return;
        }

        int remainingHits = Mathf.Max(0, skillCast.Definition.HitCount - 1);
        bool commitDeath = remainingHits <= 0;
        ApplyHitToEnemy(
            target,
            0.06f,
            commitDeath,
            skillCast.Definition);
        skillCast.LockedTarget = target;

        if (remainingHits <= 0)
            return;

        CancelQueuedSkillHits(false);
        queuedSkillHitsRoutine = StartCoroutine(PerformRepeatedSingleTargetSkillHits(
            skillCast.Definition,
            target,
            remainingHits));
    }

    private IEnumerator PerformRepeatedSingleTargetSkillHits(
        PlayerSkillDefinition definition,
        EnemyHealth initialTarget,
        int remainingHits)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.HitInterval));

            if (character == null || character.IsDead || character.IsStunned || targetingService == null)
                break;

            EnemyHealth target = targetingService.ResolveLockedSkillTarget(definition, initialTarget, false);
            if (target == null)
                break;

            bool commitDeath = hitIndex >= remainingHits - 1;
            ApplyHitToEnemy(
                target,
                0.045f,
                commitDeath,
                definition);
        }

        queuedSkillHitsRoutine = null;
    }

    private bool TryExecuteAuthoritativeSkillSequence(
        PlayerSkillDefinition definition,
        EnemyHealth target)
    {
        if (!SupportsAuthoritativeSkillSequence(definition) || target == null || character == null)
            return false;

        float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
        return MultiplayerPrototypeEnemyCoordinator.TryRequestSkillSequence(
            target,
            direction,
            character,
            definition.SkillId);
    }

    private IEnumerator PerformRepeatedAreaSkillHits(PlayerSkillDefinition definition, int remainingHits)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.HitInterval));

            if (character == null || character.IsDead || character.IsStunned || targetingService == null)
                break;

            List<EnemyHealth> targets = targetingService.FindSkillAreaTargets(definition);
            if (targets.Count == 0)
                break;

            bool commitDeath = hitIndex >= remainingHits - 1;
            ApplyHitsToTargets(targets, definition, 0.045f, commitDeath);
        }

        queuedSkillHitsRoutine = null;
    }

    private void ApplyHitsToTargets(
        List<EnemyHealth> targets,
        PlayerSkillDefinition definition,
        float impactDuration = 0.06f,
        bool commitDeath = true)
    {
        foreach (EnemyHealth target in targets)
            ApplyHitToEnemy(target, impactDuration, commitDeath, definition);
    }

    private int CalculateSkillDamage(PlayerSkillDefinition definition)
    {
        if (definition == null || combatModule == null || character == null)
            return 1;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int baseDamage = combatModule.CalculateDamage(snapshot, isSkillDamage: true);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.DamageMultiplier)));
    }

    private void ApplyHitToEnemy(
        EnemyHealth enemy,
        float impactDuration,
        bool commitDeath = true,
        PlayerSkillDefinition skillDefinition = null)
    {
        if (character == null)
            return;

        hitApplicationService.ApplyHit(
            enemy,
            character,
            transform.position.x,
            0f,
            ResolveLocalFallbackDamage(skillDefinition),
            impactDuration,
            commitDeath,
            skillDefinition != null ? skillDefinition.SkillId : string.Empty);
    }

    private void SpawnSkillProjectile(
        PlayerSkillDefinition definition,
        Vector3 spawnPosition,
        Vector3 direction,
        EnemyHealth lockedTarget,
        bool commitDeathOnHit)
    {
        if (definition == null || character == null)
            return;

        GameObject projectilePrefab = bootstrap != null && bootstrap.RuntimePrefabCatalog != null
            ? bootstrap.RuntimePrefabCatalog.SkillProjectilePrefab
            : null;
        bool skipDefaultVisual = false;

        GameObject projectileObject = null;
        if (projectilePrefab != null)
        {
            projectileObject = Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(direction.normalized, Vector3.up));
            projectileObject.name = $"{definition.SkillId}_Projectile";
            skipDefaultVisual = HasVisualContent(projectileObject);
        }
        else
        {
            projectileObject = new GameObject($"{definition.SkillId}_Projectile");
            projectileObject.transform.position = spawnPosition;
            projectileObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        PlayerSkillProjectile projectile = projectileObject.GetComponent<PlayerSkillProjectile>();
        if (projectile == null)
            projectile = projectileObject.AddComponent<PlayerSkillProjectile>();
        projectile.Initialize(
            character,
            lockedTarget,
            enemyLayer,
            MultiplayerPrototypeRuntime.IsEnabled ? 0 : CalculateSkillDamage(definition),
            definition.SkillId,
            direction,
            definition.ProjectileSpeed,
            definition.ProjectileRadius,
            definition.ProjectileLifetime,
            definition.ProjectileVisualScale,
            commitDeathOnHit,
            skipDefaultVisual);
    }

    private static bool HasVisualContent(GameObject rootObject)
    {
        if (rootObject == null)
            return false;

        return rootObject.GetComponentInChildren<Renderer>(true) != null
            || rootObject.GetComponentInChildren<TrailRenderer>(true) != null
            || rootObject.GetComponentInChildren<ParticleSystem>(true) != null;
    }

    private int ResolveLocalFallbackDamage(PlayerSkillDefinition skillDefinition)
    {
        if (skillDefinition != null)
            return CalculateSkillDamage(skillDefinition);

        if (combatModule == null || character == null)
            return 1;

        return Mathf.Max(1, combatModule.CalculateBasicDamage(character.GetCombatSnapshot()));
    }

    private static bool SupportsAuthoritativeSkillSequence(PlayerSkillDefinition definition)
    {
        return MultiplayerPrototypeRuntime.IsEnabled
            && definition != null
            && definition.HitCount > 1
            && definition.TargetingMode == PlayerSkillTargetingMode.FrontSingleTarget;
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
}
