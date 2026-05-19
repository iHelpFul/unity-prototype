using UnityEngine;

public sealed class PlayerSkillCastExecutionService
{
    private static readonly float DirectSkillImpactDuration = 0.06f;
    private static readonly float PlayerSkillProjectileImpactDuration = 0.04f;

    private PlayerCharacter character;
    private PlayerDirectHitExecutionService directHitExecutionService;
    private PlayerProjectileExecutionService projectileExecutionService;
    private PlayerAttackSequenceScheduler attackSequenceScheduler;
    private System.Action<PresentationCueSet, CombatCuePhase, Vector3> publishCue;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        PlayerDirectHitExecutionService directHitService,
        PlayerProjectileExecutionService projectileService,
        PlayerAttackSequenceScheduler sequenceScheduler,
        System.Action<PresentationCueSet, CombatCuePhase, Vector3> cuePublisher)
    {
        character = ownerCharacter;
        directHitExecutionService = directHitService;
        projectileExecutionService = projectileService;
        attackSequenceScheduler = sequenceScheduler;
        publishCue = cuePublisher;
    }

    public bool ExecuteSkillCast(PendingSkillCastRequest skillCast)
    {
        if (skillCast?.Definition == null)
            return false;

        if (skillCast.CommitAsBurstSkill)
        {
            if (skillCast.Payload == null)
                return false;
        }
        else
        {
            skillCast.Payload ??= BuildSkillPayload(skillCast.Definition, skillCast.ResolvedSkillLevel);
            if (skillCast.Payload == null)
                return false;
        }

        ConsumeMomentumForSkill(skillCast.Payload);
        if (skillCast.CommitAsBurstSkill)
        {
            bool burstExecuted = ExecuteBurstSkillCast(skillCast);
            if (burstExecuted)
                GainMomentumFromSkill(skillCast.Definition, skillCast.ResolvedSkillLevel);

            return burstExecuted;
        }

        character?.ActionStateController?.CommitSkill();
        publishCue?.Invoke(skillCast.Definition.PresentationCueSet, CombatCuePhase.Release, character.transform.position);

        bool executed = false;
        if (skillCast.Definition.ExecutionKind == CombatExecutionKind.Projectile)
        {
            executed = ExecuteProjectileSkillCast(skillCast.Definition, skillCast.ResolvedSkillLevel, skillCast.Payload);
        }
        else if (skillCast.Definition.CombatTargetingKind == CombatTargetingKind.Area)
        {
            executed = ExecuteAreaSkillHit(skillCast.Definition, skillCast.ResolvedSkillLevel, skillCast.Payload);
        }
        else
        {
            executed = ExecuteFrontSingleTargetSkillHit(skillCast);
        }

        if (executed)
            GainMomentumFromSkill(skillCast.Definition, skillCast.ResolvedSkillLevel);

        return executed;
    }

    private bool ExecuteBurstSkillCast(PendingSkillCastRequest skillCast)
    {
        if (skillCast?.Definition == null || skillCast.Payload == null)
            return false;

        if (skillCast.Definition.ExecutionKind == CombatExecutionKind.Projectile)
        {
            return ExecuteProjectileSkillCast(skillCast.Definition, skillCast.ResolvedSkillLevel, skillCast.Payload);
        }

        if (skillCast.Definition.CombatTargetingKind == CombatTargetingKind.Area)
            return ExecuteBurstAreaSkillHit(skillCast.Payload);

        if (skillCast.Payload.MaxTargets > 1)
            return ExecuteBurstFrontSweepSkillHit(skillCast.Definition, skillCast.Payload);

        return ExecuteFrontSingleTargetSkillHit(skillCast);
    }

    private bool ExecuteAreaSkillHit(PlayerSkillDefinition definition, int skillLevel, AttackPayload payload)
    {
        int hitCount = payload != null
            ? Mathf.Max(1, payload.ResolvedHitCount)
            : definition.GetResolvedHitCount(skillLevel);
        bool commitDeath = hitCount <= 1;

        if (!directHitExecutionService.ExecuteAreaHit(
                definition,
                skillLevel,
                payload,
                DirectSkillImpactDuration,
                commitDeath))
        {
            return false;
        }

        if (hitCount > 1)
            attackSequenceScheduler?.ScheduleRepeatedAreaHits(definition, skillLevel, hitCount - 1, payload);

        return true;
    }

    private bool ExecuteProjectileSkillCast(PlayerSkillDefinition definition, int skillLevel, AttackPayload payload)
    {
        if (definition == null)
            return false;

        attackSequenceScheduler?.CancelActiveSequence();

        if (definition.ProjectileLaunchMode != ProjectileLaunchMode.Locked)
            return ExecuteFreeProjectileSkillCast(definition, skillLevel, payload);

        EnemyHealth lockedTarget = directHitExecutionService.ResolveSingleTarget(
            definition,
            skillLevel,
            payload,
            null,
            true);
        int projectileCount = payload != null
            ? Mathf.Max(1, payload.ResolvedProjectileCount)
            : definition.GetResolvedProjectileCount(skillLevel);
        float projectileInterval = projectileExecutionService.GetResolvedProjectileShotInterval(definition, skillLevel);
        CommittedEnemyHitPacket[] committedPackets = lockedTarget != null
            ? directHitExecutionService.BuildCommittedHitPackets(
                payload,
                lockedTarget,
                projectileCount,
                PlayerSkillProjectileImpactDuration,
                projectileInterval)
            : null;

        projectileExecutionService.SpawnProjectileShot(
            definition,
            skillLevel,
            lockedTarget,
            0,
            projectileCount,
            payload,
            GetCommittedHitPacket(committedPackets, 0));

        if (projectileCount > 1)
            attackSequenceScheduler?.ScheduleProjectileShots(
                definition,
                skillLevel,
                lockedTarget,
                projectileCount,
                payload,
                committedPackets);

        return true;
    }

    private bool ExecuteFreeProjectileSkillCast(PlayerSkillDefinition definition, int skillLevel, AttackPayload payload)
    {
        if (definition == null || character == null)
            return false;

        int projectileCount = definition.GetResolvedProjectileCount(skillLevel);
        if (payload != null)
            projectileCount = Mathf.Max(1, payload.ResolvedProjectileCount);
        if (projectileCount <= 0)
            return false;

        projectileExecutionService.SpawnProjectileShot(
            definition,
            skillLevel,
            null,
            0,
            projectileCount,
            payload,
            null);

        if (projectileCount > 1)
            attackSequenceScheduler?.ScheduleProjectileShots(
                definition,
                skillLevel,
                null,
                projectileCount,
                payload,
                null);

        return true;
    }

    private bool ExecuteFrontSingleTargetSkillHit(PendingSkillCastRequest skillCast)
    {
        EnemyHealth target = directHitExecutionService.ResolveSingleTarget(
            skillCast.Definition,
            skillCast.ResolvedSkillLevel,
            skillCast.Payload,
            skillCast.LockedTarget,
            true);
        if (target == null)
            return false;

        if (directHitExecutionService.TryExecuteAuthoritativeSkillSequence(
                skillCast.Definition,
                skillCast.ResolvedSkillLevel,
                target))
        {
            skillCast.LockedTarget = target;
            return true;
        }

        int totalHits = skillCast.Payload != null
            ? Mathf.Max(1, skillCast.Payload.ResolvedHitCount)
            : Mathf.Max(1, skillCast.Definition.GetResolvedHitCount(skillCast.ResolvedSkillLevel));
        float hitInterval = Mathf.Max(0.01f, skillCast.Definition.GetResolvedHitInterval(skillCast.ResolvedSkillLevel));
        skillCast.CommittedHitPackets ??= directHitExecutionService.BuildCommittedHitPackets(
            skillCast.Payload,
            target,
            totalHits,
            DirectSkillImpactDuration,
            hitInterval);
        directHitExecutionService.ApplyCommittedHitPacket(
            target,
            skillCast.Definition,
            skillCast.Payload,
            skillCast.CommittedHitPackets,
            0);
        skillCast.LockedTarget = target;

        int remainingHits = Mathf.Max(0, totalHits - 1);
        if (remainingHits > 0)
        {
            attackSequenceScheduler?.ScheduleRepeatedSingleTargetHits(
                skillCast.Definition,
                skillCast.ResolvedSkillLevel,
                target,
                remainingHits,
                skillCast.Payload,
                skillCast.CommittedHitPackets);
        }

        return true;
    }

    private bool ExecuteBurstFrontSweepSkillHit(PlayerSkillDefinition definition, AttackPayload payload)
    {
        return directHitExecutionService.ExecuteMultiTargetHitBox(
            payload,
            definition,
            DirectSkillImpactDuration);
    }

    private bool ExecuteBurstAreaSkillHit(AttackPayload payload)
    {
        return directHitExecutionService.ExecuteAreaHit(
            null,
            1,
            payload,
            DirectSkillImpactDuration,
            true);
    }

    private AttackPayload BuildSkillPayload(PlayerSkillDefinition definition, int skillLevel)
    {
        if (definition == null || character == null)
            return null;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        return AttackPayloadBuilder.BuildSkillPayload(character, snapshot, definition, skillLevel);
    }

    private void GainMomentumFromSkill(PlayerSkillDefinition definition, int skillLevel)
    {
        if (character == null || definition == null)
            return;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int totalMomentumGain = Mathf.Max(0, definition.GetResolvedMomentumGain(skillLevel) + snapshot.MomentumGainBonus);
        character.GainCombatMomentum(totalMomentumGain);
    }

    private void ConsumeMomentumForSkill(AttackPayload payload)
    {
        if (character == null || payload == null)
            return;

        character.ConsumeCombatMomentum(payload.MomentumToConsume);
    }

    private static CommittedEnemyHitPacket? GetCommittedHitPacket(
        CommittedEnemyHitPacket[] committedPackets,
        int index)
    {
        if (committedPackets == null || index < 0 || index >= committedPackets.Length)
            return null;

        return committedPackets[index];
    }
}
