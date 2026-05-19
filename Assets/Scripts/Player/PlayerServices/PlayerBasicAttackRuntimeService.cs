using UnityEngine;

public sealed class PlayerBasicAttackRuntimeService
{
    private readonly struct BasicAttackRewardContext
    {
        public readonly PlayerBasicAttackProfile Profile;
        public readonly bool IsChainFinisher;
        public readonly bool IsAerial;
        public readonly int MomentumGainBonus;

        public BasicAttackRewardContext(
            PlayerBasicAttackProfile profile,
            bool isChainFinisher,
            bool isAerial,
            int momentumGainBonus)
        {
            Profile = profile;
            IsChainFinisher = isChainFinisher;
            IsAerial = isAerial;
            MomentumGainBonus = Mathf.Max(0, momentumGainBonus);
        }
    }

    private PlayerCharacter character;
    private PlayerMovementController movementController;
    private PlayerAnimationController animationController;
    private PlayerCombatModule combatModule;
    private PlayerDirectHitExecutionService directHitExecutionService;
    private PlayerProjectileExecutionService projectileExecutionService;
    private System.Action<PresentationCueSet, CombatCuePhase, Vector3> publishCue;
    private bool currentBasicAttackIsEmpowered;
    private EmpoweredBasicDefinition currentEmpoweredBasicDefinition;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        PlayerMovementController resolvedMovementController,
        PlayerAnimationController resolvedAnimationController,
        PlayerCombatModule resolvedCombatModule,
        PlayerDirectHitExecutionService resolvedDirectHitExecutionService,
        PlayerProjectileExecutionService resolvedProjectileExecutionService,
        System.Action<PresentationCueSet, CombatCuePhase, Vector3> publishPresentationCue)
    {
        character = ownerCharacter;
        movementController = resolvedMovementController;
        animationController = resolvedAnimationController;
        combatModule = resolvedCombatModule;
        directHitExecutionService = resolvedDirectHitExecutionService;
        projectileExecutionService = resolvedProjectileExecutionService;
        publishCue = publishPresentationCue;
    }

    public bool TryStartBasicAttack()
    {
        if (character == null || combatModule == null)
            return false;

        currentBasicAttackIsEmpowered = false;
        currentEmpoweredBasicDefinition = default;
        bool wasAttacking = combatModule.IsAttacking;
        bool shouldPreviewEmpoweredBasic = !wasAttacking && character.HasEmpoweredBasicReady;
        EmpoweredBasicDefinition previewEmpoweredBasic = shouldPreviewEmpoweredBasic
            ? character.GetActiveEmpoweredBasic()
            : default;
        combatModule.RequestAttack(
            shouldPreviewEmpoweredBasic && previewEmpoweredBasic.IsConfigured
                ? previewEmpoweredBasic.AnimationSpeedMultiplier
                : 1f);
        if (wasAttacking || !combatModule.IsAttacking)
            return false;

        if (shouldPreviewEmpoweredBasic
            && character.TryConsumeEmpoweredBasic(out EmpoweredBasicDefinition consumedEmpoweredBasic))
        {
            currentBasicAttackIsEmpowered = consumedEmpoweredBasic.IsConfigured;
            currentEmpoweredBasicDefinition = consumedEmpoweredBasic.GetSanitized();
        }

        character.ActionStateController?.BeginBasicAttack(GetBasicAttackRecoveryDuration());
        PlayerBasicAttackProfile profile = character.GetBasicAttackProfile();
        animationController?.PlayBasicAttackAnimation(profile, combatModule.ComboIndex);
        publishCue?.Invoke(
            profile != null ? profile.PresentationCueSet : null,
            CombatCuePhase.CastStart,
            character.transform.position);
        return true;
    }

    public void HandleBasicAttackHitFrame()
    {
        if (character == null || combatModule == null)
            return;

        PlayerBasicAttackProfile basicAttackProfile = character.GetBasicAttackProfile();
        BasicAttackRewardContext rewardContext = BuildBasicAttackRewardContext(basicAttackProfile);
        if (currentBasicAttackIsEmpowered && currentEmpoweredBasicDefinition.IsConfigured)
            character.TryActivateGrantedReadyState(currentEmpoweredBasicDefinition);

        publishCue?.Invoke(
            basicAttackProfile != null ? basicAttackProfile.PresentationCueSet : null,
            CombatCuePhase.Release,
            character.transform.position);

        if (ShouldUseBasicAttackProjectile(basicAttackProfile))
        {
            SpawnBasicAttackProjectile(basicAttackProfile, rewardContext);
            ClearCurrentEmpoweredBasicAttack();
            return;
        }

        bool landedHit = TryHitEnemies();
        ClearCurrentEmpoweredBasicAttack();
        if (!landedHit)
            return;

        ApplySuccessfulBasicAttackRewards(rewardContext);
    }

    public void HandleComboWindow()
    {
        if (character == null || combatModule == null)
            return;

        int previousComboIndex = combatModule.ComboIndex;
        combatModule.OnComboWindow();

        if (combatModule.ComboIndex != previousComboIndex)
            animationController?.PlayBasicAttackAnimation(character.GetBasicAttackProfile(), combatModule.ComboIndex);
    }

    public float GetBasicAttackRecoveryDuration()
    {
        PlayerBasicAttackProfile profile = character != null ? character.GetBasicAttackProfile() : null;
        return profile != null ? profile.RecoveryTime : 0f;
    }

    private bool TryHitEnemies()
    {
        if (combatModule == null || directHitExecutionService == null || character == null)
            return false;

        PlayerBasicAttackProfile profile = character.GetBasicAttackProfile();
        AttackPayload payload = BuildBasicAttackPayload();
        if (profile == null || payload == null)
            return false;

        int maxTargets = combatModule.MaxBasicTargets;
        float impactDuration = GetBasicAttackImpactDuration(profile);
        return directHitExecutionService.TryHitBasicAttack(profile, payload, maxTargets, impactDuration);
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
            combatModule.CurrentComboCounter,
            currentBasicAttackIsEmpowered ? currentEmpoweredBasicDefinition : default);
    }

    private bool ShouldUseBasicAttackProjectile(PlayerBasicAttackProfile profile)
    {
        if (profile == null)
            return false;

        return profile.ExecutionKind == CombatExecutionKind.Projectile;
    }

    private void SpawnBasicAttackProjectile(PlayerBasicAttackProfile profile, BasicAttackRewardContext rewardContext)
    {
        if (profile == null || character == null || projectileExecutionService == null)
            return;

        AttackPayload payload = BuildBasicAttackPayload();
        if (payload == null)
            return;

        projectileExecutionService.SpawnBasicAttackProjectile(
            profile,
            payload,
            () => ApplySuccessfulBasicAttackRewards(rewardContext));
    }

    private void ApplySuccessfulBasicAttackRewards(BasicAttackRewardContext rewardContext)
    {
        if (character == null || rewardContext.Profile == null)
            return;

        RegisterGaugeBuildFromBasicAttack(rewardContext);
        GainMomentumFromBasicAttack(rewardContext);
        combatModule?.RegisterSuccessfulBasicHit();
    }

    private void GainMomentumFromBasicAttack(BasicAttackRewardContext rewardContext)
    {
        if (character == null || rewardContext.Profile == null)
            return;

        int totalMomentumGain = Mathf.Max(0, rewardContext.Profile.MomentumGainOnValidHit + rewardContext.MomentumGainBonus);
        character.GainCombatMomentum(totalMomentumGain);
    }

    private void RegisterGaugeBuildFromBasicAttack(BasicAttackRewardContext rewardContext)
    {
        if (character == null || rewardContext.Profile == null)
            return;

        character.RegisterBasicAttackBuilderHit(
            rewardContext.Profile,
            rewardContext.IsChainFinisher,
            rewardContext.IsAerial,
            wasValidHit: true);
    }

    private BasicAttackRewardContext BuildBasicAttackRewardContext(PlayerBasicAttackProfile basicAttackProfile)
    {
        if (basicAttackProfile == null || combatModule == null)
            return default;

        PlayerCombatSnapshot snapshot = character != null ? character.GetCombatSnapshot() : default;
        bool isChainFinisher =
            combatModule.CurrentChainCount >= Mathf.Max(1, basicAttackProfile.MaxChainCount);
        bool isAerial = movementController != null && !movementController.IsGrounded;
        return new BasicAttackRewardContext(
            basicAttackProfile,
            isChainFinisher,
            isAerial,
            snapshot.MomentumGainBonus);
    }

    private static float GetBasicAttackImpactDuration(PlayerBasicAttackProfile profile)
    {
        if (profile == null)
            return 0.05f;

        if (profile.ActiveTime > 0f)
            return Mathf.Clamp(profile.ActiveTime * 0.5f, 0.03f, 0.12f);

        return 0.05f;
    }

    private void ClearCurrentEmpoweredBasicAttack()
    {
        currentBasicAttackIsEmpowered = false;
        currentEmpoweredBasicDefinition = default;
    }
}
