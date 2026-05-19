using UnityEngine;

public readonly struct CombatResolutionResult
{
    public CombatResolutionResult(
        bool didHit,
        bool didSurge,
        int finalDamage,
        float hitChance,
        float surgeChance,
        float surgePower)
    {
        DidHit = didHit;
        DidSurge = didSurge;
        FinalDamage = finalDamage;
        HitChance = hitChance;
        SurgeChance = surgeChance;
        SurgePower = surgePower;
    }

    public bool DidHit { get; }
    public bool DidSurge { get; }
    public int FinalDamage { get; }
    public float HitChance { get; }
    public float SurgeChance { get; }
    public float SurgePower { get; }
}

public readonly struct CommittedEnemyHitPacket
{
    public CommittedEnemyHitPacket(
        bool didHit,
        bool didSurge,
        int finalDamage,
        float impactDuration,
        bool commitDeath,
        bool playHitReaction,
        bool finalizeSequenceDeath,
        float reactionLockDuration = 0f)
    {
        DidHit = didHit;
        DidSurge = didSurge;
        FinalDamage = finalDamage;
        ImpactDuration = impactDuration;
        CommitDeath = commitDeath;
        PlayHitReaction = playHitReaction;
        FinalizeSequenceDeath = finalizeSequenceDeath;
        ReactionLockDuration = reactionLockDuration;
    }

    public bool DidHit { get; }
    public bool DidSurge { get; }
    public int FinalDamage { get; }
    public float ImpactDuration { get; }
    public bool CommitDeath { get; }
    public bool PlayHitReaction { get; }
    public bool FinalizeSequenceDeath { get; }
    public float ReactionLockDuration { get; }
}

public static class CombatResolver
{
    public static CombatResolutionResult ResolveAgainstEnemy(
        AttackPayload payload,
        EnemyHealth enemy,
        bool previewPendingReadyEmpower = false)
    {
        if (payload == null)
            return new CombatResolutionResult(false, false, 0, 0f, 0f, 1f);

        CombatFormulaProfile formulaProfile = ResolveFormulaProfile(payload.SourceJobType);
        ReadyStateEmpowerDefinition readyStateEmpower = ResolveReadyStateEmpower(payload, previewPendingReadyEmpower);
        float hitChance = CalculateHitChance(payload, enemy, formulaProfile);
        bool didHit = !payload.CanMiss || Random.value <= hitChance;
        if (!didHit)
            return new CombatResolutionResult(false, false, 0, hitChance, 0f, 1f);

        int rolledDamage = AttackPayloadBuilder.RollResolvedDamage(payload);
        float elementMultiplier = CalculateElementMultiplier(payload, enemy);
        int resolvedBaseDamage = Mathf.Max(1, Mathf.RoundToInt(
            rolledDamage
            * readyStateEmpower.DamageMultiplier
            * elementMultiplier));
        float surgeChance = CalculateSurgeChance(payload, formulaProfile, readyStateEmpower);
        float surgePower = CalculateSurgePower(payload, formulaProfile, readyStateEmpower);
        bool didSurge = payload.CanSurge && Random.value <= surgeChance;

        int finalDamage = didSurge
            ? Mathf.Max(1, Mathf.RoundToInt(resolvedBaseDamage * Mathf.Max(1f, surgePower)))
            : Mathf.Max(1, resolvedBaseDamage);

        return new CombatResolutionResult(
            didHit: true,
            didSurge: didSurge,
            finalDamage: finalDamage,
            hitChance: hitChance,
            surgeChance: surgeChance,
            surgePower: surgePower);
    }

    public static CommittedEnemyHitPacket[] ResolveCommittedSequenceAgainstEnemy(
        AttackPayload payload,
        EnemyHealth enemy,
        int packetCount,
        float baseImpactDuration,
        float packetInterval = 0f,
        float reactionTailDuration = 0.12f,
        bool previewPendingReadyEmpower = false)
    {
        int resolvedPacketCount = Mathf.Max(1, packetCount);
        CommittedEnemyHitPacket[] packets = new CommittedEnemyHitPacket[resolvedPacketCount];
        bool assignedReactionPacket = false;
        int targetCurrentHp = enemy != null ? Mathf.Max(1, enemy.CurrentHP) : 1;
        int targetDefense = enemy != null && enemy.Stats != null ? Mathf.Max(0, enemy.Stats.Defense) : 0;
        int cumulativeEffectiveDamage = 0;
        bool lethalSequence = false;

        for (int packetIndex = 0; packetIndex < resolvedPacketCount; packetIndex++)
        {
            CombatResolutionResult result = ResolveAgainstEnemy(
                payload,
                enemy,
                previewPendingReadyEmpower);
            bool playHitReaction = result.DidHit && !assignedReactionPacket;

            if (playHitReaction)
                assignedReactionPacket = true;

            if (result.DidHit)
            {
                int effectiveDamage = Mathf.Max(1, result.FinalDamage - targetDefense);
                cumulativeEffectiveDamage += effectiveDamage;
                if (cumulativeEffectiveDamage >= targetCurrentHp)
                    lethalSequence = true;
            }

            float reactionLockDuration = result.DidHit
                ? ResolvePacketReactionLockDuration(packetIndex, resolvedPacketCount, packetInterval, reactionTailDuration)
                : 0f;

            packets[packetIndex] = new CommittedEnemyHitPacket(
                didHit: result.DidHit,
                didSurge: result.DidSurge,
                finalDamage: result.FinalDamage,
                impactDuration: result.DidSurge ? baseImpactDuration * 1.25f : baseImpactDuration,
                commitDeath: false,
                playHitReaction: playHitReaction,
                finalizeSequenceDeath: false,
                reactionLockDuration: reactionLockDuration);
        }

        if (lethalSequence && resolvedPacketCount > 0)
        {
            int finalPacketIndex = resolvedPacketCount - 1;
            CommittedEnemyHitPacket finalPacket = packets[finalPacketIndex];
            packets[finalPacketIndex] = new CommittedEnemyHitPacket(
                didHit: finalPacket.DidHit,
                didSurge: finalPacket.DidSurge,
                finalDamage: finalPacket.FinalDamage,
                impactDuration: finalPacket.ImpactDuration,
                commitDeath: finalPacket.DidHit,
                playHitReaction: finalPacket.PlayHitReaction,
                finalizeSequenceDeath: true,
                reactionLockDuration: finalPacket.ReactionLockDuration);
        }

        return packets;
    }

    private static CombatFormulaProfile ResolveFormulaProfile(PlayerJobType sourceJobType)
    {
        PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(sourceJobType);
        return jobDefinition != null ? jobDefinition.CombatFormulaProfile : null;
    }

    private static float CalculateHitChance(AttackPayload payload, EnemyHealth enemy, CombatFormulaProfile formulaProfile)
    {
        float minimumHitChance = formulaProfile != null
            ? Mathf.Clamp01(formulaProfile.HitRules.MinHitChanceClamp)
            : 0.75f;
        float maximumHitChance = formulaProfile != null
            ? Mathf.Clamp01(formulaProfile.HitRules.MaxHitChanceClamp)
            : 0.98f;
        float hitRateWeight = formulaProfile != null
            ? Mathf.Max(0f, formulaProfile.HitRules.HitRateWeight)
            : 1f;
        float avoidanceWeight = formulaProfile != null
            ? Mathf.Max(0f, formulaProfile.HitRules.AvoidanceWeight)
            : 1f;
        float levelWeight = formulaProfile != null
            ? Mathf.Max(0f, formulaProfile.HitRules.LevelDeltaWeight)
            : 0.1f;
        float baseHitChance = formulaProfile != null
            ? Mathf.Clamp01(formulaProfile.HitRules.BaseHitChance)
            : 0.9f;
        float advantageDivisor = formulaProfile != null
            ? Mathf.Max(1f, formulaProfile.HitRules.HitAdvantageDivisor)
            : 100f;

        float targetAvoidance = ResolveTargetAvoidance(enemy);
        bool guaranteedHitWhenNoAvoidance = formulaProfile == null || formulaProfile.HitRules.GuaranteedHitWhenTargetHasNoAvoidance;
        if (targetAvoidance <= 0f && guaranteedHitWhenNoAvoidance)
            return 1f;

        float weightedHitRate = Mathf.Max(0f, payload.ResolvedHitRate) * hitRateWeight;
        float weightedAvoidance = targetAvoidance * avoidanceWeight;
        float weightedLevel = Mathf.Max(0, payload.SourceLevel) * levelWeight;

        float rawChance = baseHitChance + ((weightedHitRate + weightedLevel - weightedAvoidance) / advantageDivisor);
        return Mathf.Clamp(rawChance, minimumHitChance, maximumHitChance);
    }

    private static float CalculateSurgeChance(
        AttackPayload payload,
        CombatFormulaProfile formulaProfile,
        ReadyStateEmpowerDefinition readyStateEmpower)
    {
        float minimumSurgeChance = formulaProfile != null
            ? formulaProfile.SurgeRules.MinSurgeChanceClamp
            : 0f;
        float maximumSurgeChance = formulaProfile != null
            ? formulaProfile.SurgeRules.MaxSurgeChanceClamp
            : 100f;
        float resolvedChance = Mathf.Clamp(
            payload.FinalSurgeChance + readyStateEmpower.SurgeChanceBonus,
            minimumSurgeChance,
            maximumSurgeChance);
        return Mathf.Clamp01(resolvedChance / 100f);
    }

    private static float CalculateSurgePower(
        AttackPayload payload,
        CombatFormulaProfile formulaProfile,
        ReadyStateEmpowerDefinition readyStateEmpower)
    {
        float minimumSurgePower = formulaProfile != null
            ? Mathf.Max(0f, formulaProfile.SurgeRules.MinSurgePowerClamp)
            : 1f;
        float maximumSurgePower = formulaProfile != null
            ? Mathf.Max(minimumSurgePower, formulaProfile.SurgeRules.MaxSurgePowerClamp)
            : 5f;
        return Mathf.Clamp(payload.FinalSurgePower + readyStateEmpower.SurgePowerBonus, minimumSurgePower, maximumSurgePower);
    }

    private static float CalculateElementMultiplier(
        AttackPayload payload,
        EnemyHealth enemy)
    {
        if (payload == null || !payload.HasElement || payload.ElementType == CombatElementType.None)
            return 1f;

        CombatElementType targetElement = enemy != null && enemy.Stats != null
            ? enemy.Stats.ElementType
            : CombatElementType.None;
        if (targetElement == CombatElementType.None)
            return 1f;

        CombatElementRuleProfile elementRules = CombatElementRuleDatabase.GetProfile();
        float relationshipMultiplier = elementRules != null
            ? elementRules.ResolveMultiplier(payload.ElementType, targetElement)
            : 1f;
        float elementPowerMultiplier = Mathf.Max(0f, payload.ElementPower);

        if (Mathf.Approximately(elementPowerMultiplier, 0f))
            elementPowerMultiplier = 1f;

        return Mathf.Max(0f, relationshipMultiplier * elementPowerMultiplier);
    }

    private static ReadyStateEmpowerDefinition ResolveReadyStateEmpower(
        AttackPayload payload,
        bool previewPendingReadyEmpower)
    {
        if (payload == null || !payload.ReadyStateEmpower.IsConfigured)
            return default;

        if (payload.IsReadyStateEmpowerActive || previewPendingReadyEmpower)
            return payload.ReadyStateEmpower;

        return default;
    }

    private static float ResolveTargetAvoidance(EnemyHealth enemy)
    {
        if (enemy == null || enemy.Stats == null)
            return 0f;

        return Mathf.Max(0f, enemy.Stats.Avoidance);
    }

    private static float ResolvePacketReactionLockDuration(
        int packetIndex,
        int packetCount,
        float packetInterval,
        float reactionTailDuration)
    {
        int remainingPacketsAfterThis = Mathf.Max(0, packetCount - packetIndex - 1);
        float remainingSequenceTime = remainingPacketsAfterThis * Mathf.Max(0f, packetInterval);
        return Mathf.Max(0f, remainingSequenceTime + Mathf.Max(0f, reactionTailDuration));
    }
}
