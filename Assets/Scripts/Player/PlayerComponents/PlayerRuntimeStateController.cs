using UnityEngine;

public class PlayerRuntimeStateController : MonoBehaviour
{
    private PlayerCharacter character;
    private PlayerBaseStats baseStats;
    private GameBootstrap bootstrap;
    private PlayerHitFlashController playerHitFlash;
    private PlayerRuntimeData runtimeStats;

    private int baseWeaponPower;
    private float skillMastery;
    private float timeToStun;
    private float timeToInv;
    private float timeToFlash;

    private bool isStunned;
    private float stunTimer;
    private bool isInvulnerable;
    private float invulTimer;
    private bool isDead;

    public PlayerRuntimeData RuntimeData => runtimeStats;
    public bool IsStunned => isStunned;
    public bool IsDead => isDead;
    public PlayerJobType CurrentJob => runtimeStats != null ? runtimeStats.CurrentJob : PlayerJobType.Drifter;
    public bool HasPendingJobAdvancement => runtimeStats != null && runtimeStats.HasPendingJobAdvancement;
    public int UnspentStatPoints => runtimeStats != null ? runtimeStats.UnspentStatPoints : 0;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        PlayerBaseStats playerBaseStats,
        int baseWeaponPower,
        float baseSkillMastery,
        float stunDuration,
        float invulnerabilityDuration,
        float flashDuration)
    {
        character = ownerCharacter;
        baseStats = playerBaseStats;
        this.baseWeaponPower = baseWeaponPower;
        skillMastery = baseSkillMastery;
        timeToStun = stunDuration;
        timeToInv = invulnerabilityDuration;
        timeToFlash = flashDuration;
        playerHitFlash = GetComponent<PlayerHitFlashController>();
        RefreshRuntimeOwnershipState();
    }

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
        RefreshRuntimeOwnershipState();
    }

    public void RefreshRuntimeOwnershipState()
    {
        if (character == null || !character.IsLocalPlayer)
        {
            runtimeStats = null;
            return;
        }

        runtimeStats = bootstrap != null ? bootstrap.CharacterSession?.PlayerData : null;
        SyncBaseStatsToRuntime();
    }

    public void RefreshSessionBindings()
    {
        if (character == null || !character.IsLocalPlayer)
            return;

        if (bootstrap != null)
            runtimeStats = bootstrap.CharacterSession?.PlayerData;
    }

    public PlayerCombatSnapshot GetCombatSnapshot()
    {
        RefreshSessionBindings();
        PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(CurrentJob);

        return CombatSnapshotBuilder.Build(
            runtimeStats,
            baseStats,
            GetEquipmentBonuses(),
            baseWeaponPower,
            skillMastery,
            jobDefinition,
            currentWeaponType: default,
            momentumStacks: character != null ? character.CurrentMomentumStacks : 0);
    }

    public PlayerBasicAttackProfile GetBasicAttackProfile()
    {
        RefreshSessionBindings();
        return PlayerJobCombatProfiles.GetBasicAttackProfile(CurrentJob);
    }

    public bool TrySpendStatPoints(PlayerProgressionStatType statType, int points)
    {
        RefreshSessionBindings();

        if (runtimeStats == null || points <= 0)
            return false;

        if (runtimeStats.UnspentStatPoints < points)
            return false;

        switch (statType)
        {
            case PlayerProgressionStatType.Might:
                runtimeStats.Might += points;
                break;
            case PlayerProgressionStatType.Precision:
                runtimeStats.Precision += points;
                break;
            case PlayerProgressionStatType.Arcane:
                runtimeStats.Arcane += points;
                break;
            case PlayerProgressionStatType.Finesse:
                runtimeStats.Finesse += points;
                break;
            case PlayerProgressionStatType.HitRate:
                runtimeStats.HitRate += points;
                break;
            default:
                return false;
        }

        runtimeStats.UnspentStatPoints -= points;
        SaveSession();
        EventBus.Publish(new PlayerExpChangedEvent
        {
            Target = character,
            CharacterId = character != null ? character.CharacterId : PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, null),
            CurrentExp = runtimeStats.CurrentExp,
            RequiredExp = runtimeStats.RequiredExp,
            UnspentStatPoints = runtimeStats.UnspentStatPoints,
            UnspentSkillPoints = runtimeStats.UnspentSkillPoints
        });
        return true;
    }

    public void TakeDamage(int amount)
    {
        RefreshSessionBindings();

        if (runtimeStats == null)
            return;

        runtimeStats.CurrentHP = Mathf.Max(0, runtimeStats.CurrentHP - amount);
        PublishHealthChanged();
        SaveSession();

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
        SaveSession();
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
        SaveSession();
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
        SaveSession();
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
        stunTimer = 0f;
        invulTimer = 0f;
        character?.ResetCombatMomentum();
        character?.ActionStateController?.ResetState();

        SaveSession();
        EventBus.Publish(new PlayerRespawnedEvent
        {
            Target = character,
            CharacterId = character != null ? character.CharacterId : string.Empty
        });
    }

    public int GetEffectiveMaxHP()
    {
        if (runtimeStats == null)
            return 0;

        ItemStatModifierData equipmentBonuses = GetEquipmentBonuses();
        return Mathf.Max(1, runtimeStats.MaxHP + equipmentBonuses.MaxHP);
    }

    public int GetEffectiveMaxMP()
    {
        if (runtimeStats == null)
            return 0;

        ItemStatModifierData equipmentBonuses = GetEquipmentBonuses();
        return Mathf.Max(0, runtimeStats.MaxMP + equipmentBonuses.MaxMP);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
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

    private void SyncBaseStatsToRuntime()
    {
        if (runtimeStats == null || baseStats == null)
            return;

        bool didChange = false;

        if (runtimeStats.Might < baseStats.Might)
        {
            runtimeStats.Might = baseStats.Might;
            didChange = true;
        }

        if (runtimeStats.Precision < baseStats.Precision)
        {
            runtimeStats.Precision = baseStats.Precision;
            didChange = true;
        }

        if (runtimeStats.Arcane < baseStats.Arcane)
        {
            runtimeStats.Arcane = baseStats.Arcane;
            didChange = true;
        }

        if (runtimeStats.Finesse < baseStats.Finesse)
        {
            runtimeStats.Finesse = baseStats.Finesse;
            didChange = true;
        }

        if (runtimeStats.HitRate < baseStats.HitRate)
        {
            runtimeStats.HitRate = baseStats.HitRate;
            didChange = true;
        }

        if (didChange)
            SaveSession();
    }

    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        RefreshSessionBindings();

        if (character == null
            || !PlayerRuntimeIdentityUtility.MatchesCharacter(character, character.CharacterId, e.Target, e.CharacterId)
            || runtimeStats == null)
        {
            return;
        }

        if (isInvulnerable || isDead)
            return;

        TakeDamage(e.Damage);

        EventBus.Publish(new PlayerHitEvent
        {
            Target = character,
            CharacterId = character.CharacterId
        });

        float direction = Mathf.Sign(character.transform.position.x - e.HitDirection);

        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.PlayerHit,
            Position = character.transform.position
        });

        EventBus.Publish(new PlayVfxEvent
        {
            Type = VfxType.PlayerHit,
            Position = character.transform.position + Vector3.up * 1f,
            Rotation = Quaternion.identity
        });

        EventBus.Publish(new CharacterKnockbackEvent
        {
            Target = character.transform,
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
        character?.ResetCombatMomentum();
        character?.ActionStateController?.ResetState();

        EventBus.Publish(new PlayerDiedEvent
        {
            Target = character,
            CharacterId = character != null ? character.CharacterId : string.Empty
        });
    }

    private void PublishHealthChanged()
    {
        if (character == null || runtimeStats == null)
            return;

        EventBus.Publish(new PlayerHealthChangedEvent
        {
            Target = character,
            CharacterId = character.CharacterId,
            CurrentHP = runtimeStats.CurrentHP,
            MaxHP = GetEffectiveMaxHP()
        });
    }

    private void PublishManaChanged()
    {
        if (character == null || runtimeStats == null)
            return;

        EventBus.Publish(new PlayerManaChangedEvent
        {
            Target = character,
            CharacterId = character.CharacterId,
            CurrentMP = runtimeStats.CurrentMP,
            MaxMP = GetEffectiveMaxMP()
        });
    }

    private void SaveSession()
    {
        bootstrap?.CharacterSession?.Save();
    }

    private ItemStatModifierData GetEquipmentBonuses()
    {
        return bootstrap != null && bootstrap.EquipmentSession != null
            ? bootstrap.EquipmentSession.GetEquipmentStatBonuses()
            : new ItemStatModifierData();
    }
}

