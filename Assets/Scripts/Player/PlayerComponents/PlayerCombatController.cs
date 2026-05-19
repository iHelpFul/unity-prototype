using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    private PlayerCharacter character;
    private Transform visual;
    private PlayerAnimationController animationController;
    private GameBootstrap bootstrap;
    private PlayerMovementController movementController;
    private readonly PlayerHitApplicationService hitApplicationService = new PlayerHitApplicationService();
    private readonly PlayerDirectHitExecutionService directHitExecutionService = new PlayerDirectHitExecutionService();
    private readonly PlayerProjectileExecutionService projectileExecutionService = new PlayerProjectileExecutionService();
    private readonly PlayerChargeController chargeController = new PlayerChargeController();
    private readonly PlayerSkillActionCoordinator skillActionCoordinator = new PlayerSkillActionCoordinator();
    private readonly PlayerAttackSequenceScheduler attackSequenceScheduler = new PlayerAttackSequenceScheduler();
    private readonly PlayerSkillCastExecutionService skillCastExecutionService = new PlayerSkillCastExecutionService();
    private readonly PlayerBasicAttackRuntimeService basicAttackRuntimeService = new PlayerBasicAttackRuntimeService();

    private PlayerCombatModule combatModule;
    private ProjectileSystem projectileSystem;
    private PlayerProgressionModule progression;
    private PlayerTargetingService targetingService;

    private LayerMask enemyLayer;
    private float skillFrontDotThreshold;
    private float skillAreaForwardOffsetFactor;
    private float skillAreaRadiusFactor;
    private float skillAreaMinRadius;
    private float lockedSkillTargetGraceRange;
    private float attackLowerHeightAllowance;

    public int ComboIndex => combatModule != null ? combatModule.ComboIndex : 0;
    public bool IsAttacking =>
        (combatModule != null && combatModule.IsAttacking)
        || (character != null
            && character.ActionStateController != null
            && (character.ActionStateController.IsBurstSkillChargeActive
                || character.ActionStateController.IsBurstSkillCommitted));
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
        movementController ??= GetComponent<PlayerMovementController>();
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
        RefreshExecutionServices();
        RefreshCoordinatorServices();
        RefreshRuntimeServices();
        RefreshProgression();
        SyncCombatProfile();
    }

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
        skillActionCoordinator.BindBootstrap(sessionBootstrap);
        RefreshProgression();
        SyncCombatProfile();
    }

    public void SyncCombatProfile()
    {
        combatModule ??= new PlayerCombatModule();

        if (character != null)
        {
            combatModule.SetBasicAttackProfile(character.GetBasicAttackProfile());
            character.RefreshCombatGaugeCapacity();
        }
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
        skillActionCoordinator.Tick(deltaTime);
        chargeController.Tick(deltaTime);
        skillActionCoordinator.SyncCommittedChargeReleases();

        if (wasAttacking
            && !combatModule.IsAttacking)
        {
            if (skillActionCoordinator.HasPendingSkillCast)
            {
                CancelPendingSkillState(false);
                return;
            }

            if (!attackSequenceScheduler.HasActiveSequence)
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

        if (attackSequenceScheduler.HasActiveSequence
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

        skillActionCoordinator.SyncCommittedChargeReleases();

        if (skillActionCoordinator.TryConsumePendingSkillCast(out PendingSkillCastRequest pendingSkillCast))
        {
            ExecutePendingSkillCast(pendingSkillCast);
            return;
        }

        basicAttackRuntimeService.HandleBasicAttackHitFrame();
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

        basicAttackRuntimeService.HandleComboWindow();
    }

    public void OnAttackEnd()
    {
        if (character == null || !character.IsLocalPlayer)
            return;

        skillActionCoordinator.SyncCommittedChargeReleases();

        if (skillActionCoordinator.TryCompleteActiveUtility())
        {
            return;
        }

        if (chargeController.ShouldSuppressAttackEnd)
        {
            return;
        }

        skillActionCoordinator.ClearPendingSkillCast();
        animationController?.ClearActionAnimationOverride();
        combatModule?.EndAttack();
        CompleteCurrentActionState();
    }

    public void OnBurstChargeLoopReady()
    {
        if (character == null || !character.IsLocalPlayer)
            return;

        chargeController.OnBurstChargeLoopReady();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AttackPressedEvent>(OnAttack);
        EventBus.Subscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Subscribe<SkillSlotReleasedEvent>(OnSkillSlotReleased);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<PlayerHitEvent>(OnPlayerHit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AttackPressedEvent>(OnAttack);
        EventBus.Unsubscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Unsubscribe<SkillSlotReleasedEvent>(OnSkillSlotReleased);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<PlayerHitEvent>(OnPlayerHit);
        CancelPendingSkillState();
        chargeController.CancelAllCharges(clearLockedGaugeSpend: true);
        skillActionCoordinator.ClearTransientState();
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
            || skillActionCoordinator.HasPendingBurstInput
            || !character.ActionStateController.CanRequestBasicAttack)
        {
            return;
        }

        basicAttackRuntimeService.TryStartBasicAttack();
    }

    private void OnSkillSlotPressed(SkillSlotPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        skillActionCoordinator.HandleSkillSlotPressed(e.SlotIndex);
    }

    private void OnSkillSlotReleased(SkillSlotReleasedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        skillActionCoordinator.HandleSkillSlotReleased(e.SlotIndex);
    }

    private void ExecutePendingSkillCast(PendingSkillCastRequest skillCast)
    {
        if (!skillCastExecutionService.ExecuteSkillCast(skillCast) && skillCast?.Definition == null)
            CompleteCurrentActionState();
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
            ResolvePresentationSourceTransform(),
            target);
    }

    private void StopPresentationCue(
        PresentationCueSet cueSet,
        CombatCuePhase phase,
        Vector3 worldPosition,
        Transform target = null)
    {
        if (cueSet == null || character == null)
            return;

        CombatPresentationDispatcher.StopCuePhase(
            cueSet,
            phase,
            worldPosition,
            ResolvePresentationSourceTransform(),
            target);
    }

    private Transform ResolvePresentationSourceTransform()
    {
        if (visual != null)
            return visual;

        if (character != null)
            return character.transform;

        return transform;
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
        skillActionCoordinator.CancelPendingSkillState();

        if (cancelQueuedHits)
            attackSequenceScheduler.CancelActiveSequence();

        if (character != null
            && character.ActionStateController != null
            && character.ActionStateController.HasPendingSkillCommit)
        {
            character.ActionStateController.ResetState();
        }
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (character == null || e.Killer != character)
            return;

        progression?.AddExp(e.ExpReward);
        character.GainCombatGauge(e.GaugeReward);
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

        if (!chargeController.HandleInterruptingHit())
            return;

        skillActionCoordinator.ClearTransientState();
        combatModule?.EndAttack();
        character.ActionStateController.InterruptCurrentAction();
        animationController?.PlayHitReaction();
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

    private void RefreshExecutionServices()
    {
        directHitExecutionService.Initialize(
            character,
            transform,
            targetingService,
            hitApplicationService);
        projectileExecutionService.Initialize(
            character,
            transform,
            visual,
            targetingService,
            projectileSystem,
            enemyLayer);
    }

    private void RefreshCoordinatorServices()
    {
        chargeController.Initialize(
            character,
            combatModule,
            animationController,
            IsPlayerAerial,
            (cueSet, phase, position) => PublishPresentationCue(cueSet, phase, position),
            (cueSet, phase, position) => StopPresentationCue(cueSet, phase, position));
        skillActionCoordinator.Initialize(
            character,
            combatModule,
            animationController,
            movementController,
            visual,
            chargeController,
            bootstrap,
            (cueSet, phase, position) => PublishPresentationCue(cueSet, phase, position),
            NotifySystemMessage);
    }

    private void RefreshRuntimeServices()
    {
        attackSequenceScheduler.Initialize(
            this,
            character,
            directHitExecutionService,
            projectileExecutionService,
            CompleteCurrentActionState);
        skillCastExecutionService.Initialize(
            character,
            directHitExecutionService,
            projectileExecutionService,
            attackSequenceScheduler,
            (cueSet, phase, position) => PublishPresentationCue(cueSet, phase, position));
        basicAttackRuntimeService.Initialize(
            character,
            movementController,
            animationController,
            combatModule,
            directHitExecutionService,
            projectileExecutionService,
            (cueSet, phase, position) => PublishPresentationCue(cueSet, phase, position));
    }

    private void RefreshProgression()
    {
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        PlayerRuntimeData data = character != null && character.IsLocalPlayer && characterSession != null
            ? characterSession.PlayerData
            : null;

        progression = new PlayerProgressionModule(data, character, bootstrap);
    }

    private bool IsPlayerAerial()
    {
        return movementController != null && !movementController.IsGrounded;
    }

    private void CompleteCurrentActionState()
    {
        if (character == null
            || character.ActionStateController == null
            || skillActionCoordinator.HasPendingSkillCast
            || attackSequenceScheduler.HasActiveSequence)
        {
            return;
        }

        character.ActionStateController.CompleteCurrentAction();
    }

}
