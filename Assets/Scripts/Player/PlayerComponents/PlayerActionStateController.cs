using UnityEngine;

public class PlayerActionStateController : MonoBehaviour
{
    [Header("Action State Durations")]
    [SerializeField] private float fallbackBasicAttackRecoveryDuration = 0.06f;
    [SerializeField] private float fallbackSkillRecoveryDuration = 0.08f;
    [SerializeField] private float interruptedDuration = 0.12f;

    private PlayerActionStateType currentActionState;
    private float actionStateTimer;
    private float currentRecoveryDuration;

    public PlayerActionStateType CurrentActionState => currentActionState;
    public bool HasPendingSkillCommit => currentActionState == PlayerActionStateType.SkillPreCommit;
    public bool IsSkillCommitted => currentActionState == PlayerActionStateType.SkillCommitted;
    public bool ShouldSuppressInterruptingAnimation => currentActionState == PlayerActionStateType.SkillPreCommit;
    public bool ShouldSuppressHitReactionAnimation =>
        currentActionState == PlayerActionStateType.BasicAttack
        || currentActionState == PlayerActionStateType.SkillPreCommit
        || currentActionState == PlayerActionStateType.SkillCommitted
        || currentActionState == PlayerActionStateType.Interrupted;
    public bool CanContinueCombatWhileStunned =>
        currentActionState == PlayerActionStateType.SkillPreCommit
        || currentActionState == PlayerActionStateType.SkillCommitted
        || currentActionState == PlayerActionStateType.Interrupted;
    public bool CanJumpCancelCurrentAction => currentActionState != PlayerActionStateType.SkillPreCommit;
    public bool CanStartJump =>
        currentActionState != PlayerActionStateType.Recovery
        && currentActionState != PlayerActionStateType.Interrupted;
    public bool CanApplyHorizontalMovement =>
        currentActionState != PlayerActionStateType.BasicAttack
        && currentActionState != PlayerActionStateType.SkillPreCommit
        && currentActionState != PlayerActionStateType.SkillCommitted;
    public bool CanRotateFromMovementInput => CanApplyHorizontalMovement;
    public bool CanRequestBasicAttack =>
        currentActionState == PlayerActionStateType.None
        || currentActionState == PlayerActionStateType.BasicAttack;
    public bool CanStartSkill => currentActionState == PlayerActionStateType.None;
    public bool CanBeInterruptedByHit => currentActionState == PlayerActionStateType.BasicAttack;
    public bool CanProcessAttackHitFrame => currentActionState != PlayerActionStateType.Interrupted;
    public bool CanProcessComboWindow => currentActionState != PlayerActionStateType.Interrupted;

    public void Initialize(PlayerCharacter ownerCharacter)
    {
        currentActionState = PlayerActionStateType.None;
        actionStateTimer = 0f;
        currentRecoveryDuration = 0f;
    }

    private void Update()
    {
        if (currentActionState != PlayerActionStateType.Recovery
            && currentActionState != PlayerActionStateType.Interrupted)
        {
            return;
        }

        actionStateTimer -= Time.deltaTime;
        if (actionStateTimer <= 0f)
            ResetState();
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

    public void CompleteCurrentAction()
    {
        switch (currentActionState)
        {
            case PlayerActionStateType.BasicAttack:
                EnterTimedState(PlayerActionStateType.Recovery, currentRecoveryDuration);
                break;

            case PlayerActionStateType.SkillPreCommit:
            case PlayerActionStateType.SkillCommitted:
                EnterTimedState(PlayerActionStateType.Recovery, currentRecoveryDuration);
                break;

            case PlayerActionStateType.Recovery:
            case PlayerActionStateType.Interrupted:
            case PlayerActionStateType.None:
            default:
                break;
        }
    }

    public void InterruptCurrentAction()
    {
        if (!CanBeInterruptedByHit)
            return;

        EnterTimedState(PlayerActionStateType.Interrupted, interruptedDuration);
    }

    public void ResetState()
    {
        SetState(PlayerActionStateType.None);
    }

    private void EnterTimedState(PlayerActionStateType actionState, float duration)
    {
        currentActionState = actionState;
        actionStateTimer = Mathf.Max(0f, duration);
    }

    private void SetState(PlayerActionStateType actionState)
    {
        currentActionState = actionState;
        actionStateTimer = 0f;

        if (actionState == PlayerActionStateType.None)
            currentRecoveryDuration = 0f;
    }

    private static float ResolveRecoveryDuration(float requestedDuration, float fallbackDuration)
    {
        if (requestedDuration > 0f)
            return requestedDuration;

        return Mathf.Max(0f, fallbackDuration);
    }
}
