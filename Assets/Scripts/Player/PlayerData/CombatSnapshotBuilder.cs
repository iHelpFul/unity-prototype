using System.Collections.Generic;
using UnityEngine;

public static class CombatSnapshotBuilder
{
    public static PlayerCombatSnapshot Build(
        PlayerRuntimeData runtimeData,
        PlayerBaseStats baseStats,
        ItemStatModifierData equipmentBonuses,
        int baseWeaponPower,
        float baseSkillMastery,
        PlayerJobDefinition jobDefinition = null,
        WeaponType currentWeaponType = default,
        int momentumStacks = 0,
        CombatElementType currentElement = CombatElementType.None)
    {
        ItemStatModifierData resolvedEquipmentBonuses = equipmentBonuses ?? new ItemStatModifierData();
        PlayerJobType currentJob = runtimeData != null ? runtimeData.CurrentJob : PlayerJobType.Novice;
        PlayerJobDefinition resolvedJobDefinition = jobDefinition ?? PlayerJobCombatProfiles.GetJobDefinition(currentJob);

        int might = ResolveStat(runtimeData != null ? runtimeData.Might : 0, baseStats != null ? baseStats.Might : 0);
        int precision = ResolveStat(runtimeData != null ? runtimeData.Precision : 0, baseStats != null ? baseStats.Precision : 0);
        int arcane = ResolveStat(runtimeData != null ? runtimeData.Arcane : 0, baseStats != null ? baseStats.Arcane : 0);
        int finesse = ResolveStat(runtimeData != null ? runtimeData.Finesse : 0, baseStats != null ? baseStats.Finesse : 0);
        int hitRate = ResolveStat(runtimeData != null ? runtimeData.HitRate : 0, baseStats != null ? baseStats.HitRate : 0);
        int level = runtimeData != null ? Mathf.Max(1, runtimeData.Level) : 1;

        int resolvedMight = might + resolvedEquipmentBonuses.Might;
        int resolvedPrecision = precision + resolvedEquipmentBonuses.Precision;
        int resolvedArcane = arcane + resolvedEquipmentBonuses.Arcane;
        int resolvedFinesse = finesse + resolvedEquipmentBonuses.Finesse;
        int resolvedHitRate = hitRate + resolvedEquipmentBonuses.HitRate;
        int resolvedWeaponPower = Mathf.Max(0, baseWeaponPower) + resolvedEquipmentBonuses.WeaponPower;
        float resolvedSkillMastery = Mathf.Max(0f, baseSkillMastery);
        float resolvedRangeModifier = 1f;
        float resolvedProjectileSpeedModifier = 1f;
        float resolvedSurgeChance = 0f;
        float resolvedSurgePower = 1f;
        int resolvedMomentumGainBonus = 0;
        float resolvedStateDurationModifier = 1f;

        ApplyDefaultPassiveBonuses(
            runtimeData,
            resolvedJobDefinition,
            currentJob,
            ref resolvedMight,
            ref resolvedPrecision,
            ref resolvedArcane,
            ref resolvedFinesse,
            ref resolvedHitRate,
            ref resolvedRangeModifier,
            ref resolvedProjectileSpeedModifier,
            ref resolvedSurgeChance,
            ref resolvedSurgePower,
            ref resolvedMomentumGainBonus,
            ref resolvedStateDurationModifier);

        return new PlayerCombatSnapshot
        {
            CurrentJob = currentJob,
            Level = level,
            CoreStat = resolvedJobDefinition != null ? resolvedJobDefinition.CoreStat : PlayerProgressionStatType.Might,
            SecondaryStat = resolvedJobDefinition != null ? resolvedJobDefinition.SecondaryStat : PlayerProgressionStatType.Precision,
            CurrentWeaponType = currentWeaponType,
            Might = resolvedMight,
            Precision = resolvedPrecision,
            Arcane = resolvedArcane,
            Finesse = resolvedFinesse,
            HitRate = resolvedHitRate,
            WeaponPower = resolvedWeaponPower,
            SkillMastery = resolvedSkillMastery,
            Avoidance = Mathf.Max(0, resolvedFinesse),
            Defense = Mathf.Max(0, resolvedMight),
            SurgeChance = Mathf.Max(0f, resolvedSurgeChance),
            SurgePower = Mathf.Max(0f, resolvedSurgePower),
            RangeModifier = Mathf.Max(0.05f, resolvedRangeModifier),
            ProjectileSpeedModifier = Mathf.Max(0.05f, resolvedProjectileSpeedModifier),
            CastSpeedModifier = 1f,
            AttackSpeedModifier = 1f,
            MomentumStacks = Mathf.Max(0, momentumStacks),
            MomentumGainBonus = Mathf.Max(0, resolvedMomentumGainBonus),
            StateDurationModifier = Mathf.Max(0.05f, resolvedStateDurationModifier),
            CurrentElement = currentElement
        };
    }

    private static int ResolveStat(int runtimeValue, int baseValue)
    {
        if (runtimeValue > 0)
            return runtimeValue;

        return Mathf.Max(0, baseValue);
    }

    private static void ApplyDefaultPassiveBonuses(
        PlayerRuntimeData runtimeData,
        PlayerJobDefinition jobDefinition,
        PlayerJobType currentJob,
        ref int might,
        ref int precision,
        ref int arcane,
        ref int finesse,
        ref int hitRate,
        ref float rangeModifier,
        ref float projectileSpeedModifier,
        ref float surgeChance,
        ref float surgePower,
        ref int momentumGainBonus,
        ref float stateDurationModifier)
    {
        IReadOnlyList<PassiveDefinition> activePassives = PlayerPassiveRuntimeUtility.ResolveActivePassiveDefinitions(
            runtimeData,
            jobDefinition,
            currentJob);

        if (activePassives == null || activePassives.Count == 0)
            return;

        for (int index = 0; index < activePassives.Count; index++)
        {
            PassiveDefinition passiveDefinition = activePassives[index];
            if (passiveDefinition == null || !passiveDefinition.SupportsJob(currentJob))
                continue;

            int passiveLevel = PlayerPassiveRuntimeUtility.ResolvePassiveLevel(runtimeData, passiveDefinition, fallbackLevel: 1);
            if (passiveLevel <= 0)
                continue;

            PassiveLevelDefinition levelDefinition = passiveDefinition.GetLevelDefinition(passiveLevel);
            if (levelDefinition == null)
                continue;

            ApplyPassiveStatModifiers(levelDefinition, ref might, ref precision, ref arcane, ref finesse, ref hitRate);

            rangeModifier *= Mathf.Max(0.05f, 1f + levelDefinition.GlobalRangeBonus);
            projectileSpeedModifier *= Mathf.Max(0.05f, 1f + levelDefinition.ProjectileSpeedBonus);
            surgeChance += Mathf.Max(0f, levelDefinition.SurgeChanceBonus);
            surgePower += Mathf.Max(0f, levelDefinition.SurgePowerBonus);
            momentumGainBonus += Mathf.Max(0, levelDefinition.MomentumGainBonus);
            stateDurationModifier *= Mathf.Max(0.05f, 1f + levelDefinition.StateDurationBonus);
        }
    }

    private static void ApplyPassiveStatModifiers(
        PassiveLevelDefinition levelDefinition,
        ref int might,
        ref int precision,
        ref int arcane,
        ref int finesse,
        ref int hitRate)
    {
        if (levelDefinition == null || levelDefinition.StatModifiers == null)
            return;

        for (int index = 0; index < levelDefinition.StatModifiers.Count; index++)
        {
            PassiveStatModifier statModifier = levelDefinition.StatModifiers[index];
            if (statModifier == null || statModifier.FlatBonus == 0)
                continue;

            switch (statModifier.StatType)
            {
                case PlayerProgressionStatType.Might:
                    might += statModifier.FlatBonus;
                    break;

                case PlayerProgressionStatType.Precision:
                    precision += statModifier.FlatBonus;
                    break;

                case PlayerProgressionStatType.Arcane:
                    arcane += statModifier.FlatBonus;
                    break;

                case PlayerProgressionStatType.Finesse:
                    finesse += statModifier.FlatBonus;
                    break;

                case PlayerProgressionStatType.HitRate:
                    hitRate += statModifier.FlatBonus;
                    break;
            }
        }
    }
}
