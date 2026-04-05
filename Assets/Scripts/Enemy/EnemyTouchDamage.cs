using System.Collections.Generic;
using UnityEngine;

public class EnemyTouchDamage : MonoBehaviour
{
    [SerializeField] private float damageCooldown = 1.0f;
    [SerializeField] private float touchRadius = 0.8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float colliderCenter;

    private int contactDamage = 5;
    private readonly Dictionary<PlayerCharacter, float> nextDamageTimes = new Dictionary<PlayerCharacter, float>();

    private EnemyAI enemyAI;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        enemyHealth = GetComponent<EnemyHealth>();
        RefreshConfiguredStats();
    }

    private void OnValidate()
    {
        damageCooldown = Mathf.Max(0.05f, damageCooldown);
        touchRadius = Mathf.Max(0.05f, touchRadius);
        colliderCenter = Mathf.Max(0f, colliderCenter);
        RefreshConfiguredStats();
    }

    private void Update()
    {
        if (MultiplayerPrototypeRuntime.IsEnabled)
            return;

        if (enemyAI != null && enemyAI.IsDead)
            return;

        CheckForPlayerContact();
    }

    private void CheckForPlayerContact()
    {
        Vector3 checkOrigin = transform.position + Vector3.up * colliderCenter;
        Collider[] hitColliders = Physics.OverlapSphere(checkOrigin, touchRadius, playerLayer);

        if (hitColliders.Length == 0)
            return;

        float currentTime = Time.time;

        foreach (Collider hitCollider in hitColliders)
        {
            PlayerCharacter target = hitCollider.GetComponentInParent<PlayerCharacter>();
            if (!IsDamageableTarget(target))
                continue;

            if (nextDamageTimes.TryGetValue(target, out float nextAllowedTime) && currentTime < nextAllowedTime)
                continue;

            nextDamageTimes[target] = currentTime + damageCooldown;

            EventBus.Publish(new PlayerDamagedEvent
            {
                Target = target,
                CharacterId = target.CharacterId,
                Source = transform,
                Damage = contactDamage,
                HitDirection = transform.position.x
            });
        }
    }

    public void ResetAfterRespawn()
    {
        nextDamageTimes.Clear();
        RefreshConfiguredStats();
    }

    public void RefreshConfiguredStats()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth != null && enemyHealth.Stats != null)
            contactDamage = enemyHealth.Stats.ContactDamage;
    }

    private bool IsDamageableTarget(PlayerCharacter target)
    {
        return target != null
            && target.IsLocalPlayer
            && target.isActiveAndEnabled
            && !target.IsDead;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderCenter, touchRadius);
    }
}
