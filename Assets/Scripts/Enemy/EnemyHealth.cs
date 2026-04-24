using System.Collections;
using System.Text;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;
    [SerializeField] private EnemyAnimationController animationController;

    [Header("Hit Feedback")]
    [SerializeField] private bool enableHitKnockback;
    [SerializeField] private float hitKnockbackForce = 0.18f;
    [SerializeField] private float hitKnockbackDuration = 0.08f;
    [SerializeField] private int hitKnockbackDamageThreshold = 999999;
    [SerializeField] private float hitReactionMovementLockDuration = 0.16f;

    private int currentHP;
    private bool isDead;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private CharacterController characterController;
    private EnemyAI enemyAI;
    private EnemyTouchDamage enemyTouchDamage;
    private Renderer[] cachedRenderers;
    private Coroutine respawnRoutine;
    private PlayerCharacter lastAttacker;
    private FieldSpawnDirector spawnDirector;
    private bool isVisible = true;
    private bool hasSpawnIdentity;

    public EnemyStats Stats => stats;
    public bool IsDead => isDead;
    public int CurrentHP => Mathf.Max(0, currentHP);
    public bool IsVisible => isVisible;
    public Vector3 SpawnPosition => spawnPosition;
    public bool HasSpawnIdentity => hasSpawnIdentity;

    private void OnValidate()
    {
        stats?.Sanitize();
        hitKnockbackForce = Mathf.Max(0f, hitKnockbackForce);
        hitKnockbackDuration = Mathf.Max(0f, hitKnockbackDuration);
        hitKnockbackDamageThreshold = Mathf.Max(0, hitKnockbackDamageThreshold);
        hitReactionMovementLockDuration = Mathf.Max(0f, hitReactionMovementLockDuration);

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (enemyTouchDamage == null)
            enemyTouchDamage = GetComponent<EnemyTouchDamage>();

        enemyAI?.RefreshConfiguredStats();
        enemyTouchDamage?.RefreshConfiguredStats();
    }

    private void Awake()
    {
        animationController = GetComponent<EnemyAnimationController>();
        characterController = GetComponent<CharacterController>();
        enemyAI = GetComponent<EnemyAI>();
        enemyTouchDamage = GetComponent<EnemyTouchDamage>();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        currentHP = Mathf.Max(1, stats != null ? stats.MaxHP : 1);
        isVisible = true;
        hasSpawnIdentity = true;
    }

    private void Start()
    {
        spawnDirector = ResolveSpawnDirector();
        spawnDirector?.Register(this);
    }

    private void OnDestroy()
    {
        spawnDirector?.Unregister(this);
    }

    public void TakeDamage(
        int amount,
        float direction,
        PlayerCharacter attacker = null,
        bool commitDeath = true,
        bool publishDamageFeedback = true,
        bool playHitReaction = true)
    {
        if (isDead || stats == null)
            return;

        lastAttacker = attacker;
        enemyAI?.NotifyProvoked(attacker);

        int finalDamage = Mathf.Max(1, amount - stats.Defense);
        currentHP -= finalDamage;

        // Non-finishing hits in multi-hit skills should never leave the enemy in a
        // "alive but zero/negative HP" state. Clamp them to 1 HP so the final hit
        // can resolve death cleanly while we still show the full rolled damage.
        if (!commitDeath && currentHP <= 0)
            currentHP = 1;

        if (currentHP > 0)
            NotifyHitReactionLock(hitReactionMovementLockDuration);

        if (publishDamageFeedback)
            PlayDamageFeedback(
                finalDamage,
                direction,
                playImpactFeedback: true,
                allowHitAnimation: false);

        if (commitDeath)
        {
            if (currentHP <= 0)
                Die();
            else if (playHitReaction)
                animationController?.PlayHit();
        }
        else if (playHitReaction)
        {
            animationController?.PlayHit();
        }
    }

    public void PlayAuthoritativeDamageFeedback(int finalDamage, float direction, bool playImpactFeedback = true)
    {
        PlayDamageFeedback(
            finalDamage,
            direction,
            playImpactFeedback,
            allowHitAnimation: false);
    }

    public void NotifyHitReactionLock(float duration)
    {
        if (duration <= 0f || isDead)
            return;

        enemyAI?.NotifyHitReactionLock(duration);
    }

    private void Die()
    {
        EnterDeadState(
            scheduleRespawn: true,
            dropLoot: true,
            publishDeathEvent: true,
            publishKillNotification: true,
            publishDeathVfx: true);
    }

    private void DisableLivingComponents()
    {
        if (enemyAI != null)
            enemyAI.enabled = false;

        if (enemyTouchDamage != null)
            enemyTouchDamage.enabled = false;

        if (characterController != null && characterController.enabled)
            characterController.enabled = false;
    }

    private IEnumerator RespawnRoutine()
    {
        float corpseTime = stats != null ? stats.CorpseDuration : 1.25f;
        yield return new WaitForSeconds(corpseTime);
        SetVisible(false);

        float respawnDelay = stats != null ? stats.RespawnDelay : 6f;
        FieldSpawnDirector director = ResolveSpawnDirector();

        if (director != null)
        {
            director.Register(this);
            yield return new WaitForSeconds(director.GetRespawnDelay(this, respawnDelay));

            while (director != null && !director.CanRespawn(this))
            {
                yield return new WaitForSeconds(director.QueueCheckInterval);
                director = ResolveSpawnDirector();
            }
        }
        else
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        Respawn();
    }

    private void Respawn()
    {
        RestoreLivingState(
            spawnPosition,
            spawnRotation,
            Mathf.Max(1, stats != null ? stats.MaxHP : 1));
        spawnDirector?.NotifyRespawnCompleted(this);
        MultiplayerPrototypeEnemyCoordinator.NotifyEnemyRespawned(this);
        respawnRoutine = null;
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        foreach (Renderer cachedRenderer in cachedRenderers)
        {
            if (cachedRenderer != null)
                cachedRenderer.enabled = visible;
        }
    }

    public bool ApplyAuthoritativeState(
        int authoritativeHp,
        bool authoritativeDead,
        bool authoritativeVisible,
        Vector3 authoritativePosition,
        Quaternion authoritativeRotation,
        int displayDamage,
        float direction,
        bool playDamageNumber,
        bool playImpactFeedback)
    {
        bool wasDead = isDead;
        Vector3 previousPosition = transform.position;

        if (authoritativeDead)
        {
            currentHP = Mathf.Max(0, authoritativeHp);

            if (!isDead)
            {
                ApplyTransformFromAuthority(authoritativePosition, authoritativeRotation);

                if (playDamageNumber && displayDamage > 0)
                {
                    PlayDamageFeedback(
                        displayDamage,
                        direction,
                        playImpactFeedback,
                        allowHitAnimation: false);
                }

                EnterDeadState(
                    scheduleRespawn: false,
                    dropLoot: false,
                    publishDeathEvent: false,
                    publishKillNotification: false,
                    publishDeathVfx: playDamageNumber || playImpactFeedback);
            }
            else
            {
                ApplyTransformFromAuthority(authoritativePosition, authoritativeRotation);

                if (playDamageNumber && displayDamage > 0)
                {
                    PlayDamageFeedback(
                        displayDamage,
                        direction,
                        playImpactFeedback: false,
                        allowHitAnimation: false);
                }
            }

            if (isVisible != authoritativeVisible)
                SetVisible(authoritativeVisible);

            animationController?.SetSpeed(0f);

            return !wasDead;
        }

        if (isDead)
        {
            RestoreLivingState(
                authoritativePosition,
                authoritativeRotation,
                Mathf.Max(1, authoritativeHp));

            if (!authoritativeVisible)
                SetVisible(false);

            return false;
        }

        ApplyTransformFromAuthority(authoritativePosition, authoritativeRotation);
        currentHP = Mathf.Max(0, authoritativeHp);

        if (isVisible != authoritativeVisible)
            SetVisible(authoritativeVisible);

        if (playDamageNumber && displayDamage > 0)
        {
            PlayDamageFeedback(
                displayDamage,
                direction,
                playImpactFeedback,
                allowHitAnimation: playImpactFeedback);
        }

        if (MultiplayerPrototypeRuntime.IsEnabled
            && (enemyAI == null || !enemyAI.enabled)
            && animationController != null)
        {
            Vector3 horizontalDelta = authoritativePosition - previousPosition;
            horizontalDelta.y = 0f;
            animationController.SetSpeed(horizontalDelta.sqrMagnitude > 0.0001f ? 1f : 0f);
        }

        return false;
    }

    private void PlayDamageFeedback(
        int finalDamage,
        float direction,
        bool playImpactFeedback,
        bool allowHitAnimation)
    {
        if (finalDamage <= 0)
            return;

        bool shouldApplyKnockback =
            playImpactFeedback
            && currentHP > 0
            && enableHitKnockback
            && hitKnockbackForce > 0f
            && hitKnockbackDuration > 0f
            && finalDamage >= hitKnockbackDamageThreshold;

        if (shouldApplyKnockback)
        {
            EventBus.Publish(new CharacterKnockbackEvent
            {
                Target = transform,
                DirectionX = direction,
                Force = hitKnockbackForce,
                Duration = hitKnockbackDuration
            });
        }

        EventBus.Publish(new DamageNumberEvent
        {
            WorldPosition = transform.position + Vector3.up * 1.2f,
            Target = transform,
            Kind = CombatFloatingTextKind.Damage,
            Damage = finalDamage
        });

        if (playImpactFeedback)
        {
            EventBus.Publish(new PlaySfxEvent
            {
                Type = SfxType.SwordHit,
                Position = transform.position
            });

            EventBus.Publish(new PlayVfxEvent
            {
                Type = VfxType.EnemyHit,
                Position = transform.position + Vector3.up * 1f,
                Rotation = Quaternion.identity
            });
        }

        if (playImpactFeedback && allowHitAnimation && currentHP > 0)
            animationController?.PlayHit();
    }

    private void EnterDeadState(
        bool scheduleRespawn,
        bool dropLoot,
        bool publishDeathEvent,
        bool publishKillNotification,
        bool publishDeathVfx)
    {
        if (isDead)
            return;

        isDead = true;
        currentHP = Mathf.Max(0, currentHP);

        if (dropLoot)
            TryDropLoot();

        if (publishDeathEvent && stats != null)
        {
            EventBus.Publish(new EnemyDiedEvent
            {
                Enemy = transform,
                Killer = lastAttacker,
                Type = stats.EnemyType,
                ExpReward = stats.ExpReward
            });
        }

        if (publishKillNotification)
            PublishKillNotification();

        if (publishDeathVfx)
        {
            EventBus.Publish(new PlayVfxEvent
            {
                Type = VfxType.EnemyDeath,
                Position = transform.position + Vector3.up * 0.9f,
                Rotation = Quaternion.identity
            });
        }

        if (!isVisible)
            SetVisible(true);

        animationController?.PlayDie();
        DisableLivingComponents();

        if (respawnRoutine != null)
            StopCoroutine(respawnRoutine);

        respawnRoutine = scheduleRespawn ? StartCoroutine(RespawnRoutine()) : null;
    }

    private void RestoreLivingState(Vector3 position, Quaternion rotation, int hp)
    {
        if (respawnRoutine != null)
            StopCoroutine(respawnRoutine);

        ApplyTransformFromAuthority(position, rotation);
        currentHP = Mathf.Max(1, hp);
        lastAttacker = null;
        SetVisible(true);

        if (characterController != null)
            characterController.enabled = true;

        if (enemyAI != null)
        {
            enemyAI.enabled = true;
            enemyAI.ResetAfterRespawn();
        }

        if (enemyTouchDamage != null)
        {
            enemyTouchDamage.enabled = true;
            enemyTouchDamage.ResetAfterRespawn();
        }

        animationController?.ResetToIdle();
        isDead = false;
        respawnRoutine = null;
    }

    private void ApplyTransformFromAuthority(Vector3 position, Quaternion rotation)
    {
        bool restoreCharacterController = characterController != null && characterController.enabled;
        if (restoreCharacterController)
            characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (restoreCharacterController)
            characterController.enabled = true;
    }

    public void SetPrototypeAuthorityMode(bool isAuthoritativeSimulation)
    {
        if (!MultiplayerPrototypeRuntime.IsEnabled)
            return;

        if (enemyAI != null)
            enemyAI.enabled = isAuthoritativeSimulation && !isDead;

        if (enemyTouchDamage != null)
            enemyTouchDamage.enabled = false;

        if (!isAuthoritativeSimulation && animationController != null && !isDead)
            animationController.SetSpeed(0f);
    }

    private void TryDropLoot()
    {
        if (MultiplayerPrototypeRuntime.IsEnabled)
            return;

        Vector3 dropOrigin = transform.position + Vector3.up * 0.8f;
        TryDropMesos(dropOrigin);
        TryDropRedPotion(dropOrigin);
        TryDropBluePotion(dropOrigin);
        TryDropCommonEtc(dropOrigin);
    }

    private void TryDropMesos(Vector3 dropOrigin)
    {
        if (stats == null || Random.value > stats.MesoDropChance)
            return;

        int amount = Random.Range(stats.MinMesoDrop, stats.MaxMesoDrop + 1);
        if (amount <= 0)
            return;

        WorldLootPickup.SpawnMesos(dropOrigin + GetDropScatter(), amount);
    }

    private void TryDropRedPotion(Vector3 dropOrigin)
    {
        if (stats == null || Random.value > stats.RedPotionDropChance)
            return;

        int amount = stats.RedPotionDropCount;
        if (amount <= 0)
            return;

        WorldLootPickup.SpawnItem(dropOrigin + GetDropScatter(), ItemDatabase.RedPotionId, amount);
    }

    private void TryDropBluePotion(Vector3 dropOrigin)
    {
        if (stats == null || Random.value > stats.BluePotionDropChance)
            return;

        int amount = stats.BluePotionDropCount;
        if (amount <= 0)
            return;

        WorldLootPickup.SpawnItem(dropOrigin + GetDropScatter(), ItemDatabase.BluePotionId, amount);
    }

    private void TryDropCommonEtc(Vector3 dropOrigin)
    {
        if (stats == null || Random.value > stats.CommonEtcDropChance)
            return;

        if (string.IsNullOrWhiteSpace(stats.CommonEtcItemId))
            return;

        int amount = stats.CommonEtcDropCount;
        if (amount <= 0)
            return;

        WorldLootPickup.SpawnItem(dropOrigin + GetDropScatter(), stats.CommonEtcItemId, amount);
    }

    private Vector3 GetDropScatter()
    {
        return new Vector3(
            Random.Range(-0.35f, 0.35f),
            Random.Range(-0.05f, 0.12f),
            Random.Range(-0.08f, 0.08f));
    }

    private void PublishKillNotification()
    {
        if (lastAttacker == null || !lastAttacker.IsLocalPlayer || stats == null)
            return;

        EventBus.Publish(new GameplayNotificationEvent
        {
            Target = lastAttacker,
            CharacterId = lastAttacker.CharacterId,
            Category = GameplayNotificationCategory.Kill,
            Message = $"Killed {GetEnemyDisplayName(stats.EnemyType)} +{stats.ExpReward} EXP"
        });
    }

    private static string GetEnemyDisplayName(EnemyType enemyType)
    {
        string rawName = enemyType.ToString();
        if (string.IsNullOrEmpty(rawName))
            return "Monster";

        StringBuilder builder = new StringBuilder(rawName.Length + 4);

        for (int index = 0; index < rawName.Length; index++)
        {
            char character = rawName[index];

            if (index > 0 && char.IsUpper(character) && !char.IsWhiteSpace(rawName[index - 1]))
                builder.Append(' ');

            builder.Append(character);
        }

        return builder.ToString();
    }

    private FieldSpawnDirector ResolveSpawnDirector()
    {
        if (spawnDirector != null && spawnDirector.gameObject.scene == gameObject.scene)
            return spawnDirector;

        FieldSpawnDirector[] directors = FindObjectsByType<FieldSpawnDirector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (FieldSpawnDirector director in directors)
        {
            if (director != null && director.gameObject.scene == gameObject.scene)
            {
                spawnDirector = director;
                return spawnDirector;
            }
        }

        spawnDirector = null;
        return null;
    }
}
