using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitApplicationService
{
    public bool ApplyCommittedHit(
        EnemyHealth enemy,
        PlayerCharacter owner,
        float sourcePositionX,
        float zeroDirectionFallback,
        CommittedEnemyHitPacket packet,
        AttackPayload payload = null,
        PresentationCueSet cueSet = null,
        Transform projectile = null)
    {
        if (enemy == null)
            return false;

        float direction = ResolveHitDirection(enemy, sourcePositionX, zeroDirectionFallback);
        bool previewReadyEmpower = ShouldPreviewReadyEmpower(owner, payload);

        CombatPresentationDispatcher.PublishEnemyResult(new CombatPresentationRequest(
            context: new CombatPresentationContext(
                enemy.transform.position,
                owner != null ? owner.transform : null,
                enemy.transform,
                projectile),
            didHit: packet.DidHit,
            didSurge: packet.DidSurge,
            damage: enemy.PreviewDisplayedDamage(packet.FinalDamage),
            impactDuration: packet.ImpactDuration,
            cueSet: cueSet,
            playImpactFeedback: packet.DidHit,
            playDamageNumber: true));

        if (!packet.DidHit)
        {
            TryFinalizeCommittedSequenceDeath(enemy, owner, direction, packet);
            return false;
        }

        ReadyStateEmpowerDefinition readyStateEmpower = ResolveActiveReadyStateEmpower(owner, payload, previewReadyEmpower);

        float resolvedBreakPower = ResolveBreakPower(payload, readyStateEmpower);
        bool didApplyBreak = resolvedBreakPower > 0f;
        if (didApplyBreak)
            enemy.ApplyBreakFromPower(resolvedBreakPower, owner);

        enemy.TakeDamage(
            packet.FinalDamage,
            direction,
            owner,
            packet.CommitDeath,
            publishDamageFeedback: false,
            playHitReaction: packet.PlayHitReaction && !didApplyBreak);

        ApplyReadyStateEmpower(
            enemy,
            owner,
            payload,
            readyStateEmpower,
            projectile);

        TryFinalizeCommittedSequenceDeath(enemy, owner, direction, packet);
        return true;
    }

    public bool ApplyHit(
        EnemyHealth enemy,
        PlayerCharacter owner,
        float sourcePositionX,
        float zeroDirectionFallback,
        int localFallbackDamage,
        float impactDuration,
        bool commitDeath = true,
        string skillId = "",
        AttackPayload payload = null,
        PresentationCueSet cueSet = null,
        Transform projectile = null)
    {
        if (enemy == null || enemy.IsDead)
            return false;

        float direction = ResolveHitDirection(enemy, sourcePositionX, zeroDirectionFallback);
        string resolvedSkillId = string.IsNullOrWhiteSpace(skillId)
            ? string.Empty
            : skillId.Trim();

        if (MultiplayerPrototypeEnemyCoordinator.TryRequestDamage(
            enemy,
            direction,
            owner,
            commitDeath,
            resolvedSkillId))
        {
            return true;
        }

        if (payload != null)
        {
            bool previewReadyEmpower = ShouldPreviewReadyEmpower(owner, payload);
            CombatResolutionResult resolution = CombatResolver.ResolveAgainstEnemy(
                payload,
                enemy,
                previewPendingReadyEmpower: previewReadyEmpower);

            CombatPresentationDispatcher.PublishEnemyResult(new CombatPresentationRequest(
                context: new CombatPresentationContext(
                    enemy.transform.position,
                    owner != null ? owner.transform : null,
                    enemy.transform,
                    projectile),
                didHit: resolution.DidHit,
                didSurge: resolution.DidSurge,
                damage: enemy.PreviewDisplayedDamage(resolution.FinalDamage),
                impactDuration: impactDuration,
                cueSet: cueSet != null ? cueSet : CombatPresentationDispatcher.ResolveCueSet(payload),
                playImpactFeedback: resolution.DidHit,
                playDamageNumber: true));

            if (!resolution.DidHit)
                return false;

            ReadyStateEmpowerDefinition readyStateEmpower = ResolveActiveReadyStateEmpower(owner, payload, previewReadyEmpower);
            float resolvedBreakPower = ResolveBreakPower(payload, readyStateEmpower);
            bool didApplyBreak = resolvedBreakPower > 0f;
            if (didApplyBreak)
                enemy.ApplyBreakFromPower(resolvedBreakPower, owner);

            enemy.TakeDamage(
                resolution.FinalDamage,
                direction,
                owner,
                commitDeath,
                publishDamageFeedback: false,
                playHitReaction: !didApplyBreak);

            ApplyReadyStateEmpower(
                enemy,
                owner,
                payload,
                readyStateEmpower,
                projectile);
            return true;
        }

        EventBus.Publish(new HitImpactEvent
        {
            Duration = impactDuration,
            TimeScale = 0.1f,
            Damage = 0
        });

        enemy.TakeDamage(
            Mathf.Max(1, localFallbackDamage),
            direction,
            owner,
            commitDeath);
        return true;
    }

    private static float ResolveHitDirection(EnemyHealth enemy, float sourcePositionX, float zeroDirectionFallback)
    {
        float direction = !Mathf.Approximately(zeroDirectionFallback, 0f)
            ? Mathf.Sign(zeroDirectionFallback)
            : Mathf.Sign(enemy.transform.position.x - sourcePositionX);

        if (Mathf.Approximately(direction, 0f))
            direction = 1f;

        return direction;
    }

    private static bool ShouldPreviewReadyEmpower(PlayerCharacter owner, AttackPayload payload)
    {
        return owner != null
            && payload != null
            && payload.CanAttemptReadyStateEmpower
            && owner.HasMatchingReadyState(payload.PendingReadyStateType);
    }

    private static ReadyStateEmpowerDefinition ResolveActiveReadyStateEmpower(
        PlayerCharacter owner,
        AttackPayload payload,
        bool previewReadyEmpower)
    {
        if (payload == null || !payload.ReadyStateEmpower.IsConfigured)
            return default;

        if (payload.IsReadyStateEmpowerActive)
            return payload.ReadyStateEmpower;

        if (!previewReadyEmpower || owner == null)
            return default;

        if (payload.ReadyStateEmpower.ConsumeReadyStateOnSuccessfulHit
            && !owner.TryConsumeReadyState(payload.PendingReadyStateType))
        {
            return default;
        }

        return payload.TryActivateReadyStateEmpower()
            ? payload.ReadyStateEmpower
            : default;
    }

    private static float ResolveBreakPower(AttackPayload payload, ReadyStateEmpowerDefinition readyStateEmpower)
    {
        if (payload == null || payload.ResolvedBreakPower <= 0f)
            return 0f;

        return Mathf.Max(0f, payload.ResolvedBreakPower * readyStateEmpower.BreakPowerMultiplier);
    }

    private void ApplyReadyStateEmpower(
        EnemyHealth enemy,
        PlayerCharacter owner,
        AttackPayload payload,
        ReadyStateEmpowerDefinition readyStateEmpower,
        Transform projectile)
    {
        if (enemy == null || payload == null || !readyStateEmpower.IsConfigured)
            return;

        ReadyStateAreaBonusDefinition areaBonus = readyStateEmpower.BonusAreaOnSuccessfulHit;
        if (!areaBonus.IsConfigured)
            return;

        AttackPayload bonusPayload = payload.CreateTriggeredAreaBonusPayload(areaBonus);
        if (bonusPayload == null)
            return;

        string bonusSkillId = string.IsNullOrWhiteSpace(payload.ActionId)
            ? string.Empty
            : $"{payload.ActionId}_ready_bonus";

        if (areaBonus.PulseCount <= 1 || areaBonus.PulseInterval <= 0f || owner == null)
        {
            ApplyReadyStateAreaPulse(enemy, owner, bonusPayload, areaBonus, bonusSkillId, projectile);
            return;
        }

        owner.StartCoroutine(ApplyReadyStateAreaPulses(
            enemy,
            owner,
            bonusPayload,
            areaBonus,
            bonusSkillId,
            projectile));
    }

    private IEnumerator ApplyReadyStateAreaPulses(
        EnemyHealth primaryEnemy,
        PlayerCharacter owner,
        AttackPayload bonusPayload,
        ReadyStateAreaBonusDefinition areaBonus,
        string skillId,
        Transform projectile)
    {
        int pulseCount = Mathf.Max(1, areaBonus.PulseCount);
        float pulseInterval = Mathf.Max(0f, areaBonus.PulseInterval);

        for (int pulseIndex = 0; pulseIndex < pulseCount; pulseIndex++)
        {
            ApplyReadyStateAreaPulse(primaryEnemy, owner, bonusPayload, areaBonus, skillId, projectile);
            if (pulseIndex >= pulseCount - 1 || pulseInterval <= 0f)
                continue;

            yield return new WaitForSeconds(pulseInterval);
        }
    }

    private void ApplyReadyStateAreaPulse(
        EnemyHealth primaryEnemy,
        PlayerCharacter owner,
        AttackPayload bonusPayload,
        ReadyStateAreaBonusDefinition areaBonus,
        string skillId,
        Transform projectile)
    {
        if (primaryEnemy == null || bonusPayload == null)
            return;

        List<EnemyHealth> targets = CollectReadyStateAreaTargets(primaryEnemy, areaBonus);
        for (int index = 0; index < targets.Count; index++)
        {
            EnemyHealth target = targets[index];
            if (target == null || target.IsDead)
                continue;

            ApplyHit(
                target,
                owner,
                primaryEnemy.transform.position.x,
                0f,
                localFallbackDamage: 1,
                impactDuration: 0.04f,
                commitDeath: true,
                skillId: skillId,
                payload: bonusPayload,
                cueSet: null,
                projectile: projectile);
        }
    }

    private static List<EnemyHealth> CollectReadyStateAreaTargets(
        EnemyHealth primaryEnemy,
        ReadyStateAreaBonusDefinition areaBonus)
    {
        List<EnemyHealth> targets = new List<EnemyHealth>();
        if (primaryEnemy == null)
            return targets;

        CombatHitBoxWorldQuery query = areaBonus.HitBox.BuildWorldQuery(primaryEnemy.transform, areaBonus.Range);
        int layerMask = 1 << primaryEnemy.gameObject.layer;
        Collider[] overlaps = Physics.OverlapBox(
            query.Center,
            query.HalfExtents,
            query.Rotation,
            layerMask,
            QueryTriggerInteraction.Collide);
        HashSet<EnemyHealth> uniqueTargets = new HashSet<EnemyHealth>();

        for (int index = 0; index < overlaps.Length; index++)
        {
            EnemyHealth candidate = overlaps[index].GetComponentInParent<EnemyHealth>();
            if (candidate == null || candidate.IsDead)
                continue;

            if (!areaBonus.IncludePrimaryTarget && candidate == primaryEnemy)
                continue;

            if (!uniqueTargets.Add(candidate))
                continue;

            targets.Add(candidate);
        }

        targets.Sort((left, right) =>
        {
            float leftDistance = (left.transform.position - query.Center).sqrMagnitude;
            float rightDistance = (right.transform.position - query.Center).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        if (targets.Count > areaBonus.MaxTargets)
            targets.RemoveRange(areaBonus.MaxTargets, targets.Count - areaBonus.MaxTargets);

        return targets;
    }

    private static void TryFinalizeCommittedSequenceDeath(
        EnemyHealth enemy,
        PlayerCharacter owner,
        float direction,
        CommittedEnemyHitPacket packet)
    {
        if (!packet.FinalizeSequenceDeath || enemy == null || enemy.IsDead || enemy.CurrentHP > 1)
            return;

        enemy.TakeDamage(
            1,
            direction,
            owner,
            commitDeath: true,
            publishDamageFeedback: false,
            playHitReaction: false);
    }
}
