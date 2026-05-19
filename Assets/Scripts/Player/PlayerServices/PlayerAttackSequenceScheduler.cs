using System.Collections;
using UnityEngine;

public sealed class PlayerAttackSequenceScheduler
{
    private MonoBehaviour coroutineHost;
    private PlayerCharacter ownerCharacter;
    private PlayerDirectHitExecutionService directHitExecutionService;
    private PlayerProjectileExecutionService projectileExecutionService;
    private System.Action sequenceCompletedCallback;
    private Coroutine activeRoutine;

    public bool HasActiveSequence => activeRoutine != null;

    public void Initialize(
        MonoBehaviour host,
        PlayerCharacter character,
        PlayerDirectHitExecutionService directHitService,
        PlayerProjectileExecutionService projectileService,
        System.Action onSequenceCompleted)
    {
        coroutineHost = host;
        ownerCharacter = character;
        directHitExecutionService = directHitService;
        projectileExecutionService = projectileService;
        sequenceCompletedCallback = onSequenceCompleted;
    }

    public void CancelActiveSequence()
    {
        if (activeRoutine != null && coroutineHost != null)
            coroutineHost.StopCoroutine(activeRoutine);

        activeRoutine = null;
    }

    public void ScheduleProjectileShots(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth lockedTarget,
        int projectileCount,
        AttackPayload payload,
        CommittedEnemyHitPacket[] committedPackets)
    {
        if (coroutineHost == null || projectileExecutionService == null || projectileCount <= 1)
            return;

        CancelActiveSequence();
        activeRoutine = coroutineHost.StartCoroutine(PerformQueuedProjectileShots(
            definition,
            skillLevel,
            lockedTarget,
            projectileCount,
            payload,
            committedPackets));
    }

    public void ScheduleRepeatedSingleTargetHits(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth initialTarget,
        int remainingHits,
        AttackPayload payload,
        CommittedEnemyHitPacket[] committedPackets)
    {
        if (coroutineHost == null || directHitExecutionService == null || remainingHits <= 0)
            return;

        CancelActiveSequence();
        activeRoutine = coroutineHost.StartCoroutine(PerformRepeatedSingleTargetSkillHits(
            definition,
            skillLevel,
            initialTarget,
            remainingHits,
            payload,
            committedPackets));
    }

    public void ScheduleRepeatedAreaHits(
        PlayerSkillDefinition definition,
        int skillLevel,
        int remainingHits,
        AttackPayload payload)
    {
        if (coroutineHost == null || directHitExecutionService == null || remainingHits <= 0)
            return;

        CancelActiveSequence();
        activeRoutine = coroutineHost.StartCoroutine(PerformRepeatedAreaSkillHits(
            definition,
            skillLevel,
            remainingHits,
            payload));
    }

    private IEnumerator PerformQueuedProjectileShots(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth lockedTarget,
        int projectileCount,
        AttackPayload payload,
        CommittedEnemyHitPacket[] committedPackets)
    {
        float interval = projectileExecutionService.GetResolvedProjectileShotInterval(definition, skillLevel);

        for (int projectileIndex = 1; projectileIndex < projectileCount; projectileIndex++)
        {
            yield return new WaitForSeconds(interval);

            if (ownerCharacter == null || ownerCharacter.IsDead)
                break;

            projectileExecutionService.SpawnProjectileShot(
                definition,
                skillLevel,
                lockedTarget,
                projectileIndex,
                projectileCount,
                payload,
                GetCommittedHitPacket(committedPackets, projectileIndex));
        }

        FinishSequence();
    }

    private IEnumerator PerformRepeatedSingleTargetSkillHits(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth initialTarget,
        int remainingHits,
        AttackPayload payload,
        CommittedEnemyHitPacket[] committedPackets)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.GetResolvedHitInterval(skillLevel)));

            if (ownerCharacter == null || ownerCharacter.IsDead)
                break;

            EnemyHealth target = directHitExecutionService.ResolveSingleTarget(
                definition,
                skillLevel,
                payload,
                initialTarget,
                false);
            if (target == null)
                break;

            directHitExecutionService.ApplyCommittedHitPacket(
                target,
                definition,
                payload,
                committedPackets,
                hitIndex + 1);
        }

        FinishSequence();
    }

    private IEnumerator PerformRepeatedAreaSkillHits(
        PlayerSkillDefinition definition,
        int skillLevel,
        int remainingHits,
        AttackPayload payload)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.GetResolvedHitInterval(skillLevel)));

            if (ownerCharacter == null || ownerCharacter.IsDead)
                break;

            if (!directHitExecutionService.ExecuteAreaHit(
                    definition,
                    skillLevel,
                    payload,
                    0.045f,
                    hitIndex >= remainingHits - 1))
            {
                break;
            }
        }

        FinishSequence();
    }

    private void FinishSequence()
    {
        activeRoutine = null;
        sequenceCompletedCallback?.Invoke();
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
