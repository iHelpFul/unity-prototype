using UnityEngine;

public class PlayerHitApplicationService
{
    public void ApplyHit(
        EnemyHealth enemy,
        PlayerCharacter owner,
        float sourcePositionX,
        float zeroDirectionFallback,
        int localFallbackDamage,
        float impactDuration,
        bool commitDeath = true,
        string skillId = "")
    {
        if (enemy == null || enemy.IsDead)
            return;

        float direction = Mathf.Sign(enemy.transform.position.x - sourcePositionX);
        if (Mathf.Approximately(direction, 0f) && !Mathf.Approximately(zeroDirectionFallback, 0f))
            direction = Mathf.Sign(zeroDirectionFallback);

        EventBus.Publish(new HitImpactEvent
        {
            Duration = impactDuration,
            TimeScale = 0.1f,
            Damage = 0
        });

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
            return;
        }

        enemy.TakeDamage(
            Mathf.Max(1, localFallbackDamage),
            direction,
            owner,
            commitDeath);
    }
}
