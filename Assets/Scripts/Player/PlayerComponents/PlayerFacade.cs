using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerFacade : MonoBehaviour
{
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Transform visual;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerMovementController movementController;
    private PlayerTargetingService targetingService;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private float footstepMinInterval = 0.22f;
    [SerializeField] private PlayerCharacter character;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameBootstrap bootstrap;

    [Header("Skill Combat")]
    [SerializeField] private float skillFrontDotThreshold = 0.1f;
    [SerializeField] private float skillAreaForwardOffsetFactor = 0.55f;
    [SerializeField] private float skillAreaRadiusFactor = 0.65f;
    [SerializeField] private float skillAreaMinRadius = 0.95f;
    [SerializeField] private float lockedSkillTargetGraceRange = 0.75f;
    [SerializeField] private float attackLowerHeightAllowance = 0.6f;

    [Header("Skill Gizmos")]
    [SerializeField] private bool drawSkillRangeGizmos = true;
    [SerializeField] private PlayerJobType previewJobForSkillGizmos = PlayerJobType.Warrior;
    [SerializeField] private Color basicAttackGizmoColor = new Color(1f, 0.25f, 0.25f, 0.3f);
    [SerializeField] private Color meleeSkillGizmoColor = new Color(1f, 0.78f, 0.2f, 0.35f);
    [SerializeField] private Color rangedSkillGizmoColor = new Color(0.3f, 0.85f, 1f, 0.35f);

    private sealed class PendingSkillCast
    {
        public PlayerSkillDefinition Definition;
        public EnemyHealth LockedTarget;
    }

    private float lastFootstepTime;
    private bool wasGrounded;
    private PlayerCombatModule combatModule;
    private PlayerProgressionModule progression;
    private PendingSkillCast pendingSkillCast;
    private Coroutine queuedSkillHitsRoutine;
    private float presentationHorizontalSpeed;
    private float presentationVerticalVelocity;
    private bool presentationIsGrounded;
    private bool presentationIsAttacking;
    private int presentationComboIndex;
    private float presentationAttackAnimationSpeed = 1f;
    private ushort jumpPresentationSequence;
    private ushort landPresentationSequence;

    public Transform VisualTransform => visual;
    public float PresentationHorizontalSpeed => presentationHorizontalSpeed;
    public float PresentationVerticalVelocity => presentationVerticalVelocity;
    public bool PresentationIsGrounded => presentationIsGrounded;
    public bool PresentationIsAttacking => presentationIsAttacking;
    public int PresentationComboIndex => presentationComboIndex;
    public float PresentationAttackAnimationSpeed => presentationAttackAnimationSpeed;
    public ushort JumpPresentationSequence => jumpPresentationSequence;
    public ushort LandPresentationSequence => landPresentationSequence;
    public int CurrentComboCounter => combatModule != null ? combatModule.CurrentComboCounter : 0;

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;

        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        PlayerRuntimeData data = character != null && character.IsLocalPlayer && characterSession != null
            ? characterSession.PlayerData
            : null;

        progression = new PlayerProgressionModule(data, character, bootstrap);

        if (combatModule != null && character != null)
            combatModule.SetBasicAttackProfile(character.GetBasicAttackProfile());
    }

    private void OnEnable()
    {
        // Movement input handling moved to PlayerMovementController
        EventBus.Subscribe<AttackPressedEvent>(OnAttack);
        EventBus.Subscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        // Movement input handling moved to PlayerMovementController
        EventBus.Unsubscribe<AttackPressedEvent>(OnAttack);
        EventBus.Unsubscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        CancelPendingSkillState();
    }

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotor>();

        if (character == null)
            character = GetComponent<PlayerCharacter>();

        if (animationController == null)
            animationController = GetComponent<PlayerAnimationController>();

        if (visual == null)
            visual = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        combatModule = new PlayerCombatModule();

        if (motor == null || character == null || animationController == null)
        {
            Debug.LogError("PlayerFacade is missing required component references.");
            enabled = false;
            return;
        }

        if (bootstrap != null)
            BindBootstrap(bootstrap);
        else
            combatModule.SetBasicAttackProfile(character.GetBasicAttackProfile());

        if (movementController == null)
            movementController = GetComponent<PlayerMovementController>();

        if (movementController == null)
            movementController = gameObject.AddComponent<PlayerMovementController>();

        movementController.Initialize(motor, visual, cameraTransform, character);

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

    private void Update()
    {
        if (!character.IsLocalPlayer)
            return;

        if (character.IsDead)
            return;

        combatModule.SetBasicAttackProfile(character.GetBasicAttackProfile());

        // Movement logic delegated to PlayerMovementController
        if (movementController != null)
            movementController.Tick(Time.deltaTime);

        UpdateVisualsAndCombat();
    }

    private void UpdateCameraVectors()
    {
        // Camera handling moved to PlayerMovementController
    }

    private void UpdateVisualsAndCombat()
    {
        bool isGrounded = movementController != null ? movementController.IsGrounded : motor.IsGrounded;
        bool jumpedThisFrame = movementController != null ? movementController.JumpedThisFrame : false;
        bool landedThisFrame = !wasGrounded && isGrounded;
        int comboIndex = combatModule.ComboIndex;
        bool isAttacking = combatModule.IsAttacking;
        float attackAnimationSpeed = combatModule.AttackAnimationSpeed;
        float horizontalSpeed = movementController != null ? movementController.HorizontalSpeed : 0f;
        float verticalVelocity = movementController != null ? movementController.VerticalVelocity : 0f;

        animationController.UpdateAnimation(
            horizontalSpeed,
            verticalVelocity,
            isGrounded,
            jumpedThisFrame,
            landedThisFrame,
            comboIndex,
            isAttacking,
            attackAnimationSpeed
        );

        presentationHorizontalSpeed = horizontalSpeed;
        presentationVerticalVelocity = verticalVelocity;
        presentationIsGrounded = isGrounded;
        presentationComboIndex = comboIndex;
        presentationIsAttacking = isAttacking;
        presentationAttackAnimationSpeed = attackAnimationSpeed;

        if (jumpedThisFrame)
            jumpPresentationSequence++;

        if (landedThisFrame)
            landPresentationSequence++;

        wasGrounded = isGrounded;
        if (!character.IsStunned)
            combatModule.Tick(Time.deltaTime);
    }

    // Movement event handlers moved to PlayerMovementController. This method is invoked by the controller
    // when a jump is pressed so PlayerFacade can handle attack cancellation logic which still lives here.
    public void HandleJumpPressedFromMovementController()
    {
        if (combatModule.IsAttacking)
        {
            CancelPendingSkillState();
            combatModule.EndAttack();
        }
    }

    private void OnAttack(AttackPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character.IsDead || character.IsStunned)
            return;

        combatModule.RequestAttack();
    }

    private void OnSkillSlotPressed(SkillSlotPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character.IsDead || character.IsStunned)
            return;

        if (bootstrap == null)
            return;

        PlayerSessionSkillApplicationService skillSession = bootstrap.SkillSession;
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
            animationController.ClearSkillAnimationOverride();
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
        animationController.PlaySkillAnimation(definition);
    }

    private bool TryHitEnemies()
    {
        Vector3 origin = visual.position + visual.forward * 1f;
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

    public void OnHitFrame()
    {
        if (!character.IsLocalPlayer)
            return;

        if (pendingSkillCast != null)
        {
            ExecutePendingSkillCast();
            return;
        }

        bool landedHit = TryHitEnemies();

        if (landedHit)
            combatModule.RegisterSuccessfulBasicHit();
    }

    public void OnComboWindow()
    {
        if (!character.IsLocalPlayer)
            return;

        combatModule.OnComboWindow();
    }

    public void OnAttackEnd()
    {
        if (!character.IsLocalPlayer)
            return;

        pendingSkillCast = null;
        animationController.ClearSkillAnimationOverride();
        combatModule.EndAttack();
    }

    public void OnSwingStart()
    {
        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.SwordSwing,
            Position = transform.position
        });
    }

    public void OnLandEffect()
    {
        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.Land,
            Position = transform.position
        });
    }

    public void OnFootSteps()
    {
        bool grounded = motor != null ? motor.IsGrounded : (movementController != null && movementController.IsGrounded);

        if (!grounded)
            return;

        float hSpeed = movementController != null ? movementController.HorizontalSpeed : 0f;
        if (hSpeed < 0.1f)
            return;

        if (combatModule.IsAttacking)
            return;

        if (Time.time < lastFootstepTime + footstepMinInterval)
            return;

        lastFootstepTime = Time.time;

        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.Footstep,
            Position = transform.position
        });
    }

    public void OnJumpSound()
    {
        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.Jump,
            Position = transform.position
        });
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
        if (definition == null)
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

        if (lockedTarget != null)
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

            if (character == null || character.IsDead || character.IsStunned)
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
        if (!SupportsAuthoritativeSkillSequence(definition) || target == null)
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

            if (character == null || character.IsDead || character.IsStunned)
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
        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int baseDamage = combatModule.CalculateDamage(snapshot);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.DamageMultiplier)));
    }

    

    private void ApplyHitToEnemy(
        EnemyHealth enemy,
        float impactDuration,
        bool commitDeath = true,
        PlayerSkillDefinition skillDefinition = null)
    {
        if (enemy == null || enemy.IsDead)
            return;

        float direction = Mathf.Sign(enemy.transform.position.x - transform.position.x);

        EventBus.Publish(new HitImpactEvent
        {
            Duration = impactDuration,
            TimeScale = 0.1f,
            Damage = 0
        });

        if (MultiplayerPrototypeEnemyCoordinator.TryRequestDamage(
            enemy,
            direction,
            character,
            commitDeath,
            skillDefinition != null ? skillDefinition.SkillId : string.Empty))
        {
            return;
        }

        enemy.TakeDamage(
            ResolveLocalFallbackDamage(skillDefinition),
            direction,
            character,
            commitDeath);
    }

    private void SpawnSkillProjectile(
        PlayerSkillDefinition definition,
        Vector3 spawnPosition,
        Vector3 direction,
        EnemyHealth lockedTarget,
        bool commitDeathOnHit)
    {
        if (definition == null)
            return;

        GameObject projectileObject = new GameObject($"{definition.SkillId}_Projectile");
        projectileObject.transform.position = spawnPosition;
        projectileObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        PlayerSkillProjectile projectile = projectileObject.AddComponent<PlayerSkillProjectile>();
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
            commitDeathOnHit);
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
        if (e.Killer != character)
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

    private void OnDrawGizmosSelected()
    {
        if (visual == null)
            visual = transform;

        DrawBasicAttackGizmo();

        if (!drawSkillRangeGizmos)
            return;

        PlayerJobType jobForGizmos = GetSkillGizmoJob();
        IReadOnlyList<PlayerSkillDefinition> skills = PlayerJobCombatProfiles.GetDefaultSkillsForJob(jobForGizmos);

        for (int index = 0; index < skills.Count; index++)
        {
            PlayerSkillDefinition skill = skills[index];
            if (skill == null || skill.SkillType != PlayerSkillType.ActiveAttack)
                continue;

            DrawSkillGizmo(skill);
        }
    }

    private void DrawBasicAttackGizmo()
    {
        Gizmos.color = basicAttackGizmoColor;

        Vector3 origin = visual.position + visual.forward * 1f;
        Gizmos.DrawLine(transform.position, origin);

#if UNITY_EDITOR
        DrawUpperHemisphereGizmo(origin, attackRange, new Color(
            basicAttackGizmoColor.r,
            basicAttackGizmoColor.g,
            basicAttackGizmoColor.b,
            1f));
#endif
    }

    private void DrawSkillGizmo(PlayerSkillDefinition skill)
    {
        if (skill == null || visual == null)
            return;

        switch (skill.TargetingMode)
        {
            case PlayerSkillTargetingMode.MeleeArea:
                DrawMeleeSkillGizmo(skill);
                break;

            case PlayerSkillTargetingMode.ForwardProjectile:
                DrawProjectileSkillGizmo(skill);
                break;

            default:
                DrawFrontSingleTargetSkillGizmo(skill);
                break;
        }
    }

    private void DrawMeleeSkillGizmo(PlayerSkillDefinition skill)
    {
        float searchRadius = targetingService != null
            ? targetingService.GetSkillAreaRadius(skill)
            : Mathf.Max(skillAreaMinRadius, skill.Range * skillAreaRadiusFactor);

        Transform facingTransform = visual != null ? visual : transform;
        Vector3 center = targetingService != null
            ? targetingService.GetSkillAreaCenter(skill, searchRadius)
            : facingTransform.position + facingTransform.forward * Mathf.Max(searchRadius * 0.25f, skill.Range * skillAreaForwardOffsetFactor);

        Gizmos.color = meleeSkillGizmoColor;
        Gizmos.DrawLine(visual.position, center);

#if UNITY_EDITOR
        DrawUpperHemisphereGizmo(center, searchRadius, new Color(
            meleeSkillGizmoColor.r,
            meleeSkillGizmoColor.g,
            meleeSkillGizmoColor.b,
            1f));
        Handles.Label(center + Vector3.up * (searchRadius + 0.15f), $"{skill.DisplayName} ({skill.Range:0.0})");
#endif
    }

    private void DrawFrontSingleTargetSkillGizmo(PlayerSkillDefinition skill)
    {
        Vector3 origin = visual.position;
        Vector3 forward = visual.forward;
        float range = Mathf.Max(0.1f, skill.Range);
        float halfAngle = Mathf.Acos(Mathf.Clamp(skillFrontDotThreshold, -1f, 1f)) * Mathf.Rad2Deg;

        Gizmos.color = rangedSkillGizmoColor;
        Gizmos.DrawLine(origin, origin + forward * range);

#if UNITY_EDITOR
        Handles.color = new Color(rangedSkillGizmoColor.r, rangedSkillGizmoColor.g, rangedSkillGizmoColor.b, 1f);
        DrawUpperHemisphereGizmo(origin, range, Handles.color);
        Vector3 fromDirection = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
        Handles.DrawWireArc(origin, Vector3.up, fromDirection, halfAngle * 2f, range);
        Handles.Label(origin + forward * (range + 0.2f) + Vector3.up * 0.1f, $"{skill.DisplayName} ({range:0.0})");
#endif
    }

    private void DrawProjectileSkillGizmo(PlayerSkillDefinition skill)
    {
        Vector3 origin = visual.position;
        Vector3 forward = visual.forward;
        float range = Mathf.Max(0.1f, skill.Range);
        int projectileCount = Mathf.Max(1, skill.ProjectileCount);
        float spreadAngle = Mathf.Max(0f, skill.ProjectileSpreadAngle);
        float lateralSpacing = projectileCount > 1 ? 0.16f : 0f;
        Vector3 spawnBasePosition = visual.position
            + forward * Mathf.Max(0f, skill.ProjectileSpawnForwardOffset)
            + Vector3.up * skill.ProjectileSpawnUpOffset;

        Gizmos.color = rangedSkillGizmoColor;
        Gizmos.DrawLine(origin, spawnBasePosition);

#if UNITY_EDITOR
        Color projectileColor = new Color(rangedSkillGizmoColor.r, rangedSkillGizmoColor.g, rangedSkillGizmoColor.b, 1f);
        DrawUpperHemisphereGizmo(origin, range, projectileColor);
        Handles.color = projectileColor;

        for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
        {
            float normalizedIndex = projectileCount == 1
                ? 0.5f
                : projectileIndex / (float)(projectileCount - 1);

            float yawOffset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, normalizedIndex);

            float lateralOffset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-lateralSpacing * 0.5f, lateralSpacing * 0.5f, normalizedIndex);

            Vector3 spawnPosition = spawnBasePosition + visual.right * lateralOffset;
            Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * forward;
            Handles.DrawWireDisc(spawnPosition, Vector3.up, Mathf.Max(0.04f, skill.ProjectileRadius));
            Handles.DrawLine(spawnPosition, spawnPosition + direction * range);
        }

        Handles.Label(
            spawnBasePosition + Vector3.up * 0.18f,
            $"{skill.DisplayName} ({range:0.0})");
#endif
    }

    private PlayerJobType GetSkillGizmoJob()
    {
        if (Application.isPlaying && character != null)
            return character.CurrentJob;

        return previewJobForSkillGizmos;
    }

#if UNITY_EDITOR
    private void DrawUpperHemisphereGizmo(Vector3 center, float radius, Color color)
    {
        Handles.color = color;
        const int ringCount = 4;
        const int meridianCount = 4;
        const int meridianSegments = 14;

        for (int ringIndex = 0; ringIndex <= ringCount; ringIndex++)
        {
            float t = ringIndex / (float)ringCount;
            float angle = Mathf.Lerp(0f, Mathf.PI * 0.5f, t);
            float ringHeight = Mathf.Sin(angle) * radius;
            float ringRadius = Mathf.Cos(angle) * radius;
            Handles.DrawWireDisc(center + Vector3.up * ringHeight, Vector3.up, ringRadius);
        }

        for (int meridianIndex = 0; meridianIndex < meridianCount; meridianIndex++)
        {
            float yaw = 180f / meridianCount * meridianIndex;
            Vector3 horizontalAxis = Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.right;
            Vector3[] points = new Vector3[meridianSegments + 1];

            for (int pointIndex = 0; pointIndex <= meridianSegments; pointIndex++)
            {
                float t = pointIndex / (float)meridianSegments;
                float angle = Mathf.Lerp(0f, Mathf.PI, t);
                Vector3 point = center
                    + horizontalAxis * Mathf.Cos(angle) * radius
                    + Vector3.up * Mathf.Sin(angle) * radius;
                points[pointIndex] = point;
            }

            Handles.DrawAAPolyLine(2f, points);
        }
    }
#endif
}
