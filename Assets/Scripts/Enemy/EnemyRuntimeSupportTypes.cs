using UnityEngine;

public enum EnemyRole
{
    None,
    Skirmisher,
    Bruiser,
    Sentinel
}

[System.Serializable]
public struct EnemyCadenceProfile
{
    [SerializeField] private EnemyAttackType attackType;
    [SerializeField, Min(0.05f)] private float moveSpeed;
    [SerializeField, Min(0.1f)] private float detectionRange;
    [SerializeField, Min(0.1f)] private float loseAggroDelay;
    [SerializeField, Min(0f)] private float chaseBoundsPadding;
    [SerializeField, Min(0.05f)] private float contactDamageCooldown;
    [SerializeField, Min(0f)] private float attackCooldown;
    [SerializeField, Min(0f)] private float attackRecoveryDuration;

    public EnemyAttackType AttackType => attackType;
    public float MoveSpeed => moveSpeed > 0f ? moveSpeed : 2f;
    public float DetectionRange => detectionRange > 0f ? detectionRange : 8f;
    public float LoseAggroDelay => loseAggroDelay > 0f ? loseAggroDelay : 2.25f;
    public float ChaseBoundsPadding => chaseBoundsPadding >= 0f ? chaseBoundsPadding : 1.2f;
    public float ContactDamageCooldown => contactDamageCooldown > 0f ? contactDamageCooldown : 1f;
    public float AttackCooldown => attackCooldown >= 0f ? attackCooldown : 1.2f;
    public float AttackRecoveryDuration => attackRecoveryDuration >= 0f ? attackRecoveryDuration : 0.18f;

    public EnemyCadenceProfile Sanitize()
    {
        EnemyCadenceProfile sanitized = this;
        sanitized.moveSpeed = Mathf.Max(0.05f, MoveSpeed);
        sanitized.detectionRange = Mathf.Max(0.1f, DetectionRange);
        sanitized.loseAggroDelay = Mathf.Max(0.1f, LoseAggroDelay);
        sanitized.chaseBoundsPadding = Mathf.Max(0f, ChaseBoundsPadding);
        sanitized.contactDamageCooldown = Mathf.Max(0.05f, ContactDamageCooldown);
        sanitized.attackCooldown = Mathf.Max(0f, AttackCooldown);
        sanitized.attackRecoveryDuration = Mathf.Max(0f, AttackRecoveryDuration);
        return sanitized;
    }
}

[System.Serializable]
public struct EnemyPoiseProfile
{
    [Tooltip("Set 0 to keep the old immediate-break behavior. Set above 0 to enable pressure buildup before a break.")]
    [SerializeField, Min(0f)] private float poiseThreshold;
    [SerializeField, Min(0f)] private float pressureDecayDelay;
    [SerializeField, Min(0f)] private float pressureDecayPerSecond;
    [SerializeField, Min(0f)] private float baseBreakDuration;
    [SerializeField, Min(0f)] private float extraDurationPerBreakPower;
    [SerializeField, Min(0.01f)] private float minimumBreakDuration;
    [SerializeField, Min(0.01f)] private float pressureTakenMultiplier;

    public float PoiseThreshold => Mathf.Max(0f, poiseThreshold);
    public float PressureDecayDelay => pressureDecayDelay > 0f ? pressureDecayDelay : 0.9f;
    public float PressureDecayPerSecond => pressureDecayPerSecond > 0f ? pressureDecayPerSecond : 0.85f;
    public float BaseBreakDuration => baseBreakDuration >= 0f ? baseBreakDuration : 0.35f;
    public float ExtraDurationPerBreakPower => extraDurationPerBreakPower >= 0f ? extraDurationPerBreakPower : 0.2f;
    public float MinimumBreakDuration => minimumBreakDuration > 0f ? minimumBreakDuration : 0.15f;
    public float PressureTakenMultiplier => pressureTakenMultiplier > 0f ? pressureTakenMultiplier : 1f;

    public EnemyPoiseProfile Sanitize()
    {
        EnemyPoiseProfile sanitized = this;
        sanitized.poiseThreshold = PoiseThreshold;
        sanitized.pressureDecayDelay = Mathf.Max(0f, PressureDecayDelay);
        sanitized.pressureDecayPerSecond = Mathf.Max(0f, PressureDecayPerSecond);
        sanitized.baseBreakDuration = Mathf.Max(0f, BaseBreakDuration);
        sanitized.extraDurationPerBreakPower = Mathf.Max(0f, ExtraDurationPerBreakPower);
        sanitized.minimumBreakDuration = Mathf.Max(0.01f, MinimumBreakDuration);
        sanitized.pressureTakenMultiplier = Mathf.Max(0.01f, PressureTakenMultiplier);
        return sanitized;
    }

    public EnemyPoiseProfile WithAdjustedThreshold(float multiplier)
    {
        EnemyPoiseProfile adjusted = this;
        adjusted.poiseThreshold = PoiseThreshold > 0f
            ? Mathf.Max(0f, PoiseThreshold * Mathf.Max(0.1f, multiplier))
            : 0f;
        return adjusted.Sanitize();
    }
}

[System.Serializable]
public struct EnemyEliteProfile
{
    [SerializeField] private bool isElite;
    [SerializeField, Min(1f)] private float maxHpMultiplier;
    [SerializeField, Min(0)] private int defenseBonus;
    [SerializeField, Min(0.1f)] private float contactDamageMultiplier;
    [SerializeField, Min(0.1f)] private float animatedAttackDamageMultiplier;
    [SerializeField, Min(0.1f)] private float expRewardMultiplier;
    [SerializeField, Min(0.1f)] private float poiseThresholdMultiplier;

    public bool IsElite => isElite;
    public float MaxHpMultiplier => maxHpMultiplier > 0f ? maxHpMultiplier : 1.6f;
    public int DefenseBonus => Mathf.Max(0, defenseBonus);
    public float ContactDamageMultiplier => contactDamageMultiplier > 0f ? contactDamageMultiplier : 1.25f;
    public float AnimatedAttackDamageMultiplier => animatedAttackDamageMultiplier > 0f ? animatedAttackDamageMultiplier : 1.35f;
    public float ExpRewardMultiplier => expRewardMultiplier > 0f ? expRewardMultiplier : 1.75f;
    public float PoiseThresholdMultiplier => poiseThresholdMultiplier > 0f ? poiseThresholdMultiplier : 1.35f;

    public EnemyEliteProfile Sanitize()
    {
        EnemyEliteProfile sanitized = this;
        sanitized.maxHpMultiplier = Mathf.Max(1f, MaxHpMultiplier);
        sanitized.defenseBonus = Mathf.Max(0, DefenseBonus);
        sanitized.contactDamageMultiplier = Mathf.Max(0.1f, ContactDamageMultiplier);
        sanitized.animatedAttackDamageMultiplier = Mathf.Max(0.1f, AnimatedAttackDamageMultiplier);
        sanitized.expRewardMultiplier = Mathf.Max(0.1f, ExpRewardMultiplier);
        sanitized.poiseThresholdMultiplier = Mathf.Max(0.1f, PoiseThresholdMultiplier);
        return sanitized;
    }
}
