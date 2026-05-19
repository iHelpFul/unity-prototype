using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Skills/Burst Linked Skill Profile")]
public class BurstLinkedSkillProfile : ScriptableObject
{
    [Header("Gauge")]
    [SerializeField] private float minimumGaugeToStart = 1f;
    [SerializeField] private bool allowPartialGaugeSpend = true;
    [SerializeField] private GaugeScalingProfile gaugeScalingProfile;
    [SerializeField] private CombatHitBoxDefinition chargedHitBox;

    [Header("Hold Behavior")]
    [SerializeField] private float tapReleaseThreshold = 0.12f;
    [SerializeField] private float maxHoldDuration = 1.5f;
    [SerializeField] private bool autoReleaseAtMaxHold = true;

    [Header("Animation")]
    [SerializeField] private string chargeEntryAnimatorStateName = string.Empty;
    [SerializeField] private string chargeAnimatorStateName = string.Empty;
    [SerializeField] private string releaseAnimatorStateName = string.Empty;

    [Header("Aerial")]
    [SerializeField] private bool allowAerialBurst = true;
    [SerializeField] private float aerialMaxHoldDurationMultiplier = 0.7f;
    [SerializeField] private float aerialGaugeSpendMultiplier = 0.85f;
    [SerializeField] private float aerialDamageMultiplier = 0.9f;

    [Header("Interrupt")]
    [SerializeField] private bool interruptedByValidHit = true;
    [SerializeField] private bool loseLockedGaugeOnInterrupt = true;

    public float MinimumGaugeToStart => minimumGaugeToStart;
    public bool AllowPartialGaugeSpend => allowPartialGaugeSpend;
    public GaugeScalingProfile GaugeScalingProfile => gaugeScalingProfile;
    public CombatHitBoxDefinition ChargedHitBox => chargedHitBox.GetSanitized();
    public float TapReleaseThreshold => tapReleaseThreshold;
    public float MaxHoldDuration => maxHoldDuration;
    public bool AutoReleaseAtMaxHold => autoReleaseAtMaxHold;
    public string ChargeEntryAnimatorStateName => chargeEntryAnimatorStateName;
    public string ChargeAnimatorStateName => chargeAnimatorStateName;
    public string ReleaseAnimatorStateName => releaseAnimatorStateName;
    public bool AllowAerialBurst => allowAerialBurst;
    public float AerialMaxHoldDurationMultiplier => aerialMaxHoldDurationMultiplier;
    public float AerialGaugeSpendMultiplier => aerialGaugeSpendMultiplier;
    public float AerialDamageMultiplier => aerialDamageMultiplier;
    public bool InterruptedByValidHit => interruptedByValidHit;
    public bool LoseLockedGaugeOnInterrupt => loseLockedGaugeOnInterrupt;

    private void OnValidate()
    {
        minimumGaugeToStart = Mathf.Max(0f, minimumGaugeToStart);
        chargedHitBox = chargedHitBox.GetSanitized();
        tapReleaseThreshold = Mathf.Max(0f, tapReleaseThreshold);
        maxHoldDuration = Mathf.Max(0.05f, maxHoldDuration);
        chargeEntryAnimatorStateName = string.IsNullOrWhiteSpace(chargeEntryAnimatorStateName)
            ? string.Empty
            : chargeEntryAnimatorStateName.Trim();
        chargeAnimatorStateName = string.IsNullOrWhiteSpace(chargeAnimatorStateName)
            ? string.Empty
            : chargeAnimatorStateName.Trim();
        releaseAnimatorStateName = string.IsNullOrWhiteSpace(releaseAnimatorStateName)
            ? string.Empty
            : releaseAnimatorStateName.Trim();
        aerialMaxHoldDurationMultiplier = Mathf.Max(0f, aerialMaxHoldDurationMultiplier);
        aerialGaugeSpendMultiplier = Mathf.Max(0f, aerialGaugeSpendMultiplier);
        aerialDamageMultiplier = Mathf.Max(0f, aerialDamageMultiplier);
    }
}
