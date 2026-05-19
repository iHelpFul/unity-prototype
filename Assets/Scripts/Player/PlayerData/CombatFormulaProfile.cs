using UnityEngine;

[System.Serializable]
public class CombatStatContributionBlock
{
    public float MightWeight = 1f;
    public float PrecisionWeight = 1f;
    public float ArcaneWeight = 1f;
    public float FinesseWeight = 1f;
    public float HitRateWeight = 1f;

    public void Sanitize()
    {
        MightWeight = Mathf.Max(0f, MightWeight);
        PrecisionWeight = Mathf.Max(0f, PrecisionWeight);
        ArcaneWeight = Mathf.Max(0f, ArcaneWeight);
        FinesseWeight = Mathf.Max(0f, FinesseWeight);
        HitRateWeight = Mathf.Max(0f, HitRateWeight);
    }
}

[System.Serializable]
public class CombatDerivedStatRuleBlock
{
    public float AvoidanceFromFinesseWeight = 1f;
    public float DefenseFromMightWeight = 1f;
    public float SurgeChanceFromPrecisionWeight = 1f;
    public float SurgePowerFromArcaneWeight = 1f;

    public void Sanitize()
    {
        AvoidanceFromFinesseWeight = Mathf.Max(0f, AvoidanceFromFinesseWeight);
        DefenseFromMightWeight = Mathf.Max(0f, DefenseFromMightWeight);
        SurgeChanceFromPrecisionWeight = Mathf.Max(0f, SurgeChanceFromPrecisionWeight);
        SurgePowerFromArcaneWeight = Mathf.Max(0f, SurgePowerFromArcaneWeight);
    }
}

[System.Serializable]
public class CombatHitRuleBlock
{
    [Tooltip("When enabled, enemies with 0 Avoidance cannot Evade.")]
    public bool GuaranteedHitWhenTargetHasNoAvoidance = true;
    [Tooltip("Starting hit chance before HitRate/Avoidance advantage is applied.")]
    public float BaseHitChance = 0.9f;
    public float HitRateWeight = 1f;
    public float AvoidanceWeight = 1f;
    public float LevelDeltaWeight = 0.1f;
    [Tooltip("Higher values make HitRate/Avoidance differences less extreme.")]
    public float HitAdvantageDivisor = 100f;
    public float MinHitChanceClamp = 0.1f;
    public float MaxHitChanceClamp = 1f;

    public void Sanitize()
    {
        BaseHitChance = Mathf.Clamp01(BaseHitChance);
        HitRateWeight = Mathf.Max(0f, HitRateWeight);
        AvoidanceWeight = Mathf.Max(0f, AvoidanceWeight);
        LevelDeltaWeight = Mathf.Max(0f, LevelDeltaWeight);
        HitAdvantageDivisor = Mathf.Max(1f, HitAdvantageDivisor);
        MinHitChanceClamp = Mathf.Clamp01(MinHitChanceClamp);
        MaxHitChanceClamp = Mathf.Clamp(MaxHitChanceClamp, MinHitChanceClamp, 1f);
    }
}

[System.Serializable]
public class CombatDamageRuleBlock
{
    public float BaseVarianceMin = 0.9f;
    public float BaseVarianceMax = 1.1f;
    public float WeaponPowerWeight = 1f;
    public float DamageCoefficientWeight = 1f;
    public float DefenseMitigationWeight = 1f;
    public float MinimumDamageFloor = 1f;

    public void Sanitize()
    {
        BaseVarianceMin = Mathf.Max(0f, BaseVarianceMin);
        BaseVarianceMax = Mathf.Max(BaseVarianceMin, BaseVarianceMax);
        WeaponPowerWeight = Mathf.Max(0f, WeaponPowerWeight);
        DamageCoefficientWeight = Mathf.Max(0f, DamageCoefficientWeight);
        DefenseMitigationWeight = Mathf.Max(0f, DefenseMitigationWeight);
        MinimumDamageFloor = Mathf.Max(1f, MinimumDamageFloor);
    }
}

[System.Serializable]
public class CombatSurgeRuleBlock
{
    public float MinSurgeChanceClamp;
    public float MaxSurgeChanceClamp = 100f;
    public float MinSurgePowerClamp = 1f;
    public float MaxSurgePowerClamp = 5f;

    public void Sanitize()
    {
        MinSurgeChanceClamp = Mathf.Clamp(MinSurgeChanceClamp, 0f, 100f);
        MaxSurgeChanceClamp = Mathf.Clamp(MaxSurgeChanceClamp, MinSurgeChanceClamp, 100f);
        MinSurgePowerClamp = Mathf.Max(0f, MinSurgePowerClamp);
        MaxSurgePowerClamp = Mathf.Max(MinSurgePowerClamp, MaxSurgePowerClamp);
    }
}

[System.Serializable]
public class CombatMomentumRuleBlock
{
    public float DamageBonusPerStack;
    public float SurgeChanceBonusPerStack;
    public float SurgePowerBonusPerStack;
    public float RangeBonusPerStack;

    public void Sanitize()
    {
        DamageBonusPerStack = Mathf.Max(0f, DamageBonusPerStack);
        SurgeChanceBonusPerStack = Mathf.Max(0f, SurgeChanceBonusPerStack);
        SurgePowerBonusPerStack = Mathf.Max(0f, SurgePowerBonusPerStack);
        RangeBonusPerStack = Mathf.Max(0f, RangeBonusPerStack);
    }
}

[CreateAssetMenu(menuName = "Game Data/Combat/Combat Formula Profile")]
public class CombatFormulaProfile : ScriptableObject
{
    [SerializeField] private string profileId = string.Empty;
    [SerializeField] private string displayName = "New Combat Formula Profile";
    [SerializeField] private CombatStatContributionBlock statContributions = new CombatStatContributionBlock();
    [SerializeField] private CombatDerivedStatRuleBlock derivedStatRules = new CombatDerivedStatRuleBlock();
    [SerializeField] private CombatHitRuleBlock hitRules = new CombatHitRuleBlock();
    [SerializeField] private CombatDamageRuleBlock damageRules = new CombatDamageRuleBlock();
    [SerializeField] private CombatSurgeRuleBlock surgeRules = new CombatSurgeRuleBlock();
    [SerializeField] private CombatMomentumRuleBlock momentumRules = new CombatMomentumRuleBlock();

    public string ProfileId => profileId;
    public string DisplayName => displayName;
    public CombatStatContributionBlock StatContributions => statContributions;
    public CombatDerivedStatRuleBlock DerivedStatRules => derivedStatRules;
    public CombatHitRuleBlock HitRules => hitRules;
    public CombatDamageRuleBlock DamageRules => damageRules;
    public CombatSurgeRuleBlock SurgeRules => surgeRules;
    public CombatMomentumRuleBlock MomentumRules => momentumRules;

    private void OnValidate()
    {
        profileId = string.IsNullOrWhiteSpace(profileId) ? string.Empty : profileId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Combat Formula" : displayName.Trim();
        statContributions ??= new CombatStatContributionBlock();
        derivedStatRules ??= new CombatDerivedStatRuleBlock();
        hitRules ??= new CombatHitRuleBlock();
        damageRules ??= new CombatDamageRuleBlock();
        surgeRules ??= new CombatSurgeRuleBlock();
        momentumRules ??= new CombatMomentumRuleBlock();
        statContributions.Sanitize();
        derivedStatRules.Sanitize();
        hitRules.Sanitize();
        damageRules.Sanitize();
        surgeRules.Sanitize();
        momentumRules.Sanitize();
    }
}
