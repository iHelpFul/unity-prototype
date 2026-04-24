using UnityEngine;

public enum PlayerBasicAttackSelectionMode
{
    Sequential = 0,
    Random = 1
}

[CreateAssetMenu(menuName = "Game Data/Jobs/Basic Attack Profile")]
public class PlayerBasicAttackProfile : ScriptableObject
{
    [SerializeField] private PlayerJobType jobType = PlayerJobType.Drifter;
    [SerializeField] private string attackId = string.Empty;
    [SerializeField] private string displayName = "New Basic Attack";
    [SerializeField] private CombatAttackFamily attackFamily = CombatAttackFamily.None;
    [SerializeField] private CombatExecutionKind executionKind = CombatExecutionKind.Melee;
    [SerializeField] private CombatTargetingKind targetingKind = CombatTargetingKind.SingleTarget;
    [SerializeField] private PresentationCueSet presentationCueSet;
    [SerializeField] private ProjectileProfile defaultProjectileProfile;
    [SerializeField] private float baseRange = 1.5f;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float damageCoefficient = 1f;
    [SerializeField] private float windupTime = 0.15f;
    [SerializeField] private float activeTime = 0.1f;
    [SerializeField] private float recoveryTime = 0.3f;
    [SerializeField] private bool hasTimingWindow;
    [SerializeField] private float timingWindowStart = 0.1f;
    [SerializeField] private float timingWindowEnd = 0.2f;
    [SerializeField] private int momentumGainOnValidHit = 1;
    [SerializeField] private float baseSurgeChanceBonus;
    [SerializeField] private float baseSurgePowerBonus;
    [SerializeField] private CombatElementType defaultElement = CombatElementType.None;
    [SerializeField] private int animationVariantCount = 1;
    [SerializeField] private int maxChainCount = 1;
    [SerializeField] private PlayerBasicAttackSelectionMode selectionMode = PlayerBasicAttackSelectionMode.Sequential;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float maxAttackDuration = 1f;
    [SerializeField] private float attackAnimationSpeed = 1f;
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private float basicDamageMultiplier = 1f;
    [SerializeField] private int maxComboCounter;
    [SerializeField] private float comboResetDelay;
    [SerializeField] private float comboDamageBonusPerStack;
    [SerializeField] private bool supportsComboCounter;

    public PlayerJobType JobType => jobType;
    public string AttackId => attackId;
    public string DisplayName => displayName;
    public CombatAttackFamily AttackFamily => attackFamily;
    public CombatExecutionKind ExecutionKind => executionKind;
    public CombatTargetingKind TargetingKind => targetingKind;
    public PresentationCueSet PresentationCueSet => presentationCueSet;
    public ProjectileProfile DefaultProjectileProfile => defaultProjectileProfile;
    public float BaseRange => baseRange;
    public int HitCount => hitCount;
    public float DamageCoefficient => damageCoefficient;
    public float WindupTime => windupTime;
    public float ActiveTime => activeTime;
    public float RecoveryTime => recoveryTime;
    public bool HasTimingWindow => hasTimingWindow;
    public float TimingWindowStart => timingWindowStart;
    public float TimingWindowEnd => timingWindowEnd;
    public int MomentumGainOnValidHit => momentumGainOnValidHit;
    public float BaseSurgeChanceBonus => baseSurgeChanceBonus;
    public float BaseSurgePowerBonus => baseSurgePowerBonus;
    public CombatElementType DefaultElement => defaultElement;
    public int AnimationVariantCount => animationVariantCount;
    public int MaxChainCount => maxChainCount;
    public PlayerBasicAttackSelectionMode SelectionMode => selectionMode;
    public float AttackCooldown => attackCooldown;
    public float MaxAttackDuration => maxAttackDuration;
    public float AttackAnimationSpeed => attackAnimationSpeed;
    public int MaxTargets => maxTargets;
    public float BasicDamageMultiplier => basicDamageMultiplier;
    public int MaxComboCounter => maxComboCounter;
    public float ComboResetDelay => comboResetDelay;
    public float ComboDamageBonusPerStack => comboDamageBonusPerStack;
    public bool SupportsComboCounter => supportsComboCounter;

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        attackId = string.IsNullOrWhiteSpace(attackId)
            ? jobType.ToString().ToLowerInvariant() + "_basic"
            : attackId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? jobType.ToString() + " Basic Attack" : displayName.Trim();
        baseRange = Mathf.Max(0f, baseRange);
        hitCount = Mathf.Max(1, hitCount);
        damageCoefficient = Mathf.Max(0.05f, damageCoefficient);
        windupTime = Mathf.Max(0f, windupTime);
        activeTime = Mathf.Max(0f, activeTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        timingWindowStart = Mathf.Max(0f, timingWindowStart);
        timingWindowEnd = Mathf.Max(timingWindowStart, timingWindowEnd);
        momentumGainOnValidHit = Mathf.Max(0, momentumGainOnValidHit);
        baseSurgeChanceBonus = Mathf.Max(0f, baseSurgeChanceBonus);
        baseSurgePowerBonus = Mathf.Max(0f, baseSurgePowerBonus);
        animationVariantCount = Mathf.Max(1, animationVariantCount);
        maxChainCount = Mathf.Max(1, maxChainCount);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        maxAttackDuration = Mathf.Max(0.05f, maxAttackDuration);
        attackAnimationSpeed = Mathf.Max(0.05f, attackAnimationSpeed);
        maxTargets = Mathf.Max(1, maxTargets);
        basicDamageMultiplier = Mathf.Max(0.05f, basicDamageMultiplier);
        maxComboCounter = Mathf.Max(0, maxComboCounter);
        comboResetDelay = Mathf.Max(0f, comboResetDelay);
        comboDamageBonusPerStack = Mathf.Max(0f, comboDamageBonusPerStack);

        if (!supportsComboCounter)
        {
            maxComboCounter = 0;
            comboResetDelay = 0f;
            comboDamageBonusPerStack = 0f;
        }
    }
}

