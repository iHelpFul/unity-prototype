using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    [Header("Character Defaults")]
    [SerializeField] private string defaultCharacterStartMapId = "Map_01";
    [SerializeField] private string defaultCharacterStartSpawnId = SceneSpawnPoint.DefaultSpawnId;

    private readonly PlayerSessionPersistenceService persistenceService = new PlayerSessionPersistenceService();
    private readonly PlayerSessionInventoryService inventoryService = new PlayerSessionInventoryService();
    private readonly PlayerSessionEquipmentService equipmentService = new PlayerSessionEquipmentService();
    private readonly PlayerSessionSkillService skillService = new PlayerSessionSkillService();
    private readonly PlayerSessionMapStateService mapStateService = new PlayerSessionMapStateService();
    private PlayerSessionEventPublisher eventPublisher;

    public AccountProfileData AccountData { get; private set; }
    public CharacterSaveData ActiveCharacter { get; private set; }
    public PlayerRuntimeData PlayerData { get; private set; }
    public bool HasSessionData => AccountData != null && ActiveCharacter != null && PlayerData != null;

    private void Awake()
    {
        if (ShouldDestroyDuplicate())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        eventPublisher ??= new PlayerSessionEventPublisher(inventoryService, equipmentService);

        LoadOrCreateSession();
        SavePlayer();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SavePlayer();
    }

    private void OnApplicationQuit()
    {
        SavePlayer();
    }

    public bool AddInventoryItem(PlayerCharacter player, string itemId, int amount)
    {
        if (!CanMutateSession(player, amount))
            return false;

        if (!inventoryService.AddItem(itemId, amount))
            return false;

        PersistInventoryChange(player, itemId);
        return true;
    }

    public bool TryRemoveInventoryItem(PlayerCharacter player, string itemId, int amount)
    {
        if (!CanMutateSession(player, amount))
            return false;

        if (!inventoryService.RemoveItem(itemId, amount, equipmentService.EquippedItems))
            return false;

        PersistInventoryChange(player, itemId);
        return true;
    }

    public int GetInventoryCount(string itemId)
    {
        return inventoryService.GetCount(itemId);
    }

    public bool HasInventoryItem(string itemId, int amount = 1)
    {
        return inventoryService.HasItem(itemId, amount);
    }

    public IReadOnlyList<InventoryEntry> GetInventoryEntries()
    {
        return inventoryService.Entries;
    }

    public IReadOnlyList<EquippedItemEntry> GetEquippedItems()
    {
        return equipmentService.EquippedItems;
    }

    public InventoryEntry GetInventoryEntry(string entryId)
    {
        return inventoryService.GetEntryById(entryId);
    }

    public InventoryEntry GetEquippedEntry(EquipmentSlotType slot)
    {
        return equipmentService.GetEquippedEntry(slot);
    }

    public string AddEquipmentItem(
        PlayerCharacter player,
        string itemId,
        ItemEquipmentAppearanceData appearanceOverride = null,
        bool autoEquip = false)
    {
        if (!CanMutateSession(player))
            return string.Empty;

        string entryId = equipmentService.AddEquipmentItem(itemId, appearanceOverride, autoEquip);
        if (string.IsNullOrWhiteSpace(entryId))
            return string.Empty;

        eventPublisher.PublishInventoryChanged(player, itemId);

        if (autoEquip)
        {
            EquipmentSlotType slot = ItemDatabase.TryGetDefinition(itemId, out ItemDefinition definition)
                ? definition.EquipmentSlot
                : EquipmentSlotType.None;
            RefreshEquippedState(player, slot, entryId, itemId, true);
        }

        SavePlayer();
        return entryId;
    }

    public bool TryEquipInventoryEntry(PlayerCharacter player, string inventoryEntryId)
    {
        if (!CanMutateSession(player))
            return false;

        InventoryEntry inventoryEntry = inventoryService.GetEntryById(inventoryEntryId);
        if (inventoryEntry == null)
            return false;

        if (!equipmentService.TryEquipEntry(inventoryEntryId, out EquipmentSlotType slot, out string replacedInventoryEntryId))
            return false;

        RefreshEquippedState(player, slot, inventoryEntryId, inventoryEntry.ItemId, true);

        if (!string.IsNullOrWhiteSpace(replacedInventoryEntryId))
        {
            InventoryEntry replacedEntry = inventoryService.GetEntryById(replacedInventoryEntryId);
            PublishEquipmentChangedForScenePlayer(
                player,
                slot,
                replacedInventoryEntryId,
                replacedEntry != null ? replacedEntry.ItemId : string.Empty,
                false);
        }

        SavePlayer();
        return true;
    }

    public bool TryUnequipItem(PlayerCharacter player, EquipmentSlotType slot)
    {
        if (!CanMutateSession(player) || slot == EquipmentSlotType.None)
            return false;

        InventoryEntry equippedEntry = equipmentService.GetEquippedEntry(slot);
        if (equippedEntry == null || !equipmentService.TryUnequip(slot, out string inventoryEntryId))
            return false;

        RefreshEquippedState(player, slot, inventoryEntryId, equippedEntry.ItemId, false);
        SavePlayer();
        return true;
    }

    public ItemStatModifierData GetEquipmentStatBonuses()
    {
        return equipmentService.GetTotalStatBonuses();
    }

    public CharacterAppearanceData GetResolvedActiveCharacterAppearance()
    {
        if (ActiveCharacter == null)
            return null;

        return equipmentService.BuildResolvedAppearance(ActiveCharacter.Appearance);
    }

    public IReadOnlyList<PlayerSkillEntry> GetUnlockedSkills()
    {
        return skillService.GetUnlockedSkills();
    }

    public PlayerSkillEntry GetAssignedSkill(int slotIndex)
    {
        return skillService.GetAssignedSkill(slotIndex);
    }

    public PlayerSkillDefinition GetAssignedSkillDefinition(int slotIndex)
    {
        return skillService.GetAssignedSkillDefinition(slotIndex);
    }

    public bool TryAssignSkillToSlot(PlayerCharacter player, string skillId, int slotIndex)
    {
        if (!CanMutateSession(player))
            return false;

        return PersistSuccessfulMutation(skillService.TryAssignSkillToSlot(skillId, slotIndex));
    }

    public bool TrySetPlayerJob(PlayerCharacter player, PlayerJobType newJobType)
    {
        if (!CanMutateSession(player))
            return false;

        if (PlayerData.CurrentJob == newJobType)
            return false;

        PlayerData.CurrentJob = newJobType;
        PlayerData.HasPendingJobAdvancement = PlayerJobCombatProfiles.IsJobAdvancementAvailable(PlayerData);
        skillService.EnsureDefaultSkillsForJob(PlayerData.CurrentJob);
        PublishJobState(player);
        SavePlayer();
        return true;
    }

    public bool CanAdvanceToJob(PlayerCharacter player, PlayerJobType targetJob, int requiredLevel, out string message)
    {
        message = string.Empty;

        if (PlayerData == null || !IsSupportedSessionPlayer(player))
        {
            message = "Player session is not ready.";
            return false;
        }

        if (targetJob == PlayerJobType.Novice)
        {
            message = "Please choose a valid job.";
            return false;
        }

        if (PlayerData.CurrentJob != PlayerJobType.Novice)
        {
            message = $"You are already a {FormatJobName(PlayerData.CurrentJob)}.";
            return false;
        }

        if (PlayerData.Level < Mathf.Max(1, requiredLevel))
        {
            message = $"Reach level {Mathf.Max(1, requiredLevel)} as a Novice first.";
            return false;
        }

        message = $"Choose your path: {FormatJobName(targetJob)}.";
        return true;
    }

    public bool TryAdvanceToJob(PlayerCharacter player, PlayerJobType targetJob, int requiredLevel, out string message)
    {
        if (!CanAdvanceToJob(player, targetJob, requiredLevel, out message))
            return false;

        if (!TrySetPlayerJob(player, targetJob))
        {
            message = "The job advancement could not be completed.";
            return false;
        }

        message = $"You are now a {FormatJobName(targetJob)}.";
        return true;
    }

    public bool TryUnlockSkill(PlayerCharacter player, string skillId, int skillLevel = 1)
    {
        if (!CanMutateSession(player))
            return false;

        return PersistSuccessfulMutation(skillService.TryUnlockSkill(skillId, skillLevel));
    }

    public bool IsSkillOnCooldown(string skillId)
    {
        return skillService.IsSkillOnCooldown(skillId);
    }

    public float GetRemainingSkillCooldown(string skillId)
    {
        return skillService.GetRemainingSkillCooldown(skillId);
    }

    public void StartSkillCooldown(string skillId, float cooldown)
    {
        skillService.StartSkillCooldown(skillId, cooldown);
    }

    public void AddMesos(PlayerCharacter player, int amount)
    {
        if (!CanMutateSession(player, amount))
            return;

        PlayerData.Mesos += amount;
        PersistCurrencyChange(player);
    }

    public bool TrySpendMesos(PlayerCharacter player, int amount)
    {
        if (!CanMutateSession(player, amount))
            return false;

        if (PlayerData.Mesos < amount)
            return false;

        PlayerData.Mesos -= amount;
        PersistCurrencyChange(player);
        return true;
    }

    public void AddConsumable(PlayerCharacter player, PlayerConsumableType type, int amount)
    {
        string itemId = ItemDatabase.GetConsumableItemId(type);
        if (string.IsNullOrEmpty(itemId))
            return;

        AddInventoryItem(player, itemId, amount);
    }

    public bool TryConsumeConsumable(PlayerCharacter player, PlayerConsumableType type)
    {
        string itemId = ItemDatabase.GetConsumableItemId(type);
        if (string.IsNullOrEmpty(itemId))
            return false;

        return TryRemoveInventoryItem(player, itemId, 1);
    }

    public bool TryUseInventoryEntry(PlayerCharacter player, string inventoryEntryId)
    {
        if (PlayerData == null || string.IsNullOrWhiteSpace(inventoryEntryId))
            return false;

        player = ResolveScenePlayer(player);
        if (!IsSupportedSessionPlayer(player) || player == null)
            return false;

        InventoryEntry entry = inventoryService.GetEntryById(inventoryEntryId);
        if (entry == null || !ItemDatabase.TryGetDefinition(entry.ItemId, out ItemDefinition definition))
            return false;

        if (definition.Category != ItemCategory.Consumable)
            return false;

        int restoreHPAmount = definition.RestoreHP;
        int restoreMPAmount = definition.RestoreMP;

        bool canRestoreHP = restoreHPAmount > 0 && PlayerData.CurrentHP < GetEffectiveMaxHP();
        bool canRestoreMP = restoreMPAmount > 0 && PlayerData.CurrentMP < GetEffectiveMaxMP();

        if (!canRestoreHP && !canRestoreMP)
            return false;

        if (!inventoryService.RemoveItem(definition.ItemId, 1, equipmentService.EquippedItems))
            return false;

        if (canRestoreHP)
            player.RestoreHP(restoreHPAmount);

        if (canRestoreMP)
            player.RestoreMP(restoreMPAmount);

        eventPublisher.PublishInventoryChanged(player, definition.ItemId);
        SavePlayer();
        return true;
    }

    public void SetPendingMapTransition(string targetMapId, string targetSpawnId)
    {
        PersistMapStateChange(() => mapStateService.SetPendingTransition(targetMapId, targetSpawnId));
    }

    public void CompleteMapTransition(string targetMapId, string targetSpawnId)
    {
        PersistMapStateChange(() => mapStateService.CompleteTransition(targetMapId, targetSpawnId));
    }

    public void ClearPendingMapTransition()
    {
        PersistMapStateChange(() => mapStateService.ClearPendingTransition());
    }

    public void SavePlayer()
    {
        persistenceService.Save(AccountData);
    }

    public void PublishSessionState(PlayerCharacter player)
    {
        if (PlayerData == null || !IsSupportedSessionPlayer(player))
            return;

        eventPublisher.PublishSessionState(PlayerData, player);
    }

    public void PublishJobState(PlayerCharacter player)
    {
        if (PlayerData == null || !IsSupportedSessionPlayer(player))
            return;

        PlayerData.HasPendingJobAdvancement = PlayerJobCombatProfiles.IsJobAdvancementAvailable(PlayerData);
        eventPublisher.PublishJobState(PlayerData, player);
    }

    public static GameBootstrap FindReadyBootstrap(GameBootstrap current = null)
    {
        if (current != null && current.HasSessionData)
            return current;

        GameBootstrap[] bootstraps = FindObjectsByType<GameBootstrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        GameBootstrap fallback = null;

        foreach (GameBootstrap candidate in bootstraps)
        {
            if (candidate == null)
                continue;

            if (candidate.HasSessionData)
                return candidate;

            fallback ??= candidate;
        }

        return fallback;
    }

    public IReadOnlyList<CharacterSlotData> GetCharacterSlots()
    {
        return persistenceService.GetCharacterSlots(AccountData);
    }

    public CharacterSaveData GetCharacterAtSlot(int slotIndex)
    {
        return persistenceService.GetCharacterAtSlot(AccountData, slotIndex);
    }

    public bool TrySelectCharacter(string characterId)
    {
        if (!persistenceService.TrySelectCharacter(AccountData, characterId, out CharacterSaveData selectedCharacter))
            return false;

        ApplyCharacterSession(selectedCharacter);
        SavePlayer();
        return true;
    }

    public bool TryCreateCharacter(int slotIndex, string nickname, CharacterAppearanceData appearance)
    {
        if (!persistenceService.TryCreateCharacter(
            AccountData,
            slotIndex,
            nickname,
            appearance,
            GetDefaultCharacterStartMapId(),
            GetDefaultCharacterStartSpawnId(),
            out CharacterSaveData createdCharacter))
        {
            return false;
        }

        if (ActiveCharacter == null)
            ApplyCharacterSession(createdCharacter);

        SavePlayer();
        return true;
    }

    private void LoadOrCreateSession()
    {
        AccountData = persistenceService.LoadOrCreateAccountProfile(
            GetDefaultCharacterStartMapId(),
            GetDefaultCharacterStartSpawnId());

        CharacterSaveData resolvedCharacter = persistenceService.ResolveSelectedCharacter(
            AccountData,
            GetDefaultCharacterStartMapId(),
            GetDefaultCharacterStartSpawnId());

        ApplyCharacterSession(resolvedCharacter);
    }

    private void BindServices()
    {
        inventoryService.SetRuntimeData(PlayerData);
        equipmentService.SetRuntimeData(PlayerData, inventoryService);
        skillService.SetRuntimeData(PlayerData);
        mapStateService.SetRuntimeData(PlayerData);
    }

    private void NormalizeLoadedPlayerData()
    {
        if (PlayerData == null)
            return;

        if (PlayerData.Level <= 0)
            PlayerData.Level = 1;

        if (!System.Enum.IsDefined(typeof(PlayerJobType), PlayerData.CurrentJob))
            PlayerData.CurrentJob = PlayerJobType.Novice;

        PlayerData.RequiredExp = GetRequiredExpForLevel(PlayerData.Level);

        if (PlayerData.CurrentExp < 0)
            PlayerData.CurrentExp = 0;

        if (PlayerData.Strength < 0)
            PlayerData.Strength = 0;

        if (PlayerData.Dexterity < 0)
            PlayerData.Dexterity = 0;

        if (PlayerData.MaxHP <= 0)
            PlayerData.MaxHP = 100;

        if (PlayerData.MaxMP <= 0)
            PlayerData.MaxMP = 50;

        int effectiveMaxHP = GetEffectiveMaxHP();
        if (PlayerData.CurrentHP <= 0 || PlayerData.CurrentHP > effectiveMaxHP)
            PlayerData.CurrentHP = effectiveMaxHP;

        int effectiveMaxMP = GetEffectiveMaxMP();
        if (PlayerData.CurrentMP < 0 || PlayerData.CurrentMP > effectiveMaxMP)
            PlayerData.CurrentMP = effectiveMaxMP;

        if (PlayerData.Mesos < 0)
            PlayerData.Mesos = 0;

        inventoryService.MigrateLegacyConsumables();
        skillService.EnsureDefaultSkillsForJob(PlayerData.CurrentJob);

        if (string.IsNullOrWhiteSpace(PlayerData.CurrentMapId))
            PlayerData.CurrentMapId = GetActiveSceneName();

        if (string.IsNullOrWhiteSpace(PlayerData.LastSpawnId))
            PlayerData.LastSpawnId = SceneSpawnPoint.DefaultSpawnId;

        mapStateService.ClearPendingTransition();
        PlayerData.HasPendingJobAdvancement = PlayerJobCombatProfiles.IsJobAdvancementAvailable(PlayerData);
    }

    private int GetEffectiveMaxHP()
    {
        if (PlayerData == null)
            return 0;

        ItemStatModifierData bonuses = equipmentService.GetTotalStatBonuses();
        return Mathf.Max(1, PlayerData.MaxHP + bonuses.MaxHP);
    }

    private int GetEffectiveMaxMP()
    {
        if (PlayerData == null)
            return 0;

        ItemStatModifierData bonuses = equipmentService.GetTotalStatBonuses();
        return Mathf.Max(0, PlayerData.MaxMP + bonuses.MaxMP);
    }

    private int GetRequiredExpForLevel(int level)
    {
        if (level <= 1)
            return 50;

        return 50 + ((level - 1) * 25);
    }

    private string GetActiveSceneName()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() ? activeScene.name : string.Empty;
    }

    private string GetDefaultCharacterStartMapId()
    {
        string configuredMapId = MapRegistry.NormalizeMapId(defaultCharacterStartMapId);
        if (!string.IsNullOrWhiteSpace(configuredMapId))
            return configuredMapId;

        return GetActiveSceneName();
    }

    private string GetDefaultCharacterStartSpawnId()
    {
        return SceneSpawnPoint.NormalizeSpawnId(defaultCharacterStartSpawnId);
    }

    private void ApplyCharacterSession(CharacterSaveData character)
    {
        ActiveCharacter = character;
        PlayerData = character != null ? character.RuntimeData : null;
        BindServices();
        NormalizeLoadedPlayerData();
    }

    private bool HasActivePlayerData()
    {
        return PlayerData != null;
    }

    private bool CanMutateSession(PlayerCharacter player)
    {
        return HasActivePlayerData() && IsSupportedSessionPlayer(player);
    }

    private bool CanMutateSession(PlayerCharacter player, int amount)
    {
        return amount > 0 && CanMutateSession(player);
    }

    private bool IsSupportedSessionPlayer(PlayerCharacter player)
    {
        return player == null || player.IsLocalPlayer;
    }

    private void PersistInventoryChange(PlayerCharacter player, string itemId)
    {
        eventPublisher.PublishInventoryChanged(player, itemId);
        SavePlayer();
    }

    private void PersistCurrencyChange(PlayerCharacter player)
    {
        eventPublisher.PublishCurrencyChanged(PlayerData, player);
        SavePlayer();
    }

    private void PersistMapStateChange(System.Action mutation)
    {
        if (!HasActivePlayerData() || mutation == null)
            return;

        mutation();
        SavePlayer();
    }

    private bool PersistSuccessfulMutation(bool didMutate)
    {
        if (!didMutate)
            return false;

        SavePlayer();
        return true;
    }

    private void RefreshEquippedState(
        PlayerCharacter player,
        EquipmentSlotType slot,
        string inventoryEntryId,
        string itemId,
        bool isEquipped)
    {
        ClampVitalsToEquipmentBonuses();

        PlayerCharacter scenePlayer = ResolveSupportedScenePlayer(player);
        if (scenePlayer != null)
        {
            eventPublisher.PublishSessionState(PlayerData, scenePlayer);

            if (slot != EquipmentSlotType.None)
            {
                PublishEquipmentChanged(
                    scenePlayer,
                    slot,
                    inventoryEntryId,
                    itemId,
                    isEquipped);
            }
        }
    }

    private void ClampVitalsToEquipmentBonuses()
    {
        if (PlayerData == null)
            return;

        PlayerData.CurrentHP = Mathf.Clamp(PlayerData.CurrentHP, 0, GetEffectiveMaxHP());
        PlayerData.CurrentMP = Mathf.Clamp(PlayerData.CurrentMP, 0, GetEffectiveMaxMP());
    }

    private PlayerCharacter ResolveScenePlayer(PlayerCharacter player)
    {
        if (player != null)
            return player;

        return WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
    }

    private PlayerCharacter ResolveSupportedScenePlayer(PlayerCharacter player)
    {
        PlayerCharacter scenePlayer = ResolveScenePlayer(player);
        return IsSupportedSessionPlayer(scenePlayer) ? scenePlayer : null;
    }

    private void PublishEquipmentChangedForScenePlayer(
        PlayerCharacter player,
        EquipmentSlotType slot,
        string inventoryEntryId,
        string itemId,
        bool isEquipped)
    {
        PlayerCharacter scenePlayer = ResolveSupportedScenePlayer(player);
        if (scenePlayer == null)
            return;

        PublishEquipmentChanged(scenePlayer, slot, inventoryEntryId, itemId, isEquipped);
    }

    private void PublishEquipmentChanged(
        PlayerCharacter player,
        EquipmentSlotType slot,
        string inventoryEntryId,
        string itemId,
        bool isEquipped)
    {
        eventPublisher.PublishEquipmentChanged(player, slot, inventoryEntryId, itemId, isEquipped);
    }

    private bool ShouldDestroyDuplicate()
    {
        GameBootstrap[] bootstraps = FindObjectsByType<GameBootstrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        GameBootstrap keeper = this;

        foreach (GameBootstrap existingBootstrap in bootstraps)
        {
            if (existingBootstrap == null)
                continue;

            if (existingBootstrap.GetInstanceID() < keeper.GetInstanceID())
                keeper = existingBootstrap;
        }

        return keeper != this;
    }

    public static string FormatJobName(PlayerJobType jobType)
    {
        return PlayerJobCombatProfiles.GetDisplayName(jobType);
    }
}
