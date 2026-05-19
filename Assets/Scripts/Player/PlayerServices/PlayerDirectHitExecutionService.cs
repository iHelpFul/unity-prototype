using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerDirectHitExecutionService
{
    private PlayerCharacter owner;
    private Transform ownerTransform;
    private PlayerTargetingService targetingService;
    private PlayerHitApplicationService hitApplicationService;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        Transform sourceTransform,
        PlayerTargetingService resolvedTargetingService,
        PlayerHitApplicationService resolvedHitApplicationService)
    {
        owner = ownerCharacter;
        ownerTransform = sourceTransform != null ? sourceTransform : ownerCharacter != null ? ownerCharacter.transform : null;
        targetingService = resolvedTargetingService;
        hitApplicationService = resolvedHitApplicationService;
    }

    public bool TryHitBasicAttack(
        PlayerBasicAttackProfile profile,
        AttackPayload payload,
        int maxTargets,
        float impactDuration)
    {
        if (profile == null || payload == null || targetingService == null)
            return false;

        List<EnemyHealth> enemies = targetingService.GetEnemiesInHitBox(
            payload.ResolvedHitBox,
            payload.ResolvedRange,
            maxTargets);
        int hitsApplied = 0;

        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyHealth enemy = enemies[index];
            if (!ApplyHitToEnemy(enemy, impactDuration, true, null, payload))
                continue;

            hitsApplied++;

            if (profile.TargetingKind == CombatTargetingKind.SingleTarget
                || hitsApplied >= maxTargets)
            {
                break;
            }
        }

        return hitsApplied > 0;
    }

    public bool ExecuteAreaHit(
        PlayerSkillDefinition definition,
        int skillLevel,
        AttackPayload payload,
        float impactDuration,
        bool commitDeath)
    {
        if (targetingService == null)
            return false;

        List<EnemyHealth> targets = payload != null
            ? CollectTargets(payload, payload.MaxTargets)
            : targetingService.FindSkillAreaTargets(definition, skillLevel);
        if (targets.Count == 0)
            return false;

        return ApplyHitsToTargets(
            targets,
            definition,
            payload,
            impactDuration,
            commitDeath,
            out _);
    }

    public bool ExecuteMultiTargetHitBox(
        AttackPayload payload,
        PlayerSkillDefinition definition,
        float impactDuration,
        bool commitDeath = true)
    {
        if (payload == null || targetingService == null)
            return false;

        List<EnemyHealth> targets = CollectTargets(payload, payload.MaxTargets);
        if (targets.Count == 0)
            return false;

        int hitsApplied = 0;
        int maxTargets = Mathf.Max(1, payload.MaxTargets);
        for (int index = 0; index < targets.Count; index++)
        {
            EnemyHealth target = targets[index];
            if (!ApplyHitToEnemy(target, impactDuration, commitDeath, definition, payload, 1))
                continue;

            hitsApplied++;
            if (hitsApplied >= maxTargets)
                break;
        }

        return hitsApplied > 0;
    }

    public EnemyHealth ResolveSingleTarget(
        PlayerSkillDefinition definition,
        int skillLevel,
        AttackPayload payload,
        EnemyHealth lockedTarget,
        bool allowReacquire)
    {
        if (targetingService == null)
            return null;

        if (payload != null)
            return ResolveSingleTargetFromPayload(payload, lockedTarget, allowReacquire);

        return targetingService.ResolveLockedSkillTarget(
                definition,
                lockedTarget,
                allowReacquire,
                skillLevel);
    }

    public EnemyHealth ResolveSingleTarget(
        AttackPayload payload,
        EnemyHealth lockedTarget,
        bool allowReacquire)
    {
        if (targetingService == null || payload == null)
            return null;

        return ResolveSingleTargetFromPayload(payload, lockedTarget, allowReacquire);
    }

    public List<EnemyHealth> CollectTargets(AttackPayload payload, int maxTargets = int.MaxValue)
    {
        if (targetingService == null || payload == null)
            return new List<EnemyHealth>();

        List<EnemyHealth> targets = targetingService.GetEnemiesInHitBox(
            payload.ResolvedHitBox,
            payload.ResolvedRange,
            int.MaxValue);
        return TrimTargets(targets, maxTargets);
    }

    public bool TryExecuteAuthoritativeSkillSequence(
        PlayerSkillDefinition definition,
        int skillLevel,
        EnemyHealth target)
    {
        if (!SupportsAuthoritativeSkillSequence(definition, skillLevel) || target == null || owner == null || ownerTransform == null)
            return false;

        float direction = Mathf.Sign(target.transform.position.x - ownerTransform.position.x);
        return MultiplayerPrototypeEnemyCoordinator.TryRequestSkillSequence(
            target,
            direction,
            owner,
            definition.SkillId);
    }

    public void ApplyHitsToTargets(
        List<EnemyHealth> targets,
        PlayerSkillDefinition definition,
        AttackPayload payload,
        float impactDuration,
        bool commitDeath)
    {
        if (targets == null)
            return;

        ApplyHitsToTargets(targets, definition, payload, impactDuration, commitDeath, out _);
    }

    private bool ApplyHitsToTargets(
        List<EnemyHealth> targets,
        PlayerSkillDefinition definition,
        AttackPayload payload,
        float impactDuration,
        bool commitDeath,
        out int hitsApplied)
    {
        hitsApplied = 0;
        if (targets == null)
            return false;

        for (int index = 0; index < targets.Count; index++)
        {
            EnemyHealth target = targets[index];
            if (!ApplyHitToEnemy(target, impactDuration, commitDeath, definition, payload, 1))
                continue;

            hitsApplied++;
        }

        return hitsApplied > 0;
    }

    public bool ApplyHitToEnemy(
        EnemyHealth enemy,
        float impactDuration,
        bool commitDeath = true,
        PlayerSkillDefinition skillDefinition = null,
        AttackPayload payload = null,
        int localFallbackDamage = 1)
    {
        if (owner == null || ownerTransform == null || hitApplicationService == null)
            return false;

        return hitApplicationService.ApplyHit(
            enemy,
            owner,
            ownerTransform.position.x,
            0f,
            Mathf.Max(1, localFallbackDamage),
            impactDuration,
            commitDeath,
            skillDefinition != null ? skillDefinition.SkillId : string.Empty,
            payload,
            null,
            null);
    }

    public CommittedEnemyHitPacket[] BuildCommittedHitPackets(
        AttackPayload payload,
        EnemyHealth target,
        int packetCount,
        float impactDuration,
        float packetInterval)
    {
        if (payload == null || target == null)
            return null;

        return CombatResolver.ResolveCommittedSequenceAgainstEnemy(
            payload,
            target,
            Mathf.Max(1, packetCount),
            impactDuration,
            packetInterval,
            ResolveEnemyReactionLockTail(impactDuration),
            previewPendingReadyEmpower: payload != null && payload.CanAttemptReadyStateEmpower);
    }

    public bool ApplyCommittedHitPacket(
        EnemyHealth target,
        PlayerSkillDefinition definition,
        AttackPayload payload,
        CommittedEnemyHitPacket[] committedPackets,
        int packetIndex)
    {
        if (owner == null || ownerTransform == null || hitApplicationService == null || target == null || committedPackets == null)
            return false;

        if (packetIndex < 0 || packetIndex >= committedPackets.Length)
            return false;

        return hitApplicationService.ApplyCommittedHit(
            target,
            owner,
            ownerTransform.position.x,
            0f,
            committedPackets[packetIndex],
            payload,
            definition != null
                ? definition.ResolvePresentationCueSet(
                    payload != null ? payload.ActionKind : AttackPayloadActionKind.Skill)
                : null,
            null);
    }

    private EnemyHealth ResolveSingleTargetFromPayload(
        AttackPayload payload,
        EnemyHealth lockedTarget,
        bool allowReacquire)
    {
        if (payload == null || targetingService == null)
            return null;

        List<EnemyHealth> candidates = CollectTargets(payload, int.MaxValue);
        if (candidates.Count == 0)
            return null;

        if (lockedTarget != null && candidates.Contains(lockedTarget))
            return lockedTarget;

        return allowReacquire ? candidates[0] : null;
    }

    private static List<EnemyHealth> TrimTargets(List<EnemyHealth> targets, int maxTargets)
    {
        if (targets == null)
            return new List<EnemyHealth>();

        int resolvedMaxTargets = maxTargets <= 0 ? int.MaxValue : maxTargets;
        if (resolvedMaxTargets == int.MaxValue || targets.Count <= resolvedMaxTargets)
            return targets;

        targets.RemoveRange(resolvedMaxTargets, targets.Count - resolvedMaxTargets);
        return targets;
    }

    private static bool SupportsAuthoritativeSkillSequence(PlayerSkillDefinition definition, int skillLevel)
    {
        return MultiplayerPrototypeRuntime.IsEnabled
            && definition != null
            && definition.GetResolvedHitCount(skillLevel) > 1
            && definition.CombatTargetingKind == CombatTargetingKind.SingleTarget;
    }

    private static float ResolveEnemyReactionLockTail(float impactDuration)
    {
        return Mathf.Max(0.14f, impactDuration * 2f);
    }
}
