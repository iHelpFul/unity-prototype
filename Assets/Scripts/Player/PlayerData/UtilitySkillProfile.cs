using UnityEngine;

[System.Serializable]
public enum UtilityRepositionMode
{
    None = 0,
    BackwardOnly = 1,
    ForwardOnly = 2,
    InputRelative = 3
}

[CreateAssetMenu(menuName = "Game Data/Skills/Utility Skill Profile")]
public class UtilitySkillProfile : ScriptableObject
{
    [Header("Flow")]
    [SerializeField] private bool refreshFlowTimer = true;
    [SerializeField] private bool refreshFlowOnlyWhenStacksActive = true;

    [Header("Ready State")]
    [SerializeField] private PlayerReadyStateType grantedReadyStateType = PlayerReadyStateType.None;
    [SerializeField] private float grantedReadyStateDuration;

    [Header("Charge Interaction")]
    [SerializeField] private bool canUseWhileCharging = true;
    [SerializeField] private bool preserveHoldCharge = true;

    [Header("Stability")]
    [SerializeField] private float stabilityDuration = 0.2f;
    [SerializeField] private bool suppressHitInterruptWhileActive;

    [Header("Defensive Window")]
    [SerializeField] private float protectionDuration = 0.2f;
    [SerializeField] private bool preventDamageWhileActive = true;
    [SerializeField] private bool preventKnockbackWhileActive = true;
    [SerializeField] private bool preventStunWhileActive = true;

    [Header("Reposition")]
    [SerializeField] private UtilityRepositionMode repositionMode = UtilityRepositionMode.None;
    [SerializeField] private float repositionDistance;
    [SerializeField] private float repositionDuration = 0.12f;

    public bool RefreshFlowTimer => refreshFlowTimer;
    public bool RefreshFlowOnlyWhenStacksActive => refreshFlowOnlyWhenStacksActive;
    public PlayerReadyStateType GrantedReadyStateType => grantedReadyStateType;
    public float GrantedReadyStateDuration => Mathf.Max(0f, grantedReadyStateDuration);
    public bool GrantsReadyState => grantedReadyStateType != PlayerReadyStateType.None && GrantedReadyStateDuration > 0f;
    public bool CanUseWhileCharging => canUseWhileCharging;
    public bool PreserveHoldCharge => preserveHoldCharge;
    public float StabilityDuration => stabilityDuration;
    public bool SuppressHitInterruptWhileActive => suppressHitInterruptWhileActive;
    public float ProtectionDuration => protectionDuration;
    public bool PreventDamageWhileActive => preventDamageWhileActive;
    public bool PreventKnockbackWhileActive => preventKnockbackWhileActive;
    public bool PreventStunWhileActive => preventStunWhileActive;
    public UtilityRepositionMode RepositionMode => repositionMode;
    public float RepositionDistance => repositionDistance;
    public float RepositionDuration => repositionDuration;
    public bool HasReposition => repositionMode != UtilityRepositionMode.None && repositionDistance > 0f;

    private void OnValidate()
    {
        if (!System.Enum.IsDefined(typeof(PlayerReadyStateType), grantedReadyStateType))
            grantedReadyStateType = PlayerReadyStateType.None;

        grantedReadyStateDuration = Mathf.Max(0f, grantedReadyStateDuration);
        stabilityDuration = Mathf.Max(0f, stabilityDuration);
        protectionDuration = Mathf.Max(0f, protectionDuration);
        repositionDistance = Mathf.Max(0f, repositionDistance);
        repositionDuration = Mathf.Max(0.01f, repositionDuration);
    }
}
