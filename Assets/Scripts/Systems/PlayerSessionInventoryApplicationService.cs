using System;
using System.Collections.Generic;

public class PlayerSessionInventoryApplicationService
{
    private readonly PlayerSessionInventoryService inventoryService;
    private readonly PlayerSessionEquipmentService equipmentService;
    private readonly PlayerSessionEventPublisher eventPublisher;
    private readonly Func<PlayerRuntimeData> getPlayerData;
    private readonly Func<int> getEffectiveMaxHP;
    private readonly Func<int> getEffectiveMaxMP;
    private readonly Action savePlayer;

    public PlayerSessionInventoryApplicationService(
        PlayerSessionInventoryService inventoryService,
        PlayerSessionEquipmentService equipmentService,
        PlayerSessionEventPublisher eventPublisher,
        Func<PlayerRuntimeData> getPlayerData,
        Func<int> getEffectiveMaxHP,
        Func<int> getEffectiveMaxMP,
        Action savePlayer)
    {
        this.inventoryService = inventoryService;
        this.equipmentService = equipmentService;
        this.eventPublisher = eventPublisher;
        this.getPlayerData = getPlayerData;
        this.getEffectiveMaxHP = getEffectiveMaxHP;
        this.getEffectiveMaxMP = getEffectiveMaxMP;
        this.savePlayer = savePlayer;
    }

    public bool AddInventoryItem(PlayerCharacter player, string itemId, int amount)
    {
        if (!inventoryService.AddItem(itemId, amount))
            return false;

        eventPublisher.PublishInventoryChanged(player, itemId);
        savePlayer?.Invoke();
        return true;
    }

    public bool TryRemoveInventoryItem(PlayerCharacter player, string itemId, int amount)
    {
        if (!inventoryService.RemoveItem(itemId, amount, equipmentService.EquippedItems))
            return false;

        eventPublisher.PublishInventoryChanged(player, itemId);
        savePlayer?.Invoke();
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

    public InventoryEntry GetInventoryEntry(string entryId)
    {
        return inventoryService.GetEntryById(entryId);
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
        PlayerRuntimeData playerData = ResolvePlayerData();
        int effectiveMaxHP = ResolveEffectiveMaxHP();
        int effectiveMaxMP = ResolveEffectiveMaxMP();

        if (playerData == null || player == null || string.IsNullOrWhiteSpace(inventoryEntryId))
            return false;

        InventoryEntry entry = inventoryService.GetEntryById(inventoryEntryId);
        if (entry == null || !ItemDatabase.TryGetDefinition(entry.ItemId, out ItemDefinition definition))
            return false;

        if (definition.Category != ItemCategory.Consumable)
            return false;

        int restoreHPAmount = definition.RestoreHP;
        int restoreMPAmount = definition.RestoreMP;

        bool canRestoreHP = restoreHPAmount > 0 && playerData.CurrentHP < effectiveMaxHP;
        bool canRestoreMP = restoreMPAmount > 0 && playerData.CurrentMP < effectiveMaxMP;

        if (!canRestoreHP && !canRestoreMP)
            return false;

        if (!inventoryService.RemoveItem(definition.ItemId, 1, equipmentService.EquippedItems))
            return false;

        if (canRestoreHP)
            player.RestoreHP(restoreHPAmount);

        if (canRestoreMP)
            player.RestoreMP(restoreMPAmount);

        eventPublisher.PublishInventoryChanged(player, definition.ItemId);
        savePlayer?.Invoke();
        return true;
    }

    private PlayerRuntimeData ResolvePlayerData()
    {
        return getPlayerData != null ? getPlayerData() : null;
    }

    private int ResolveEffectiveMaxHP()
    {
        return getEffectiveMaxHP != null ? getEffectiveMaxHP() : 0;
    }

    private int ResolveEffectiveMaxMP()
    {
        return getEffectiveMaxMP != null ? getEffectiveMaxMP() : 0;
    }
}
