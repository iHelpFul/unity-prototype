using UnityEngine;

public static class DamageCalculator
{
    public struct DamageRange
    {
        public int MinDamage;
        public int MaxDamage;

        public DamageRange(int min, int max)
        {
            MinDamage = min;
            MaxDamage = Mathf.Max(1, Mathf.Max(min, max));
        }
    }

    private struct SkillProfile
    {
        public float PrimaryWeight;
        public float SupportWeight;
        public float MinorMightWeight;
        public float MinorPrecisionWeight;
        public float MinorArcaneWeight;
        public float MinorFinesseWeight;
        public float MinorHitRateWeight;
        public float AccuracyPrecisionWeight;
        public float AccuracyHitRateWeight;
        public float AccuracyCoreWeight;
        public float AccuracySupportWeight;
        public float Scale;
    }

    public static int CalculateDamage(
        int might,
        int precision,
        int arcane,
        int finesse,
        int hitRate,
        int weaponPower,
        float mastery,
        bool isSkillDamage)
    {
        return CalculateDamage(
            new PlayerCombatSnapshot
            {
                CurrentJob = PlayerJobType.Drifter,
                Might = might,
                Precision = precision,
                Arcane = arcane,
                Finesse = finesse,
                HitRate = hitRate,
                WeaponPower = weaponPower,
                SkillMastery = mastery
            },
            isSkillDamage);
    }

    public static DamageRange CalculateDamageRange(
        int might,
        int precision,
        int arcane,
        int finesse,
        int hitRate,
        int weaponPower,
        float mastery,
        bool isSkillDamage,
        float damageMultiplier = 1f)
    {
        return CalculateDamageRange(
            new PlayerCombatSnapshot
            {
                CurrentJob = PlayerJobType.Drifter,
                Might = might,
                Precision = precision,
                Arcane = arcane,
                Finesse = finesse,
                HitRate = hitRate,
                WeaponPower = weaponPower,
                SkillMastery = mastery
            },
            isSkillDamage,
            damageMultiplier);
    }

    public static DamageRange CalculateDamageRange(
        PlayerCombatSnapshot snapshot,
        bool isSkillDamage,
        float damageMultiplier = 1f)
    {
        CombatFormulaProfile formulaProfile = ResolveCombatFormulaProfile(snapshot.CurrentJob);
        if (formulaProfile != null)
            return CalculateDamageRangeWithFormula(snapshot, formulaProfile, isSkillDamage, damageMultiplier);

        PlayerJobType job = snapshot.CurrentJob;
        float might = Mathf.Max(0, snapshot.Might);
        float precision = Mathf.Max(0, snapshot.Precision);
        float arcane = Mathf.Max(0, snapshot.Arcane);
        float finesse = Mathf.Max(0, snapshot.Finesse);
        float hitRateStat = Mathf.Max(0, snapshot.HitRate);
        float weapon = Mathf.Max(0, snapshot.WeaponPower);
        float mastery = Mathf.Max(0f, snapshot.SkillMastery);
        float multiplier = Mathf.Max(0.1f, damageMultiplier);

        SkillProfile profile = GetProfile(job, isSkillDamage);

        float primaryStat = ResolveCoreStat(job, might, precision, arcane, finesse);
        float supportStat = ResolveSupportStat(job, might, precision, arcane, finesse);

        float primaryContribution = primaryStat * profile.PrimaryWeight
            + supportStat * profile.SupportWeight;
        float secondaryContribution =
            might * profile.MinorMightWeight
            + precision * profile.MinorPrecisionWeight
            + arcane * profile.MinorArcaneWeight
            + finesse * profile.MinorFinesseWeight
            + hitRateStat * profile.MinorHitRateWeight;

        float accuracyFactor = CalculateAccuracyFactor(
            precision,
            hitRateStat,
            primaryStat,
            supportStat,
            profile);

        float maxDamageMultiplier = 1f + profile.Scale;
        float minAccuracyMultiplier = Mathf.Lerp(0.7f, 0.95f, accuracyFactor);
        float maxAccuracyMultiplier = Mathf.Lerp(0.95f, 1.08f, accuracyFactor);

        float baseStat = primaryContribution * 4f;
        float minStat = (baseStat * 0.9f) * mastery + secondaryContribution;

        float max =
            ((baseStat + secondaryContribution) * weapon * maxAccuracyMultiplier * maxDamageMultiplier) / 100f;

        float min =
            (minStat * weapon * minAccuracyMultiplier * maxDamageMultiplier) / 100f;

        float maxDamage = Mathf.Max(0f, max) * multiplier;
        float minDamage = Mathf.Max(0f, min) * multiplier;

        return new DamageRange(
            Mathf.Max(1, Mathf.RoundToInt(minDamage)),
            Mathf.Max(1, Mathf.RoundToInt(maxDamage)));
    }

    public static int CalculateDamage(
        PlayerCombatSnapshot snapshot,
        bool isSkillDamage)
    {
        CombatFormulaProfile formulaProfile = ResolveCombatFormulaProfile(snapshot.CurrentJob);
        if (formulaProfile != null)
            return CalculateDamageWithFormula(snapshot, formulaProfile, isSkillDamage);

        PlayerJobType job = snapshot.CurrentJob;
        float might = Mathf.Max(0, snapshot.Might);
        float precision = Mathf.Max(0, snapshot.Precision);
        float arcane = Mathf.Max(0, snapshot.Arcane);
        float finesse = Mathf.Max(0, snapshot.Finesse);
        float hitRateStat = Mathf.Max(0, snapshot.HitRate);
        float weapon = Mathf.Max(0, snapshot.WeaponPower);
        float mastery = Mathf.Max(0f, snapshot.SkillMastery);

        SkillProfile profile = GetProfile(job, isSkillDamage);

        float primaryStat = ResolveCoreStat(job, might, precision, arcane, finesse);
        float supportStat = ResolveSupportStat(job, might, precision, arcane, finesse);

        float primaryContribution = primaryStat * profile.PrimaryWeight
            + supportStat * profile.SupportWeight;
        float secondaryContribution =
            might * profile.MinorMightWeight
            + precision * profile.MinorPrecisionWeight
            + arcane * profile.MinorArcaneWeight
            + finesse * profile.MinorFinesseWeight
            + hitRateStat * profile.MinorHitRateWeight;

        float accuracyFactor = CalculateAccuracyFactor(
            precision,
            hitRateStat,
            primaryStat,
            supportStat,
            profile);

        float maxDamageMultiplier = 1f + profile.Scale;
        float minAccuracyMultiplier = Mathf.Lerp(0.7f, 0.95f, accuracyFactor);
        float maxAccuracyMultiplier = Mathf.Lerp(0.95f, 1.08f, accuracyFactor);

        float baseStat = primaryContribution * 4f;
        float minStat = (baseStat * 0.9f) * mastery + secondaryContribution;

        float max =
            ((baseStat + secondaryContribution) * weapon * maxAccuracyMultiplier * maxDamageMultiplier) / 100f;

        float min =
            (minStat * weapon * minAccuracyMultiplier * maxDamageMultiplier) / 100f;

        float damage = Random.Range(Mathf.Max(0f, min), Mathf.Max(min, max));
        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    public static int CalculateDamage(PlayerCombatSnapshot snapshot)
    {
        return CalculateDamage(snapshot, isSkillDamage: false);
    }

    private static DamageRange CalculateDamageRangeWithFormula(
        PlayerCombatSnapshot snapshot,
        CombatFormulaProfile formulaProfile,
        bool isSkillDamage,
        float damageMultiplier)
    {
        float minDamage = CalculateFormulaDamage(snapshot, formulaProfile, isSkillDamage, true);
        float maxDamage = CalculateFormulaDamage(snapshot, formulaProfile, isSkillDamage, false);
        float multiplier = Mathf.Max(0.1f, damageMultiplier);

        return new DamageRange(
            Mathf.Max(1, Mathf.RoundToInt(minDamage * multiplier)),
            Mathf.Max(1, Mathf.RoundToInt(maxDamage * multiplier)));
    }

    private static int CalculateDamageWithFormula(
        PlayerCombatSnapshot snapshot,
        CombatFormulaProfile formulaProfile,
        bool isSkillDamage)
    {
        float minDamage = CalculateFormulaDamage(snapshot, formulaProfile, isSkillDamage, true);
        float maxDamage = CalculateFormulaDamage(snapshot, formulaProfile, isSkillDamage, false);
        float rolledDamage = Random.Range(Mathf.Min(minDamage, maxDamage), Mathf.Max(minDamage, maxDamage));
        return Mathf.Max(1, Mathf.RoundToInt(rolledDamage));
    }

    private static float CalculateFormulaDamage(
        PlayerCombatSnapshot snapshot,
        CombatFormulaProfile formulaProfile,
        bool isSkillDamage,
        bool isMinimumDamage)
    {
        PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(snapshot.CurrentJob);
        PlayerProgressionStatType coreStatType = jobDefinition != null ? jobDefinition.CoreStat : snapshot.CoreStat;
        PlayerProgressionStatType supportStatType = jobDefinition != null ? jobDefinition.SecondaryStat : snapshot.SecondaryStat;

        float coreStat = ResolveConfiguredStat(snapshot, coreStatType);
        float supportStat = ResolveConfiguredStat(snapshot, supportStatType);
        float weightedContribution = CalculateWeightedContribution(snapshot, formulaProfile.StatContributions);
        float coreWeight = GetContributionWeight(formulaProfile.StatContributions, coreStatType);
        float supportWeight = GetContributionWeight(formulaProfile.StatContributions, supportStatType);
        float accuracyFactor = CalculateFormulaAccuracyFactor(snapshot, formulaProfile, coreStat, supportStat);
        float skillScale = isSkillDamage ? 1.15f : 1f;
        float masteryMultiplier = Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(snapshot.SkillMastery));
        float weaponPower = Mathf.Max(1f, snapshot.WeaponPower * Mathf.Max(0f, formulaProfile.DamageRules.WeaponPowerWeight));

        float baseStat =
            coreStat * Mathf.Max(0.1f, coreWeight) * 4f
            + supportStat * Mathf.Max(0.05f, supportWeight) * 1.5f
            + weightedContribution * 0.5f;

        float accuracyMultiplier = isMinimumDamage
            ? Mathf.Lerp(0.85f, 0.98f, accuracyFactor)
            : Mathf.Lerp(0.98f, 1.12f, accuracyFactor);

        float variance = isMinimumDamage
            ? Mathf.Max(0f, formulaProfile.DamageRules.BaseVarianceMin)
            : Mathf.Max(formulaProfile.DamageRules.BaseVarianceMin, formulaProfile.DamageRules.BaseVarianceMax);

        float scaledDamage =
            ((baseStat * skillScale * masteryMultiplier) * weaponPower * Mathf.Max(0.01f, formulaProfile.DamageRules.DamageCoefficientWeight))
            / 100f;

        float mitigatedDamage = scaledDamage * variance * accuracyMultiplier;
        return Mathf.Max(formulaProfile.DamageRules.MinimumDamageFloor, mitigatedDamage);
    }

    private static float CalculateAccuracyFactor(
        float precision,
        float hitRate,
        float coreStat,
        float supportStat,
        SkillProfile profile)
    {
        float accuracy = precision * profile.AccuracyPrecisionWeight
            + hitRate * profile.AccuracyHitRateWeight
            + coreStat * profile.AccuracyCoreWeight
            + supportStat * profile.AccuracySupportWeight;

        return Mathf.Clamp01(accuracy / 120f);
    }

    private static float CalculateFormulaAccuracyFactor(
        PlayerCombatSnapshot snapshot,
        CombatFormulaProfile formulaProfile,
        float coreStat,
        float supportStat)
    {
        float hitRate = Mathf.Max(0, snapshot.HitRate) * formulaProfile.HitRules.HitRateWeight;
        float levelContribution = Mathf.Max(1, snapshot.Level) * formulaProfile.HitRules.LevelDeltaWeight;
        float precisionContribution = Mathf.Max(0, snapshot.Precision) * 0.5f;
        float accuracy = hitRate + precisionContribution + coreStat * 0.1f + supportStat * 0.05f + levelContribution;

        return Mathf.Clamp01(accuracy / 120f);
    }

    private static float CalculateWeightedContribution(
        PlayerCombatSnapshot snapshot,
        CombatStatContributionBlock statContributions)
    {
        return Mathf.Max(0, snapshot.Might) * statContributions.MightWeight
            + Mathf.Max(0, snapshot.Precision) * statContributions.PrecisionWeight
            + Mathf.Max(0, snapshot.Arcane) * statContributions.ArcaneWeight
            + Mathf.Max(0, snapshot.Finesse) * statContributions.FinesseWeight
            + Mathf.Max(0, snapshot.HitRate) * statContributions.HitRateWeight;
    }

    private static float ResolveConfiguredStat(
        PlayerCombatSnapshot snapshot,
        PlayerProgressionStatType statType)
    {
        return statType switch
        {
            PlayerProgressionStatType.Might => Mathf.Max(0, snapshot.Might),
            PlayerProgressionStatType.Precision => Mathf.Max(0, snapshot.Precision),
            PlayerProgressionStatType.Arcane => Mathf.Max(0, snapshot.Arcane),
            PlayerProgressionStatType.Finesse => Mathf.Max(0, snapshot.Finesse),
            PlayerProgressionStatType.HitRate => Mathf.Max(0, snapshot.HitRate),
            _ => Mathf.Max(0, snapshot.Might)
        };
    }

    private static float GetContributionWeight(
        CombatStatContributionBlock statContributions,
        PlayerProgressionStatType statType)
    {
        return statType switch
        {
            PlayerProgressionStatType.Might => statContributions.MightWeight,
            PlayerProgressionStatType.Precision => statContributions.PrecisionWeight,
            PlayerProgressionStatType.Arcane => statContributions.ArcaneWeight,
            PlayerProgressionStatType.Finesse => statContributions.FinesseWeight,
            PlayerProgressionStatType.HitRate => statContributions.HitRateWeight,
            _ => statContributions.MightWeight
        };
    }

    private static CombatFormulaProfile ResolveCombatFormulaProfile(PlayerJobType job)
    {
        PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(job);
        return jobDefinition != null ? jobDefinition.CombatFormulaProfile : null;
    }

    private static float ResolveCoreStat(
        PlayerJobType job,
        float might,
        float precision,
        float arcane,
        float finesse)
    {
        return job switch
        {
            PlayerJobType.Vanguard => might,
            PlayerJobType.Shade => finesse,
            PlayerJobType.Arcanist => arcane,
            _ => might
        };
    }

    private static float ResolveSupportStat(
        PlayerJobType job,
        float might,
        float precision,
        float arcane,
        float finesse)
    {
        return precision;
    }

    private static SkillProfile GetProfile(PlayerJobType job, bool isSkillDamage)
    {
        if (isSkillDamage)
        {
            return job switch
            {
                PlayerJobType.Vanguard => new SkillProfile
                {
                    PrimaryWeight = 4.0f,
                    SupportWeight = 1.6f,
                    MinorMightWeight = 0.45f,
                    MinorPrecisionWeight = 0.35f,
                    MinorArcaneWeight = 0.20f,
                    MinorFinesseWeight = 0.28f,
                    MinorHitRateWeight = 0.40f,
                    AccuracyPrecisionWeight = 2.0f,
                    AccuracyHitRateWeight = 2.0f,
                    AccuracyCoreWeight = 0.30f,
                    AccuracySupportWeight = 0.12f,
                    Scale = 0.04f
                },
                PlayerJobType.Shade => new SkillProfile
                {
                    PrimaryWeight = 2.8f,
                    SupportWeight = 2.4f,
                    MinorMightWeight = 0.50f,
                    MinorPrecisionWeight = 0.35f,
                    MinorArcaneWeight = 0.40f,
                    MinorFinesseWeight = 0.45f,
                    MinorHitRateWeight = 0.42f,
                    AccuracyPrecisionWeight = 2.0f,
                    AccuracyHitRateWeight = 2.2f,
                    AccuracyCoreWeight = 0.35f,
                    AccuracySupportWeight = 0.18f,
                    Scale = 0.03f
                },
                PlayerJobType.Arcanist => new SkillProfile
                {
                    PrimaryWeight = 4.5f,
                    SupportWeight = 1.6f,
                    MinorMightWeight = 0.40f,
                    MinorPrecisionWeight = 0.35f,
                    MinorArcaneWeight = 0.30f,
                    MinorFinesseWeight = 0.40f,
                    MinorHitRateWeight = 0.38f,
                    AccuracyPrecisionWeight = 2.0f,
                    AccuracyHitRateWeight = 2.0f,
                    AccuracyCoreWeight = 0.30f,
                    AccuracySupportWeight = 0.14f,
                    Scale = 0.04f
                },
                _ => new SkillProfile
                {
                    PrimaryWeight = 3.2f,
                    SupportWeight = 1.4f,
                    MinorMightWeight = 0.45f,
                    MinorPrecisionWeight = 0.38f,
                    MinorArcaneWeight = 0.38f,
                    MinorFinesseWeight = 0.38f,
                    MinorHitRateWeight = 0.40f,
                    AccuracyPrecisionWeight = 2.0f,
                    AccuracyHitRateWeight = 2.0f,
                    AccuracyCoreWeight = 0.30f,
                    AccuracySupportWeight = 0.12f,
                    Scale = 0.035f
                }
            };
        }

        return job switch
        {
            PlayerJobType.Vanguard => new SkillProfile
            {
                PrimaryWeight = 4.2f,
                SupportWeight = 1.2f,
                MinorMightWeight = 0.34f,
                MinorPrecisionWeight = 0.25f,
                MinorArcaneWeight = 0.12f,
                MinorFinesseWeight = 0.10f,
                MinorHitRateWeight = 0.28f,
                AccuracyPrecisionWeight = 1.6f,
                AccuracyHitRateWeight = 1.8f,
                AccuracyCoreWeight = 0.25f,
                AccuracySupportWeight = 0.10f,
                Scale = 0.03f
            },
            PlayerJobType.Shade => new SkillProfile
            {
                PrimaryWeight = 2.8f,
                SupportWeight = 1.3f,
                MinorMightWeight = 0.38f,
                MinorPrecisionWeight = 0.30f,
                MinorArcaneWeight = 0.25f,
                MinorFinesseWeight = 0.45f,
                MinorHitRateWeight = 0.35f,
                AccuracyPrecisionWeight = 1.8f,
                AccuracyHitRateWeight = 2.0f,
                AccuracyCoreWeight = 0.24f,
                AccuracySupportWeight = 0.14f,
                Scale = 0.028f
            },
            PlayerJobType.Arcanist => new SkillProfile
            {
                PrimaryWeight = 1.7f,
                SupportWeight = 1.2f,
                MinorMightWeight = 0.40f,
                MinorPrecisionWeight = 0.28f,
                MinorArcaneWeight = 0.35f,
                MinorFinesseWeight = 0.25f,
                MinorHitRateWeight = 0.36f,
                AccuracyPrecisionWeight = 1.8f,
                AccuracyHitRateWeight = 2.0f,
                AccuracyCoreWeight = 0.24f,
                AccuracySupportWeight = 0.14f,
                Scale = 0.028f
            },
            _ => new SkillProfile
            {
                PrimaryWeight = 3.4f,
                SupportWeight = 1.2f,
                MinorMightWeight = 0.35f,
                MinorPrecisionWeight = 0.32f,
                MinorArcaneWeight = 0.32f,
                MinorFinesseWeight = 0.32f,
                MinorHitRateWeight = 0.33f,
                AccuracyPrecisionWeight = 1.6f,
                AccuracyHitRateWeight = 1.8f,
                AccuracyCoreWeight = 0.25f,
                AccuracySupportWeight = 0.10f,
                Scale = 0.03f
            }
        };
    }
}


