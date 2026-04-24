using UnityEngine;

public class PlayerHitApplicationService
{
    public bool ApplyCommittedHit(
        EnemyHealth enemy,
        PlayerCharacter owner,
        float sourcePositionX,
        float zeroDirectionFallback,
        CommittedEnemyHitPacket packet,
        PresentationCueSet cueSet = null,
        Transform projectile = null)
    {
        if (enemy == null)
            return false;

        float direction = !Mathf.Approximately(zeroDirectionFallback, 0f)
            ? Mathf.Sign(zeroDirectionFallback)
            : Mathf.Sign(enemy.transform.position.x - sourcePositionX);

        if (Mathf.Approximately(direction, 0f))
            direction = 1f;

        CombatPresentationDispatcher.PublishEnemyResult(new CombatPresentationRequest(
            context: new CombatPresentationContext(
                enemy.transform.position,
                owner != null ? owner.transform : null,
                enemy.transform,
                projectile),
            didHit: packet.DidHit,
            didSurge: packet.DidSurge,
            damage: packet.FinalDamage,
            impactDuration: packet.ImpactDuration,
            cueSet: cueSet,
            playImpactFeedback: packet.DidHit,
            playDamageNumber: true));

        if (!packet.DidHit)
        {
            TryFinalizeCommittedSequenceDeath(enemy, owner, direction, packet);
            return false;
        }

        enemy.NotifyHitReactionLock(packet.ReactionLockDuration);
        enemy.TakeDamage(
            packet.FinalDamage,
            direction,
            owner,
            packet.CommitDeath,
            publishDamageFeedback: false,
            playHitReaction: packet.PlayHitReaction);

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

        float direction = !Mathf.Approximately(zeroDirectionFallback, 0f)
            ? Mathf.Sign(zeroDirectionFallback)
            : Mathf.Sign(enemy.transform.position.x - sourcePositionX);

        if (Mathf.Approximately(direction, 0f))
            direction = 1f;

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
            CombatResolutionResult resolution = CombatResolver.ResolveAgainstEnemy(payload, enemy);
            CombatPresentationDispatcher.PublishEnemyResult(new CombatPresentationRequest(
                context: new CombatPresentationContext(
                    enemy.transform.position,
                    owner != null ? owner.transform : null,
                    enemy.transform,
                    projectile),
                didHit: resolution.DidHit,
                didSurge: resolution.DidSurge,
                damage: resolution.FinalDamage,
                impactDuration: impactDuration,
                cueSet: cueSet != null ? cueSet : CombatPresentationDispatcher.ResolveCueSet(payload),
                playImpactFeedback: resolution.DidHit,
                playDamageNumber: true));

            if (!resolution.DidHit)
                return false;

            enemy.TakeDamage(
                resolution.FinalDamage,
                direction,
                owner,
                commitDeath,
                publishDamageFeedback: false,
                playHitReaction: true);
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
