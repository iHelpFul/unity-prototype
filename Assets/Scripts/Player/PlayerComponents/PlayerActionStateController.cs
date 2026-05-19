using UnityEngine;

public class PlayerActionStateController : MonoBehaviour
{
    [Header("Action State Durations")]
    [SerializeField] private float fallbackBasicAttackRecoveryDuration = 0.06f;
    [SerializeField] private float fallbackSkillRecoveryDuration = 0.08f;
    [SerializeField] private float fallbackUtilityRecoveryDuration = 0.08f;
    [SerializeField] private float interruptedDuration = 0.12f;

    private PlayerActionStateType currentActionState;
    private float actionStateTimer;
    private float currentRecoveryDuration;
    private bool burstSkillChargeActive;
    private bool utilityActive;
    private bool utilityActivatedDuringCharge;
    private bool utilityPreserveHoldCharge;
    private bool utilitySuppressHitInterruptWhileActive;
    private float utilityInterruptGuardTimer;
    private float utilityProtectionTimer;
    private bool utilityPreventDamageWhileActive;
    private bool utilityPreventKnockbackWhileActive;
    private bool utilityPreventStunWhileActive;
    private float utilityRecoveryDuration;

    public PlayerActionStateType CurrentActionState => currentActionState;
    public bool HasPendingSkillCommit => currentActionState == PlayerActionStateType.SkillPreCommit;
    public bool IsSkillCommitted => currentActionState == PlayerActionStateType.SkillCommitted;
    public bool IsBurstSkillCommitted => currentActionState == PlayerActionStateType.BurstSkillCommitted;
    public bool IsBurstSkillChargeActive => burstSkillChargeActive;
    public bool IsUtilityActive => utilityActive;
    public bool IsAnyChargeActive => burstSkillChargeActive;
    public bool HasActiveUtilityInterruptGuard =>
        utilitySuppressHitInterruptWhileActive
        && utilityInterruptGuardTimer > 0f;
    public bool HasActiveUtilityProtection => utilityProtectionTimer > 0f;
    public bool ShouldNegateIncomingDamage =>
        HasActiveUtilityProtection
        && utilityPreventDamageWhileActive;
    public bool ShouldNegateIncomingKnockback =>
        HasActiveUtilityProtection
        && utilityPreventKnockbackWhileActive;
    public bool ShouldNegateIncomingStun =>
        HasActiveUtilityProtection
        && utilityPreventStunWhileActive;
    public bool ShouldSuppressInterruptingAnimation =>
        currentActionState == PlayerActionStateType.SkillPreCommit
        || burstSkillChargeActive
        || utilityActive;
    public bool ShouldSuppressHitReactionAnimation =>
        currentActionState == PlayerActionStateType.BasicAttack
        || currentActionState == PlayerActionStateType.SkillPreCommit
        || currentActionState == PlayerActionStateType.SkillCommitted
        || currentActionState == PlayerActionStateType.BurstSkillCommitted
        || currentActionState == PlayerActionStateType.Interrupted
        || burstSkillChargeActive
        || HasActiveUtilityInterruptGuard;
    public bool CanContinueCombatWhileStunned =>
        currentActionState == PlayerActionStateType.SkillPreCommit
        || currentActionState == PlayerActionStateType.SkillCommitted
        || currentActionState == PlayerActionStateType.BurstSkillCommitted
        || currentActionState == PlayerActionStateType.Interrupted
        || burstSkillChargeActive
        || utilityActive;
    public bool CanJumpCancelCurrentAction =>
        currentActionState != PlayerActionStateType.BasicAttack
        && currentActionState != PlayerActionStateType.SkillPreCommit
        && !burstSkillChargeActive
        && !utilityActive;
    public bool CanStartJump =>
        currentActionState != PlayerActionStateType.Recovery
        && currentActionState != PlayerActionStateType.Interrupted
        && currentActionState != PlayerActionStateType.BurstSkillCommitted
        && !burstSkillChargeActive
        && !utilityActive;
    public bool CanApplyHorizontalMovement =>
        currentActionState != PlayerActionStateType.BasicAttack
        && currentActionState != PlayerActionStateType.SkillPreCommit
        && currentActionState != PlayerActionStateType.SkillCommitted
        && currentActionState != PlayerActionStateType.BurstSkillCommitted
        && !burstSkillChargeActive
        && !utilityActive;
    public bool CanRotateFromMovementInput => CanApplyHorizontalMovement;
    public bool CanRequestBasicAttack =>
        !burstSkillChargeActive
        && !utilityActive
        && currentActionState != PlayerActionStateType.BurstSkillCommitted
        && (currentActionState == PlayerActionStateType.None
            || currentActionState == PlayerActionStateType.BasicAttack);
    public bool CanStartSkill =>
        !burstSkillChargeActive
        && !utilityActive
        && currentActionState == PlayerActionStateType.None;
    public bool CanStartBurstLinkedSkill =>
        !burstSkillChargeActive
        && !utilityActive
        && currentActionState == PlayerActionStateType.None;
    public bool CanReleaseBurstLinkedSkill => burstSkillChargeActive;
    public bool CanActivateUtilityDuringHold => IsAnyChargeActive;
    public bool CanBeInterruptedByHit =>
        !HasActiveUtilityInterruptGuard
        && (currentActionState == PlayerActionStateType.BasicAttack
            || burstSkillChargeActive
            || utilityActive);
    public bool CanProcessAttackHitFrame => currentActionState != PlayerActionStateType.Interrupted;
    public bool CanProcessComboWindow => currentActionState != PlayerActionStateType.Interrupted;

    public void Initialize(PlayerCharacter ownerCharacter)
    {
        currentActionState = PlayerActionStateType.None;
        actionStateTimer = 0f;
        currentRecoveryDuration = 0f;
        burstSkillChargeActive = false;
        utilityActive = false;
        utilityActivatedDuringCharge = false;
        utilityPreserveHoldCharge = false;
        utilitySuppressHitInterruptWhileActive = false;
        utilityInterruptGuardTimer = 0f;
        utilityProtectionTimer = 0f;
        utilityPreventDamageWhileActive = false;
        utilityPreventKnockbackWhileActive = false;
        utilityPreventStunWhileActive = false;
        utilityRecoveryDuration = 0f;
    }

    private void Update()
    {
        if (utilityInterruptGuardTimer > 0f)
            utilityInterruptGuardTimer = Mathf.Max(0f, utilityInterruptGuardTimer - Time.deltaTime);

        if (utilityProtectionTimer > 0f)
            utilityProtectionTimer = Mathf.Max(0f, utilityProtectionTimer - Time.deltaTime);

        if (currentActionState != PlayerActionStateType.Recovery
            && currentActionState != PlayerActionStateType.Interrupted)
        {
            return;
        }

        actionStateTimer -= Time.deltaTime;
        if (actionStateTimer <= 0f)
            ResetState(clearUtilityProtection: false);
    }

    public void BeginBasicAttack()
    {
        BeginBasicAttack(0f);
    }

    public void BeginBasicAttack(float recoveryDuration)
    {
        currentRecoveryDuration = ResolveRecoveryDuration(
            recoveryDuration,
            fallbackBasicAttackRecoveryDuration);
        SetState(PlayerActionStateType.BasicAttack);
    }

    public void BeginSkillPreCommit()
    {
        BeginSkillPreCommit(0f);
    }

    public void BeginSkillPreCommit(float recoveryDuration)
    {
        currentRecoveryDuration = ResolveRecoveryDuration(
            recoveryDuration,
            fallbackSkillRecoveryDuration);
        SetState(PlayerActionStateType.SkillPreCommit);
    }

    public void CommitSkill()
    {
        SetState(PlayerActionStateType.SkillCommitted);
    }

    public bool CanStartUtilitySkill(UtilitySkillProfile utilityProfile)
    {
        if (utilityActive)
            return false;

        if (currentActionState != PlayerActionStateType.None)
            return false;

        if (!IsAnyChargeActive)
            return true;

        return utilityProfile != null && utilityProfile.CanUseWhileCharging;
    }

    public void BeginUtility(UtilitySkillProfile utilityProfile)
    {
        BeginUtility(utilityProfile, 0f);
    }

    public void BeginUtility(UtilitySkillProfile utilityProfile, float recoveryDuration)
    {
        utilityActive = true;
        utilityActivatedDuringCharge = IsAnyChargeActive;
        utilityPreserveHoldCharge =
            utilityActivatedDuringCharge
            && utilityProfile != null
            && utilityProfile.PreserveHoldCharge;
        utilitySuppressHitInterruptWhileActive =
            utilityProfile != null
            && utilityProfile.SuppressHitInterruptWhileActive;
        utilityInterruptGuardTimer = utilitySuppressHitInterruptWhileActive && utilityProfile != null
            ? Mathf.Max(0f, utilityProfile.StabilityDuration)
            : 0f;
        utilityProtectionTimer = utilityProfile != null
            ? Mathf.Max(0f, utilityProfile.ProtectionDuration)
            : 0f;
        utilityPreventDamageWhileActive =
            utilityProfile != null
            && utilityProfile.PreventDamageWhileActive;
        utilityPreventKnockbackWhileActive =
            utilityProfile != null
            && utilityProfile.PreventKnockbackWhileActive;
        utilityPreventStunWhileActive =
            utilityProfile != null
            && utilityProfile.PreventStunWhileActive;
        utilityRecoveryDuration = ResolveRecoveryDuration(
            recoveryDuration,
            fallbackUtilityRecoveryDuration);
    }

    public void CompleteUtility()
    {
        if (!utilityActive)
            return;

        bool preserveActiveCharge = utilityActivatedDuringCharge && utilityPreserveHoldCharge && IsAnyChargeActive;
        bool shouldCancelActiveCharge = utilityActivatedDuringCharge && !preserveActiveCharge;
        float recoveryDuration = utilityRecoveryDuration;

        ClearUtilityActionState();

        if (shouldCancelActiveCharge)
            burstSkillChargeActive = false;

        if (preserveActiveCharge)
            return;

        EnterTimedState(PlayerActionStateType.Recovery, recoveryDuration);
    }

    public void CancelUtility()
    {
        ClearUtilityState();
    }

    public void BeginBurstSkillCharge()
    {
        burstSkillChargeActive = true;
        actionStateTimer = 0f;
    }

    public void CommitBurstSkillRelease(float recoveryDuration)
    {
        burstSkillChargeActive = false;
        currentRecoveryDuration = ResolveRecoveryDuration(
            recoveryDuration,
            fallbackSkillRecoveryDuration);
        SetState(PlayerActionStateType.BurstSkillCommitted);
    }

    public void CancelBurstSkillCharge()
    {
        burstSkillChargeActive = false;
    }

    public void CompleteCurrentAction()
    {
        switch (currentActionState)
        {
            case PlayerActionStateType.BasicAttack:
            case PlayerActionStateType.SkillPreCommit:
            case PlayerActionStateType.SkillCommitted:
            case PlayerActionStateType.BurstSkillCommitted:
                EnterTimedState(PlayerActionStateType.Recovery, currentRecoveryDuration);
                break;
        }
    }

    public void InterruptCurrentAction()
    {
        if (!CanBeInterruptedByHit)
            return;

        burstSkillChargeActive = false;
        ClearUtilityState();
        EnterTimedState(PlayerActionStateType.Interrupted, interruptedDuration);
    }

    public void ResetState()
    {
        ResetState(clearUtilityProtection: true);
    }

    public void ResetState(bool clearUtilityProtection)
    {
        SetStateToNone(clearUtilityProtection);
    }

    private void EnterTimedState(PlayerActionStateType actionState, float duration)
    {
        currentActionState = actionState;
        actionStateTimer = Mathf.Max(0f, duration);
    }

    private void SetState(PlayerActionStateType actionState)
    {
        if (actionState == PlayerActionStateType.None)
        {
            SetStateToNone(clearUtilityProtection: true);
            return;
        }

        currentActionState = actionState;
        actionStateTimer = 0f;
    }

    private void SetStateToNone(bool clearUtilityProtection)
    {
        currentActionState = PlayerActionStateType.None;
        actionStateTimer = 0f;
        currentRecoveryDuration = 0f;
        utilityRecoveryDuration = 0f;
        burstSkillChargeActive = false;
        ClearUtilityActionState();

        if (clearUtilityProtection)
            ClearUtilityProtectionState();
    }

    private void ClearUtilityState()
    {
        ClearUtilityActionState();
        ClearUtilityProtectionState();
    }

    private void ClearUtilityActionState()
    {
        utilityActive = false;
        utilityActivatedDuringCharge = false;
        utilityPreserveHoldCharge = false;
    }

    private void ClearUtilityProtectionState()
    {
        utilitySuppressHitInterruptWhileActive = false;
        utilityProtectionTimer = 0f;
        utilityInterruptGuardTimer = 0f;
        utilityPreventDamageWhileActive = false;
        utilityPreventKnockbackWhileActive = false;
        utilityPreventStunWhileActive = false;
    }

    private static float ResolveRecoveryDuration(float requestedDuration, float fallbackDuration)
    {
        if (requestedDuration > 0f)
            return requestedDuration;

        return Mathf.Max(0f, fallbackDuration);
    }
}
