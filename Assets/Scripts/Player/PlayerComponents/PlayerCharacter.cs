using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    [Header("Player Context")]
    [SerializeField] private bool isLocalPlayer = true;
    [SerializeField] private string characterId;
    [SerializeField] private GameBootstrap bootstrap;

    [Header("Base Stats")]
    [SerializeField] private PlayerBaseStats baseStats;

    [Header("Combat")]
    [SerializeField] private int weaponAttack = 15;
    [SerializeField] private float skillMastery = 0.6f;
    [SerializeField] private float timeToStun;
    [SerializeField] private float timeToInv;
    [SerializeField] private float timeToFlash;

    [Header("Consumables")]
    [SerializeField] private float manualPickupRange = 1.6f;

    private bool isStunned;
    private float stunTimer;
    private bool isInvulnerable;
    private float invulTimer;
    private PlayerRuntimeData runtimeStats;
    private PlayerHitFlashController playerHitFlash;
    private bool isDead;
    private bool? runtimeLocalPlayerOverride;
    private string runtimeCharacterId;

    public bool IsLocalPlayer => runtimeLocalPlayerOverride ?? isLocalPlayer;
    public string CharacterId => ResolveCharacterId();
    public bool IsStunned => isStunned;
    public bool IsDead => isDead;
    public PlayerJobType CurrentJob => runtimeStats != null ? runtimeStats.CurrentJob : PlayerJobType.Novice;
    public bool HasPendingJobAdvancement => runtimeStats != null && runtimeStats.HasPendingJobAdvancement;

    public void SetRuntimeLocalPlayer(bool isRuntimeLocalPlayer)
    {
        runtimeLocalPlayerOverride = isRuntimeLocalPlayer;
        RefreshRuntimeOwnershipState();
    }

    public void SetRuntimeCharacterId(string resolvedCharacterId)
    {
        runtimeCharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(resolvedCharacterId);
    }

    public void RefreshPrototypeSessionState()
    {
        RefreshSessionBindings();

        if (IsLocalPlayer)
            bootstrap?.PublishSessionState(this);
    }

    private void Awake()
    {
        ResolveBootstrap();

        if (IsLocalPlayer && bootstrap == null)
        {
            Debug.LogError("PlayerCharacter requires an active GameBootstrap for the local player.");
            enabled = false;
            return;
        }

        runtimeStats = IsLocalPlayer && bootstrap != null ? bootstrap.PlayerData : null;
        SyncBaseStatsToRuntime();
        playerHitFlash = GetComponent<PlayerHitFlashController>();
    }

    private void Start()
    {
        if (!IsLocalPlayer)
            return;

        RefreshSessionBindings();
        bootstrap?.PublishSessionState(this);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Subscribe<InteractPressedEvent>(OnInteractPressed);
        EventBus.Subscribe<UseConsumablePressedEvent>(OnUseConsumablePressed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Unsubscribe<InteractPressedEvent>(OnInteractPressed);
        EventBus.Unsubscribe<UseConsumablePressedEvent>(OnUseConsumablePressed);
    }

    private void Update()
    {
        if (isInvulnerable)
        {
            invulTimer -= Time.deltaTime;

            if (invulTimer <= 0f)
                isInvulnerable = false;
        }

        if (!isStunned)
            return;

        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0f)
            isStunned = false;
    }

    public PlayerCombatSnapshot GetCombatSnapshot()
    {
        RefreshSessionBindings();
        ItemStatModifierData equipmentBonuses = GetEquipmentBonuses();

        int strength = runtimeStats != null
            ? runtimeStats.Strength
            : baseStats != null ? baseStats.Strength : 0;

        int dexterity = runtimeStats != null
            ? runtimeStats.Dexterity
            : baseStats != null ? baseStats.Dexterity : 0;

        return new PlayerCombatSnapshot
        {
            Strength = strength + equipmentBonuses.Strength,
            Dexterity = dexterity + equipmentBonuses.Dexterity,
            WeaponAttack = weaponAttack + equipmentBonuses.WeaponAttack,
            SkillMastery = skillMastery
        };
    }

    public PlayerBasicAttackProfile GetBasicAttackProfile()
    {
        RefreshSessionBindings();
        return PlayerJobCombatProfiles.GetBasicAttackProfile(CurrentJob);
    }

    public void TakeDamage(int amount)
    {
        RefreshSessionBindings();

        if (runtimeStats == null)
            return;

        runtimeStats.CurrentHP = Mathf.Max(0, runtimeStats.CurrentHP - amount);
        PublishHealthChanged();
        bootstrap?.SavePlayer();

        if (runtimeStats.CurrentHP == 0)
            Die();
    }

    public void RestoreHP(int amount)
    {
        RefreshSessionBindings();

        if (runtimeStats == null || amount <= 0)
            return;

        int updatedHP = Mathf.Clamp(runtimeStats.CurrentHP + amount, 0, GetEffectiveMaxHP());
        if (updatedHP == runtimeStats.CurrentHP)
            return;

        runtimeStats.CurrentHP = updatedHP;
        PublishHealthChanged();
        bootstrap?.SavePlayer();
    }

    public void RestoreMP(int amount)
    {
        RefreshSessionBindings();

        if (runtimeStats == null || amount <= 0)
            return;

        int updatedMP = Mathf.Clamp(runtimeStats.CurrentMP + amount, 0, GetEffectiveMaxMP());
        if (updatedMP == runtimeStats.CurrentMP)
            return;

        runtimeStats.CurrentMP = updatedMP;
        PublishManaChanged();
        bootstrap?.SavePlayer();
    }

    public bool HasEnoughMP(int amount)
    {
        RefreshSessionBindings();

        if (runtimeStats == null)
            return false;

        return runtimeStats.CurrentMP >= Mathf.Max(0, amount);
    }

    public bool TrySpendMP(int amount)
    {
        RefreshSessionBindings();

        if (runtimeStats == null)
            return false;

        int manaCost = Mathf.Max(0, amount);
        if (manaCost == 0)
            return true;

        if (runtimeStats.CurrentMP < manaCost)
            return false;

        runtimeStats.CurrentMP -= manaCost;
        PublishManaChanged();
        bootstrap?.SavePlayer();
        return true;
    }

    public void ResetAfterDeath()
    {
        RefreshSessionBindings();

        if (runtimeStats == null)
            return;

        runtimeStats.CurrentHP = GetEffectiveMaxHP();
        PublishHealthChanged();
        isDead = false;
        isInvulnerable = false;
        isStunned = false;

        bootstrap?.SavePlayer();
        EventBus.Publish(new PlayerRespawnedEvent
        {
            Target = this,
            CharacterId = CharacterId
        });
    }

    private void SyncBaseStatsToRuntime()
    {
        if (runtimeStats == null || baseStats == null)
            return;

        bool didChange = false;

        if (runtimeStats.Strength < baseStats.Strength)
        {
            runtimeStats.Strength = baseStats.Strength;
            didChange = true;
        }

        if (runtimeStats.Dexterity < baseStats.Dexterity)
        {
            runtimeStats.Dexterity = baseStats.Dexterity;
            didChange = true;
        }

        if (didChange)
            bootstrap?.SavePlayer();
    }

    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        RefreshSessionBindings();

        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(this, CharacterId, e.Target, e.CharacterId) || runtimeStats == null)
            return;

        if (isInvulnerable || isDead)
            return;

        TakeDamage(e.Damage);

        EventBus.Publish(new PlayerHitEvent
        {
            Target = this,
            CharacterId = CharacterId
        });

        float direction = Mathf.Sign(transform.position.x - e.HitDirection);

        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.PlayerHit,
            Position = transform.position
        });

        EventBus.Publish(new PlayVfxEvent
        {
            Type = VfxType.PlayerHit,
            Position = transform.position + Vector3.up * 1f,
            Rotation = Quaternion.identity
        });

        EventBus.Publish(new CharacterKnockbackEvent
        {
            Target = transform,
            DirectionX = direction,
            Force = 1.2f,
            Duration = 0.15f
        });

        isStunned = true;
        stunTimer = timeToStun;
        isInvulnerable = true;
        invulTimer = timeToInv;
        playerHitFlash?.PlayFlash(timeToFlash);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        EventBus.Publish(new PlayerDiedEvent
        {
            Target = this,
            CharacterId = CharacterId
        });
    }

    private void OnInteractPressed(InteractPressedEvent e)
    {
        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(this, CharacterId, e.Player, e.CharacterId) || isDead)
            return;

        WorldLootPickup nearestPickup = FindNearestManualPickup();
        nearestPickup?.TryCollect(this);
    }

    private void OnUseConsumablePressed(UseConsumablePressedEvent e)
    {
        RefreshSessionBindings();

        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(this, CharacterId, e.Player, e.CharacterId)
            || isDead
            || runtimeStats == null
            || bootstrap == null)
            return;

        string itemId = ItemDatabase.GetConsumableItemId(e.ConsumableType);
        if (string.IsNullOrEmpty(itemId))
            return;

        ItemDefinition definition = ItemDatabase.GetDefinition(itemId);
        if (definition == null)
            return;

        int restoreHPAmount = definition.RestoreHP;
        int restoreMPAmount = definition.RestoreMP;

        bool canRestoreHP = restoreHPAmount > 0 && runtimeStats.CurrentHP < GetEffectiveMaxHP();
        bool canRestoreMP = restoreMPAmount > 0 && runtimeStats.CurrentMP < GetEffectiveMaxMP();

        if (!canRestoreHP && !canRestoreMP)
            return;

        if (!bootstrap.TryConsumeConsumable(this, e.ConsumableType))
            return;

        if (canRestoreHP)
            RestoreHP(restoreHPAmount);

        if (canRestoreMP)
            RestoreMP(restoreMPAmount);
    }

    private void PublishHealthChanged()
    {
        EventBus.Publish(new PlayerHealthChangedEvent
        {
            Target = this,
            CharacterId = CharacterId,
            CurrentHP = runtimeStats.CurrentHP,
            MaxHP = GetEffectiveMaxHP()
        });
    }

    private void PublishManaChanged()
    {
        EventBus.Publish(new PlayerManaChangedEvent
        {
            Target = this,
            CharacterId = CharacterId,
            CurrentMP = runtimeStats.CurrentMP,
            MaxMP = GetEffectiveMaxMP()
        });
    }

    private WorldLootPickup FindNearestManualPickup()
    {
        WorldLootPickup[] worldPickups = FindObjectsByType<WorldLootPickup>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        WorldLootPickup nearestPickup = null;
        float bestSqrDistance = manualPickupRange * manualPickupRange;

        foreach (WorldLootPickup pickup in worldPickups)
        {
            if (pickup == null || !pickup.CanBePickedUp(this, manualPickupRange))
                continue;

            float sqrDistance = (pickup.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            nearestPickup = pickup;
        }

        return nearestPickup;
    }

    private void ResolveBootstrap()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
    }

    private void RefreshRuntimeOwnershipState()
    {
        if (!IsLocalPlayer)
        {
            runtimeStats = null;
            return;
        }

        ResolveBootstrap();
        runtimeStats = bootstrap != null ? bootstrap.PlayerData : null;
        SyncBaseStatsToRuntime();
    }

    private void RefreshSessionBindings()
    {
        if (!IsLocalPlayer)
            return;

        ResolveBootstrap();

        if (bootstrap != null)
            runtimeStats = bootstrap.PlayerData;
    }

    private string ResolveCharacterId()
    {
        if (IsLocalPlayer)
        {
            ResolveBootstrap();
            if (bootstrap != null && bootstrap.ActiveCharacter != null)
            {
                characterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(bootstrap.ActiveCharacter.CharacterId);
                runtimeCharacterId = characterId;
                return characterId;
            }
        }

        string normalizedRuntimeCharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(runtimeCharacterId);
        if (!string.IsNullOrWhiteSpace(normalizedRuntimeCharacterId))
        {
            characterId = normalizedRuntimeCharacterId;
            return characterId;
        }

        return PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
    }

    private ItemStatModifierData GetEquipmentBonuses()
    {
        return bootstrap != null ? bootstrap.GetEquipmentStatBonuses() : new ItemStatModifierData();
    }

    private int GetEffectiveMaxHP()
    {
        if (runtimeStats == null)
            return 0;

        ItemStatModifierData equipmentBonuses = GetEquipmentBonuses();
        return Mathf.Max(1, runtimeStats.MaxHP + equipmentBonuses.MaxHP);
    }

    private int GetEffectiveMaxMP()
    {
        if (runtimeStats == null)
            return 0;

        ItemStatModifierData equipmentBonuses = GetEquipmentBonuses();
        return Mathf.Max(0, runtimeStats.MaxMP + equipmentBonuses.MaxMP);
    }
}
