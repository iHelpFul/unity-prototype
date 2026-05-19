using System.Collections.Generic;
using UnityEngine;

public static class AttackPayloadBuilder
{
    private struct PassiveAttackFamilyTotals
    {
        public float DamageBonus;
        public float RangeBonus;
        public float SurgeChanceBonus;
        public float SurgePowerBonus;
    }

    public static AttackPayload BuildBasicAttackPayload(
        PlayerCharacter sourceCharacter,
        PlayerCombatSnapshot snapshot,
        PlayerBasicAttackProfile profile,
        int comboCounter = 0,
        EmpoweredBasicDefinition empoweredBasic = default)
    {
        if (profile == null)
            return null;

        EmpoweredBasicDefinition resolvedEmpoweredBasic = empoweredBasic.GetSanitized();
        float comboMultiplier = 1f;
        if (profile.SupportsComboCounter && comboCounter > 0)
            comboMultiplier += comboCounter * profile.ComboDamageBonusPerStack;

        PlayerJobDefinition jobDefinition = ResolveJobDefinition(snapshot.CurrentJob);
        PlayerRuntimeData runtimeData = sourceCharacter != null ? sourceCharacter.RuntimeData : null;
        CombatFormulaProfile formulaProfile = jobDefinition != null ? jobDefinition.CombatFormulaProfile : null;
        ProjectileProfile projectileProfile = profile.DefaultProjectileProfile;
        PassiveAttackFamilyTotals passiveTotals = ResolvePassiveAttackFamilyTotals(
            runtimeData,
            jobDefinition,
            snapshot.CurrentJob,
            profile.AttackFamily);
        int momentumStacks = Mathf.Max(0, snapshot.MomentumStacks);
        float momentumDamageMultiplier = ResolveMomentumDamageMultiplier(formulaProfile, momentumStacks);
        float momentumRangeMultiplier = ResolveMomentumRangeMultiplier(formulaProfile, momentumStacks);
        float momentumSurgeChanceBonus = ResolveMomentumSurgeChanceBonus(formulaProfile, momentumStacks);
        float momentumSurgePowerBonus = ResolveMomentumSurgePowerBonus(formulaProfile, momentumStacks);

        float totalDamageMultiplier = Mathf.Max(
            0.05f,
            profile.BasicDamageMultiplier
            * comboMultiplier
            * Mathf.Max(0.05f, profile.DamageCoefficient)
            * (resolvedEmpoweredBasic.IsConfigured ? resolvedEmpoweredBasic.DamageMultiplier : 1f)
            * Mathf.Max(0.05f, 1f + passiveTotals.DamageBonus)
            * momentumDamageMultiplier);

        DamageCalculator.DamageRange damageRange = DamageCalculator.CalculateDamageRange(
            snapshot,
            isSkillDamage: false,
            damageMultiplier: totalDamageMultiplier);

        float resolvedRange = Mathf.Max(0f, profile.BaseRange);
        if (snapshot.RangeModifier > 0f)
            resolvedRange *= snapshot.RangeModifier;
        resolvedRange *= Mathf.Max(0.05f, 1f + passiveTotals.RangeBonus);
        resolvedRange *= momentumRangeMultiplier;

        float baseSurgeChance = Mathf.Max(0f, snapshot.SurgeChance);
        float contextSurgeChanceBonus = Mathf.Max(0f, momentumSurgeChanceBonus + passiveTotals.SurgeChanceBonus);
        float skillSurgeChanceBonus = Mathf.Max(
            0f,
            profile.BaseSurgeChanceBonus
            + (resolvedEmpoweredBasic.IsConfigured ? resolvedEmpoweredBasic.SurgeChanceBonus : 0f));
        float finalSurgeChance = Mathf.Clamp(
            baseSurgeChance + contextSurgeChanceBonus + skillSurgeChanceBonus,
            0f,
            100f);

        float baseSurgePower = Mathf.Max(0f, snapshot.SurgePower);
        float contextSurgePowerBonus = Mathf.Max(0f, momentumSurgePowerBonus + passiveTotals.SurgePowerBonus);
        float skillSurgePowerBonus = Mathf.Max(
            0f,
            profile.BaseSurgePowerBonus
            + (resolvedEmpoweredBasic.IsConfigured ? resolvedEmpoweredBasic.SurgePowerBonus : 0f));
        float finalSurgePower = Mathf.Max(0f, baseSurgePower + contextSurgePowerBonus + skillSurgePowerBonus);
        CombatElementType elementType = profile.DefaultElement != CombatElementType.None
            ? profile.DefaultElement
            : snapshot.CurrentElement;

        return AttackPayload.CreateTransient(
            newPayloadId: BuildPayloadId(profile.AttackId, 1),
            newActionKind: AttackPayloadActionKind.BasicAttack,
            newActionId: profile.AttackId,
            newAttackFamily: profile.AttackFamily,
            newExecutionKind: profile.ExecutionKind,
            newSourceJobType: snapshot.CurrentJob,
            newSourceLevel: Mathf.Max(1, snapshot.Level),
            newSourcePositionAtRelease: sourceCharacter != null ? sourceCharacter.transform.position : Vector3.zero,
            newSkillLevel: 1,
            newResolvedMinDamage: damageRange.MinDamage,
            newResolvedMaxDamage: damageRange.MaxDamage,
            newResolvedDamageCoefficient: totalDamageMultiplier,
            newResolvedWeaponPower: Mathf.Max(0, snapshot.WeaponPower),
            newResolvedHitRate: Mathf.Max(0, snapshot.HitRate),
            newResolvedHitCount: Mathf.Max(1, profile.HitCount),
            newResolvedProjectileCount: 1,
            newResolvedRange: resolvedRange,
            newResolvedHitBox: profile.HitBox,
            newResolvedBreakPower: Mathf.Max(0f, profile.BaseBreakPower),
            newMomentumBefore: Mathf.Max(0, snapshot.MomentumStacks),
            newMomentumToConsume: 0,
            newBaseSurgeChance: baseSurgeChance,
            newContextSurgeChanceBonus: contextSurgeChanceBonus,
            newSkillSurgeChanceBonus: skillSurgeChanceBonus,
            newFinalSurgeChance: finalSurgeChance,
            newBaseSurgePower: baseSurgePower,
            newContextSurgePowerBonus: contextSurgePowerBonus,
            newSkillSurgePowerBonus: skillSurgePowerBonus,
            newFinalSurgePower: finalSurgePower,
            newHasElement: elementType != CombatElementType.None,
            newElementType: elementType,
            newElementPower: elementType != CombatElementType.None ? 1f : 0f,
            newCanMiss: true,
            newCanSurge: true,
            newCanApplyStates: true,
            newCanTriggerOnHitEffects: true,
            newCanTriggerOnKillEffects: true,
            newMaxTargets: projectileProfile != null
                ? Mathf.Max(1, projectileProfile.MaxTargets)
                : Mathf.Max(1, profile.MaxTargets),
            newStopOnFirstValidHit: projectileProfile == null || projectileProfile.StopOnFirstValidHit);
    }

    public static AttackPayload BuildSkillPayload(
        PlayerCharacter sourceCharacter,
        PlayerCombatSnapshot snapshot,
        PlayerSkillDefinition definition,
        int skillLevel = 1)
    {
        if (definition == null)
            return null;

        int resolvedSkillLevel = Mathf.Max(1, skillLevel);
        PlayerSkillLevelDefinition levelDefinition = definition.GetLevelDefinition(resolvedSkillLevel);
        PlayerJobDefinition jobDefinition = ResolveJobDefinition(snapshot.CurrentJob);
        PlayerRuntimeData runtimeData = sourceCharacter != null ? sourceCharacter.RuntimeData : null;
        CombatFormulaProfile formulaProfile = jobDefinition != null ? jobDefinition.CombatFormulaProfile : null;
        ProjectileProfile projectileProfile = definition.GetResolvedProjectileProfile(resolvedSkillLevel);
        PassiveAttackFamilyTotals passiveTotals = ResolvePassiveAttackFamilyTotals(
            runtimeData,
            jobDefinition,
            snapshot.CurrentJob,
            definition.AttackFamily);
        int momentumStacks = Mathf.Max(0, snapshot.MomentumStacks);
        float momentumDamageMultiplier = ResolveMomentumDamageMultiplier(formulaProfile, momentumStacks);
        float momentumRangeMultiplier = ResolveMomentumRangeMultiplier(formulaProfile, momentumStacks);
        float momentumSurgeChanceBonus = ResolveMomentumSurgeChanceBonus(formulaProfile, momentumStacks);
        float momentumSurgePowerBonus = ResolveMomentumSurgePowerBonus(formulaProfile, momentumStacks);

        float levelDamageCoefficient = levelDefinition != null
            ? Mathf.Max(0.05f, levelDefinition.DamageCoefficient)
            : 1f;
        float totalDamageMultiplier = Mathf.Max(
            0.1f,
            definition.DamageMultiplier
            * levelDamageCoefficient
            * Mathf.Max(0.05f, 1f + passiveTotals.DamageBonus)
            * momentumDamageMultiplier);
        DamageCalculator.DamageRange damageRange = DamageCalculator.CalculateDamageRange(
            snapshot,
            isSkillDamage: true,
            damageMultiplier: totalDamageMultiplier);

        int resolvedHitCount = definition.GetResolvedHitCount(resolvedSkillLevel);
        float resolvedRange = definition.GetResolvedRange(resolvedSkillLevel);

        if (snapshot.RangeModifier > 0f)
            resolvedRange *= snapshot.RangeModifier;
        resolvedRange *= Mathf.Max(0.05f, 1f + passiveTotals.RangeBonus);
        resolvedRange *= momentumRangeMultiplier;

        float baseSurgeChance = Mathf.Max(0f, snapshot.SurgeChance);
        float contextSurgeChanceBonus = Mathf.Max(0f, momentumSurgeChanceBonus + passiveTotals.SurgeChanceBonus);
        float skillSurgeChanceBonus = Mathf.Max(
            0f,
            definition.BaseSurgeChanceBonus + (levelDefinition != null ? levelDefinition.ExtraSurgeChance : 0f));
        float finalSurgeChance = Mathf.Clamp(
            baseSurgeChance + contextSurgeChanceBonus + skillSurgeChanceBonus,
            0f,
            100f);

        float baseSurgePower = Mathf.Max(0f, snapshot.SurgePower);
        float contextSurgePowerBonus = Mathf.Max(0f, momentumSurgePowerBonus + passiveTotals.SurgePowerBonus);
        float skillSurgePowerBonus = Mathf.Max(
            0f,
            definition.BaseSurgePowerBonus + (levelDefinition != null ? levelDefinition.ExtraSurgePower : 0f));
        float finalSurgePower = Mathf.Max(0f, baseSurgePower + contextSurgePowerBonus + skillSurgePowerBonus);

        CombatElementType elementType = definition.DefaultElement != CombatElementType.None
            ? definition.DefaultElement
            : snapshot.CurrentElement;
        ReadyStateEmpowerDefinition readyStateEmpower = definition.ReadyStateEmpower;

        return AttackPayload.CreateTransient(
            newPayloadId: BuildPayloadId(definition.SkillId, resolvedSkillLevel),
            newActionKind: AttackPayloadActionKind.Skill,
            newActionId: definition.SkillId,
            newAttackFamily: definition.AttackFamily,
            newExecutionKind: definition.ExecutionKind,
            newSourceJobType: snapshot.CurrentJob,
            newSourceLevel: Mathf.Max(1, snapshot.Level),
            newSourcePositionAtRelease: sourceCharacter != null ? sourceCharacter.transform.position : Vector3.zero,
            newSkillLevel: resolvedSkillLevel,
            newResolvedMinDamage: damageRange.MinDamage,
            newResolvedMaxDamage: damageRange.MaxDamage,
            newResolvedDamageCoefficient: totalDamageMultiplier,
            newResolvedWeaponPower: Mathf.Max(0, snapshot.WeaponPower),
            newResolvedHitRate: Mathf.Max(0, snapshot.HitRate),
            newResolvedHitCount: resolvedHitCount,
            newResolvedProjectileCount: definition.GetResolvedProjectileCount(resolvedSkillLevel),
            newResolvedRange: Mathf.Max(0f, resolvedRange),
            newResolvedHitBox: definition.HitBox,
            newResolvedBreakPower: Mathf.Max(0f, definition.BaseBreakPower),
            newMomentumBefore: Mathf.Max(0, snapshot.MomentumStacks),
            newMomentumToConsume: definition.GetResolvedMomentumCost(resolvedSkillLevel),
            newPendingReadyStateType: ResolvePendingReadyStateType(sourceCharacter, readyStateEmpower),
            newReadyStateEmpower: readyStateEmpower,
            newBaseSurgeChance: baseSurgeChance,
            newContextSurgeChanceBonus: contextSurgeChanceBonus,
            newSkillSurgeChanceBonus: skillSurgeChanceBonus,
            newFinalSurgeChance: finalSurgeChance,
            newBaseSurgePower: baseSurgePower,
            newContextSurgePowerBonus: contextSurgePowerBonus,
            newSkillSurgePowerBonus: skillSurgePowerBonus,
            newFinalSurgePower: finalSurgePower,
            newHasElement: elementType != CombatElementType.None,
            newElementType: elementType,
            newElementPower: elementType != CombatElementType.None ? 1f : 0f,
            newCanMiss: true,
            newCanSurge: true,
            newCanApplyStates: true,
            newCanTriggerOnHitEffects: true,
            newCanTriggerOnKillEffects: true,
            newMaxTargets: ResolveProjectileTargetCap(
                projectileProfile,
                definition.GetResolvedMaxTargets(resolvedSkillLevel),
                0),
            newStopOnFirstValidHit: projectileProfile != null
                ? projectileProfile.StopOnFirstValidHit
                : definition.CombatTargetingKind != CombatTargetingKind.Area);
    }

    public static AttackPayload BuildBurstLinkedSkillPayload(
        PlayerCharacter sourceCharacter,
        PlayerCombatSnapshot snapshot,
        PlayerSkillDefinition definition,
        int skillLevel,
        BurstLinkedSkillProfile burstProfile,
        float gaugeSpent,
        int flowStacksAtRelease,
        float holdDurationAtRelease,
        bool isAerialRelease)
    {
        if (definition == null || burstProfile == null)
            return null;

        int resolvedSkillLevel = Mathf.Max(1, skillLevel);
        PlayerSkillLevelDefinition levelDefinition = definition.GetLevelDefinition(resolvedSkillLevel);
        PlayerJobDefinition jobDefinition = ResolveJobDefinition(snapshot.CurrentJob);
        PlayerRuntimeData runtimeData = sourceCharacter != null ? sourceCharacter.RuntimeData : null;
        CombatFormulaProfile formulaProfile = jobDefinition != null ? jobDefinition.CombatFormulaProfile : null;
        ProjectileProfile projectileProfile = definition.GetResolvedProjectileProfile(resolvedSkillLevel);
        PassiveAttackFamilyTotals passiveTotals = ResolvePassiveAttackFamilyTotals(
            runtimeData,
            jobDefinition,
            snapshot.CurrentJob,
            definition.AttackFamily);
        int momentumStacks = Mathf.Max(0, snapshot.MomentumStacks);
        float momentumDamageMultiplier = ResolveMomentumDamageMultiplier(formulaProfile, momentumStacks);
        float momentumRangeMultiplier = ResolveMomentumRangeMultiplier(formulaProfile, momentumStacks);
        float momentumSurgeChanceBonus = ResolveMomentumSurgeChanceBonus(formulaProfile, momentumStacks);
        float momentumSurgePowerBonus = ResolveMomentumSurgePowerBonus(formulaProfile, momentumStacks);
        GaugeScalingProfile scalingProfile = burstProfile.GaugeScalingProfile;
        float normalizedGaugeSpend = ResolveGaugeSpendNormalized(scalingProfile, gaugeSpent);
        float damageMultiplierFromGauge = ResolveScaledFloatMultiplier(
            scalingProfile != null ? scalingProfile.DamageMultiplierBySpend : null,
            normalizedGaugeSpend);
        float rangeMultiplierFromGauge = ResolveScaledFloatMultiplier(
            scalingProfile != null ? scalingProfile.RangeBySpend : null,
            normalizedGaugeSpend);
        float breakMultiplierFromGauge = ResolveScaledFloatMultiplier(
            scalingProfile != null ? scalingProfile.BreakPowerBySpend : null,
            normalizedGaugeSpend);
        int resolvedProjectileCount = ResolveScaledIntValue(
            scalingProfile != null ? scalingProfile.ProjectileCountBySpend : null,
            normalizedGaugeSpend,
            definition.GetResolvedProjectileCount(resolvedSkillLevel),
            1);
        float radiusScaleFromGauge = ResolveScaledFloatMultiplier(
            scalingProfile != null ? scalingProfile.RadiusBySpend : null,
            normalizedGaugeSpend);
        float surgeChanceBonusFromGauge = ResolveScaledFloatBonus(
            scalingProfile != null ? scalingProfile.SurgeChanceBySpend : null,
            normalizedGaugeSpend);
        float surgePowerBonusFromGauge = ResolveScaledFloatBonus(
            scalingProfile != null ? scalingProfile.SurgePowerBySpend : null,
            normalizedGaugeSpend);
        float aerialDamageMultiplier = isAerialRelease
            ? Mathf.Max(0f, burstProfile.AerialDamageMultiplier)
            : 1f;

        float levelDamageCoefficient = levelDefinition != null
            ? Mathf.Max(0.05f, levelDefinition.DamageCoefficient)
            : 1f;
        float totalDamageMultiplier = Mathf.Max(
            0.1f,
            definition.DamageMultiplier
            * levelDamageCoefficient
            * damageMultiplierFromGauge
            * Mathf.Max(0.05f, 1f + passiveTotals.DamageBonus)
            * momentumDamageMultiplier
            * aerialDamageMultiplier);

        DamageCalculator.DamageRange damageRange = DamageCalculator.CalculateDamageRange(
            snapshot,
            isSkillDamage: true,
            damageMultiplier: totalDamageMultiplier);

        float resolvedRange = definition.GetResolvedRange(resolvedSkillLevel);
        if (snapshot.RangeModifier > 0f)
            resolvedRange *= snapshot.RangeModifier;
        resolvedRange *= Mathf.Max(0.05f, 1f + passiveTotals.RangeBonus);
        resolvedRange *= momentumRangeMultiplier;
        resolvedRange *= rangeMultiplierFromGauge;

        int resolvedHitCount = ResolveScaledIntValue(
            scalingProfile != null ? scalingProfile.HitCountBySpend : null,
            normalizedGaugeSpend,
            definition.GetResolvedHitCount(resolvedSkillLevel),
            1);
        int resolvedMaxTargets = ResolveScaledIntValue(
            scalingProfile != null ? scalingProfile.MaxTargetsBySpend : null,
            normalizedGaugeSpend,
            definition.GetResolvedMaxTargets(resolvedSkillLevel),
            1);
        int resolvedPierceCount = ResolveScaledIntValue(
            scalingProfile != null ? scalingProfile.PierceCountBySpend : null,
            normalizedGaugeSpend,
            0,
            0);

        float baseSurgeChance = Mathf.Max(0f, snapshot.SurgeChance);
        float contextSurgeChanceBonus = Mathf.Max(0f, momentumSurgeChanceBonus + passiveTotals.SurgeChanceBonus);
        float skillSurgeChanceBonus = Mathf.Max(
            0f,
            definition.BaseSurgeChanceBonus
            + (levelDefinition != null ? levelDefinition.ExtraSurgeChance : 0f)
            + surgeChanceBonusFromGauge);
        float finalSurgeChance = Mathf.Clamp(
            baseSurgeChance + contextSurgeChanceBonus + skillSurgeChanceBonus,
            0f,
            100f);

        float baseSurgePower = Mathf.Max(0f, snapshot.SurgePower);
        float contextSurgePowerBonus = Mathf.Max(0f, momentumSurgePowerBonus + passiveTotals.SurgePowerBonus);
        float skillSurgePowerBonus = Mathf.Max(
            0f,
            definition.BaseSurgePowerBonus
            + (levelDefinition != null ? levelDefinition.ExtraSurgePower : 0f)
            + surgePowerBonusFromGauge);
        float finalSurgePower = Mathf.Max(0f, baseSurgePower + contextSurgePowerBonus + skillSurgePowerBonus);

        CombatElementType elementType = definition.DefaultElement != CombatElementType.None
            ? definition.DefaultElement
            : snapshot.CurrentElement;
        bool isAreaTargeting = definition.CombatTargetingKind == CombatTargetingKind.Area;
        ReadyStateEmpowerDefinition readyStateEmpower = definition.ReadyStateEmpower;

        return AttackPayload.CreateTransient(
            newPayloadId: BuildPayloadId(definition.SkillId, resolvedSkillLevel),
            newActionKind: AttackPayloadActionKind.BurstLinkedSkill,
            newActionId: definition.SkillId,
            newAttackFamily: definition.AttackFamily,
            newExecutionKind: definition.ExecutionKind,
            newSourceJobType: snapshot.CurrentJob,
            newSourceLevel: Mathf.Max(1, snapshot.Level),
            newSourcePositionAtRelease: sourceCharacter != null ? sourceCharacter.transform.position : Vector3.zero,
            newSkillLevel: resolvedSkillLevel,
            newResolvedMinDamage: damageRange.MinDamage,
            newResolvedMaxDamage: damageRange.MaxDamage,
            newResolvedDamageCoefficient: totalDamageMultiplier,
            newResolvedWeaponPower: Mathf.Max(0, snapshot.WeaponPower),
            newResolvedHitRate: Mathf.Max(0, snapshot.HitRate),
            newResolvedHitCount: resolvedHitCount,
            newResolvedProjectileCount: resolvedProjectileCount,
            newResolvedRange: Mathf.Max(0f, resolvedRange),
            newResolvedHitBox: burstProfile.ChargedHitBox.GetRadiusScaled(radiusScaleFromGauge),
            newResolvedBreakPower: Mathf.Max(0f, definition.BaseBreakPower * breakMultiplierFromGauge),
            newGaugeSpentAtRelease: Mathf.Max(0f, gaugeSpent),
            newGaugeSpendNormalized: normalizedGaugeSpend,
            newFlowStacksAtRelease: Mathf.Max(0, flowStacksAtRelease),
            newHoldDurationAtRelease: Mathf.Max(0f, holdDurationAtRelease),
            newAerialRelease: isAerialRelease,
            newMomentumBefore: Mathf.Max(0, snapshot.MomentumStacks),
            newMomentumToConsume: definition.GetResolvedMomentumCost(resolvedSkillLevel),
            newPendingReadyStateType: ResolvePendingReadyStateType(sourceCharacter, readyStateEmpower),
            newReadyStateEmpower: readyStateEmpower,
            newBaseSurgeChance: baseSurgeChance,
            newContextSurgeChanceBonus: contextSurgeChanceBonus,
            newSkillSurgeChanceBonus: skillSurgeChanceBonus,
            newFinalSurgeChance: finalSurgeChance,
            newBaseSurgePower: baseSurgePower,
            newContextSurgePowerBonus: contextSurgePowerBonus,
            newSkillSurgePowerBonus: skillSurgePowerBonus,
            newFinalSurgePower: finalSurgePower,
            newHasElement: elementType != CombatElementType.None,
            newElementType: elementType,
            newElementPower: elementType != CombatElementType.None ? 1f : 0f,
            newCanMiss: true,
            newCanSurge: true,
            newCanApplyStates: true,
            newCanTriggerOnHitEffects: true,
            newCanTriggerOnKillEffects: true,
            newMaxTargets: ResolveProjectileTargetCap(
                projectileProfile,
                resolvedMaxTargets,
                resolvedPierceCount),
            newStopOnFirstValidHit: projectileProfile != null
                ? projectileProfile.StopOnFirstValidHit
                : !isAreaTargeting);
    }

    public static int RollResolvedDamage(AttackPayload payload)
    {
        if (payload == null)
            return 1;

        int minimumDamage = Mathf.Max(1, payload.ResolvedMinDamage);
        int maximumDamage = Mathf.Max(minimumDamage, payload.ResolvedMaxDamage);
        return Random.Range(minimumDamage, maximumDamage + 1);
    }

    private static string BuildPayloadId(string actionId, int skillLevel)
    {
        string normalizedActionId = string.IsNullOrWhiteSpace(actionId)
            ? "attack"
            : actionId.Trim();

        return $"{normalizedActionId}_L{Mathf.Max(1, skillLevel)}_{System.Guid.NewGuid():N}";
    }

    private static PlayerJobDefinition ResolveJobDefinition(PlayerJobType sourceJobType)
    {
        return PlayerJobCombatProfiles.GetJobDefinition(sourceJobType);
    }

    private static PassiveAttackFamilyTotals ResolvePassiveAttackFamilyTotals(
        PlayerRuntimeData runtimeData,
        PlayerJobDefinition jobDefinition,
        PlayerJobType sourceJobType,
        CombatAttackFamily attackFamily)
    {
        PassiveAttackFamilyTotals totals = new PassiveAttackFamilyTotals();
        if (attackFamily == CombatAttackFamily.None)
            return totals;

        IReadOnlyList<PassiveDefinition> activePassives = PlayerPassiveRuntimeUtility.ResolveActivePassiveDefinitions(
            runtimeData,
            jobDefinition,
            sourceJobType);

        if (activePassives == null || activePassives.Count == 0)
            return totals;

        for (int passiveIndex = 0; passiveIndex < activePassives.Count; passiveIndex++)
        {
            PassiveDefinition passiveDefinition = activePassives[passiveIndex];
            if (passiveDefinition == null || !passiveDefinition.SupportsJob(sourceJobType))
                continue;

            int passiveLevel = PlayerPassiveRuntimeUtility.ResolvePassiveLevel(runtimeData, passiveDefinition, fallbackLevel: 1);
            if (passiveLevel <= 0)
                continue;

            PassiveLevelDefinition levelDefinition = passiveDefinition.GetLevelDefinition(passiveLevel);
            if (levelDefinition == null || levelDefinition.AttackFamilyModifiers == null)
                continue;

            for (int modifierIndex = 0; modifierIndex < levelDefinition.AttackFamilyModifiers.Count; modifierIndex++)
            {
                PassiveAttackFamilyModifier modifier = levelDefinition.AttackFamilyModifiers[modifierIndex];
                if (modifier == null || modifier.AttackFamily != attackFamily)
                    continue;

                totals.DamageBonus += Mathf.Max(0f, modifier.DamageBonus);
                totals.RangeBonus += Mathf.Max(0f, modifier.RangeBonus);
                totals.SurgeChanceBonus += Mathf.Max(0f, modifier.SurgeChanceBonus);
                totals.SurgePowerBonus += Mathf.Max(0f, modifier.SurgePowerBonus);
            }
        }

        return totals;
    }

    private static float ResolveMomentumDamageMultiplier(CombatFormulaProfile formulaProfile, int momentumStacks)
    {
        if (formulaProfile == null || momentumStacks <= 0)
            return 1f;

        return Mathf.Max(0.05f, 1f + formulaProfile.MomentumRules.DamageBonusPerStack * momentumStacks);
    }

    private static float ResolveMomentumRangeMultiplier(CombatFormulaProfile formulaProfile, int momentumStacks)
    {
        if (formulaProfile == null || momentumStacks <= 0)
            return 1f;

        return Mathf.Max(0.05f, 1f + formulaProfile.MomentumRules.RangeBonusPerStack * momentumStacks);
    }

    private static float ResolveMomentumSurgeChanceBonus(CombatFormulaProfile formulaProfile, int momentumStacks)
    {
        if (formulaProfile == null || momentumStacks <= 0)
            return 0f;

        return Mathf.Max(0f, formulaProfile.MomentumRules.SurgeChanceBonusPerStack * momentumStacks);
    }

    private static float ResolveMomentumSurgePowerBonus(CombatFormulaProfile formulaProfile, int momentumStacks)
    {
        if (formulaProfile == null || momentumStacks <= 0)
            return 0f;

        return Mathf.Max(0f, formulaProfile.MomentumRules.SurgePowerBonusPerStack * momentumStacks);
    }

    private static float ResolveGaugeSpendNormalized(GaugeScalingProfile scalingProfile, float gaugeSpent)
    {
        if (scalingProfile == null)
            return 0f;

        float minimumSpend = Mathf.Max(0f, scalingProfile.MinimumGaugeSpend);
        float maximumSpend = Mathf.Max(minimumSpend, scalingProfile.MaximumGaugeSpend);
        if (maximumSpend <= minimumSpend)
            return gaugeSpent > 0f ? 1f : 0f;

        return Mathf.Clamp01((gaugeSpent - minimumSpend) / (maximumSpend - minimumSpend));
    }

    private static float ResolveScaledFloatMultiplier(
        GaugeScaledFloatParameter parameter,
        float normalizedGaugeSpend)
    {
        if (parameter == null || !parameter.Enabled)
            return 1f;

        return Mathf.Max(0f, parameter.Evaluate(normalizedGaugeSpend));
    }

    private static float ResolveScaledFloatBonus(
        GaugeScaledFloatParameter parameter,
        float normalizedGaugeSpend)
    {
        if (parameter == null || !parameter.Enabled)
            return 0f;

        return Mathf.Max(0f, parameter.Evaluate(normalizedGaugeSpend));
    }

    private static int ResolveScaledIntValue(
        GaugeScaledIntParameter parameter,
        float normalizedGaugeSpend,
        int fallbackValue,
        int minimumAllowedValue)
    {
        int resolvedFallbackValue = Mathf.Max(minimumAllowedValue, fallbackValue);
        if (parameter == null || !parameter.Enabled)
            return resolvedFallbackValue;

        return Mathf.Max(resolvedFallbackValue, parameter.Evaluate(normalizedGaugeSpend));
    }

    private static int ResolveProjectileTargetCap(
        ProjectileProfile projectileProfile,
        int requestedMaxTargets,
        int pierceCount)
    {
        int resolvedTargets = Mathf.Max(1, requestedMaxTargets);
        if (projectileProfile != null)
            resolvedTargets = Mathf.Max(resolvedTargets, projectileProfile.MaxTargets);

        if (pierceCount > 0)
            resolvedTargets = Mathf.Max(resolvedTargets, pierceCount + 1);

        return resolvedTargets;
    }

    private static PlayerReadyStateType ResolvePendingReadyStateType(
        PlayerCharacter sourceCharacter,
        ReadyStateEmpowerDefinition readyStateEmpower)
    {
        if (sourceCharacter == null || !readyStateEmpower.IsConfigured)
            return PlayerReadyStateType.None;

        return sourceCharacter.HasMatchingReadyState(readyStateEmpower.RequiredReadyStateType)
            ? readyStateEmpower.RequiredReadyStateType
            : PlayerReadyStateType.None;
    }
}
