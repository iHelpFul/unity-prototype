using UnityEngine;
using System.Collections.Generic;

public enum PlayerBasicAttackSelectionMode
{
    Sequential = 0,
    Random = 1
}

[CreateAssetMenu(menuName = "Game Data/Jobs/Basic Attack Profile")]
public class PlayerBasicAttackProfile : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private PlayerJobType jobType = PlayerJobType.Novice;
    [SerializeField] private string attackId = string.Empty;
    [SerializeField] private string displayName = "New Basic Attack";
    [SerializeField] private CombatAttackFamily attackFamily = CombatAttackFamily.None;
    [SerializeField] private CombatExecutionKind executionKind = CombatExecutionKind.Direct;
    [SerializeField] private CombatTargetingKind targetingKind = CombatTargetingKind.SingleTarget;
    [SerializeField] private PresentationCueSet presentationCueSet;
    [SerializeField] private ProjectileProfile defaultProjectileProfile;
    [SerializeField] private ProjectileLaunchMode projectileLaunchMode = ProjectileLaunchMode.Free;

    [Header("Core Combat")]
    [SerializeField] private float baseRange = 1.5f;
    [SerializeField] private CombatHitBoxDefinition hitBox;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float damageCoefficient = 1f;
    [SerializeField] private float windupTime = 0.15f;
    [SerializeField] private float activeTime = 0.1f;
    [SerializeField] private float recoveryTime = 0.3f;
    [SerializeField] private bool hasTimingWindow;
    [SerializeField] private float timingWindowStart = 0.1f;
    [SerializeField] private float timingWindowEnd = 0.2f;
    [SerializeField] private int momentumGainOnValidHit = 1;
    [SerializeField] private float baseBreakPower;
    [SerializeField] private float baseSurgeChanceBonus;
    [SerializeField] private float baseSurgePowerBonus;
    [SerializeField] private CombatElementType defaultElement = CombatElementType.None;

    [Header("Gauge Builder")]
    [SerializeField] private bool buildsGauge = true;
    [SerializeField] private bool flowStacksRequireValidHit = true;
    [SerializeField] private bool resetAllFlowStacksOnTimeout = true;
    [SerializeField] private bool resetFlowStacksOnMaxReached;
    [SerializeField] private int maxFlowStacks = 3;
    [SerializeField] private float firstFlowWindowDuration = 3f;
    [SerializeField] private float chainedFlowWindowDuration = 2f;
    [SerializeField] private float gaugeGainOnValidHit = 1f;
    [SerializeField] private float gaugeGainOnChainFinisher = 2f;
    [SerializeField] private AnimationCurve gaugeGainMultiplierByFlowStack =
        AnimationCurve.Linear(0f, 1f, 3f, 1.75f);
    [SerializeField] private float aerialGaugeGainMultiplier = 0.75f;

    [Header("Flow Completion")]
    [SerializeField] private EmpoweredBasicDefinition empoweredBasic;

    [Header("Legacy Combat Loop")]
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

    [Header("Action Animation")]
    [SerializeField] private string[] animatorStateNames = new string[0];

    public PlayerJobType JobType => jobType;
    public string AttackId => attackId;
    public string DisplayName => displayName;
    public CombatAttackFamily AttackFamily => attackFamily;
    public CombatExecutionKind ExecutionKind => executionKind;
    public CombatTargetingKind TargetingKind => targetingKind;
    public PresentationCueSet PresentationCueSet => presentationCueSet;
    public ProjectileProfile DefaultProjectileProfile => defaultProjectileProfile;
    public ProjectileLaunchMode ProjectileLaunchMode => projectileLaunchMode;
    public float BaseRange => baseRange;
    public CombatHitBoxDefinition HitBox => hitBox.GetSanitized();
    public int HitCount => hitCount;
    public float DamageCoefficient => damageCoefficient;
    public float WindupTime => windupTime;
    public float ActiveTime => activeTime;
    public float RecoveryTime => recoveryTime;
    public bool HasTimingWindow => hasTimingWindow;
    public float TimingWindowStart => timingWindowStart;
    public float TimingWindowEnd => timingWindowEnd;
    public int MomentumGainOnValidHit => momentumGainOnValidHit;
    public float BaseBreakPower => baseBreakPower;
    public float BaseSurgeChanceBonus => baseSurgeChanceBonus;
    public float BaseSurgePowerBonus => baseSurgePowerBonus;
    public CombatElementType DefaultElement => defaultElement;
    public bool BuildsGauge => buildsGauge;
    public bool FlowStacksRequireValidHit => flowStacksRequireValidHit;
    public bool ResetAllFlowStacksOnTimeout => resetAllFlowStacksOnTimeout;
    public bool ResetFlowStacksOnMaxReached => resetFlowStacksOnMaxReached;
    public int MaxFlowStacks => maxFlowStacks;
    public float FirstFlowWindowDuration => firstFlowWindowDuration;
    public float ChainedFlowWindowDuration => chainedFlowWindowDuration;
    public float GaugeGainOnValidHit => gaugeGainOnValidHit;
    public float GaugeGainOnChainFinisher => gaugeGainOnChainFinisher;
    public AnimationCurve GaugeGainMultiplierByFlowStack => gaugeGainMultiplierByFlowStack;
    public float AerialGaugeGainMultiplier => aerialGaugeGainMultiplier;
    public EmpoweredBasicDefinition EmpoweredBasic => empoweredBasic.GetSanitized();
    public int AnimationVariantCount => animationVariantCount;
    public int ResolvedAnimationVariantCount => Mathf.Max(1, CountConfiguredAnimatorStateNames());
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
    public IReadOnlyList<string> AnimatorStateNames => animatorStateNames;

    public bool HasAnimatorStateOverrides => CountConfiguredAnimatorStateNames() > 0;

    public string GetAnimatorStateName(int animationVariantIndex)
    {
        if (animatorStateNames == null || animatorStateNames.Length == 0)
            return string.Empty;

        List<string> configuredStateNames = new List<string>();
        for (int index = 0; index < animatorStateNames.Length; index++)
        {
            string stateName = NormalizeStateName(animatorStateNames[index]);
            if (!string.IsNullOrWhiteSpace(stateName))
                configuredStateNames.Add(stateName);
        }

        if (configuredStateNames.Count == 0)
            return string.Empty;

        int resolvedIndex = Mathf.Clamp(animationVariantIndex - 1, 0, configuredStateNames.Count - 1);
        return configuredStateNames[resolvedIndex];
    }

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
        hitBox = hitBox.GetSanitized();
        hitCount = Mathf.Max(1, hitCount);
        damageCoefficient = Mathf.Max(0.05f, damageCoefficient);
        windupTime = Mathf.Max(0f, windupTime);
        activeTime = Mathf.Max(0f, activeTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        timingWindowStart = Mathf.Max(0f, timingWindowStart);
        timingWindowEnd = Mathf.Max(timingWindowStart, timingWindowEnd);
        momentumGainOnValidHit = Mathf.Max(0, momentumGainOnValidHit);
        baseBreakPower = Mathf.Max(0f, baseBreakPower);
        baseSurgeChanceBonus = Mathf.Max(0f, baseSurgeChanceBonus);
        baseSurgePowerBonus = Mathf.Max(0f, baseSurgePowerBonus);
        maxFlowStacks = Mathf.Max(1, maxFlowStacks);
        firstFlowWindowDuration = Mathf.Max(0.05f, firstFlowWindowDuration);
        chainedFlowWindowDuration = Mathf.Max(0.05f, chainedFlowWindowDuration);
        gaugeGainOnValidHit = Mathf.Max(0f, gaugeGainOnValidHit);
        gaugeGainOnChainFinisher = Mathf.Max(0f, gaugeGainOnChainFinisher);
        aerialGaugeGainMultiplier = Mathf.Max(0f, aerialGaugeGainMultiplier);
        gaugeGainMultiplierByFlowStack ??= AnimationCurve.Linear(0f, 1f, 3f, 1.75f);
        empoweredBasic = empoweredBasic.GetSanitized();
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
        animatorStateNames ??= new string[0];

        if (!supportsComboCounter)
        {
            maxComboCounter = 0;
            comboResetDelay = 0f;
            comboDamageBonusPerStack = 0f;
        }

        for (int index = 0; index < animatorStateNames.Length; index++)
            animatorStateNames[index] = NormalizeStateName(animatorStateNames[index]);
    }

    private int CountConfiguredAnimatorStateNames()
    {
        if (animatorStateNames == null || animatorStateNames.Length == 0)
            return 0;

        int configuredCount = 0;
        for (int index = 0; index < animatorStateNames.Length; index++)
        {
            if (!string.IsNullOrWhiteSpace(NormalizeStateName(animatorStateNames[index])))
                configuredCount++;
        }

        return configuredCount;
    }

    private static string NormalizeStateName(string stateName)
    {
        return string.IsNullOrWhiteSpace(stateName) ? string.Empty : stateName.Trim();
    }
}

