using System.Collections.Generic;

public class PlayerSessionEventPublisher
{
    private readonly PlayerSessionInventoryService inventoryService;
    private readonly PlayerSessionEquipmentService equipmentService;

    public PlayerSessionEventPublisher(
        PlayerSessionInventoryService inventoryService,
        PlayerSessionEquipmentService equipmentService)
    {
        this.inventoryService = inventoryService;
        this.equipmentService = equipmentService;
    }

    public void PublishSessionState(PlayerRuntimeData data, PlayerCharacter player)
    {
        if (data == null || player == null)
            return;

        string characterId = player.CharacterId;
        ItemStatModifierData statBonuses = equipmentService != null
            ? equipmentService.GetTotalStatBonuses()
            : new ItemStatModifierData();

        EventBus.Publish(new PlayerHealthChangedEvent
        {
            Target = player,
            CharacterId = characterId,
            CurrentHP = data.CurrentHP,
            MaxHP = System.Math.Max(1, data.MaxHP + statBonuses.MaxHP)
        });

        EventBus.Publish(new PlayerManaChangedEvent
        {
            Target = player,
            CharacterId = characterId,
            CurrentMP = data.CurrentMP,
            MaxMP = System.Math.Max(0, data.MaxMP + statBonuses.MaxMP)
        });

        EventBus.Publish(new PlayerExpChangedEvent
        {
            Target = player,
            CharacterId = characterId,
            CurrentExp = data.CurrentExp,
            RequiredExp = data.RequiredExp
        });

        PublishCurrencyChanged(data, player);
        PublishJobState(data, player);
        PublishConsumablesChanged(player);
        PublishAllInventoryEntries(player);
        PublishAllEquippedItems(player);
    }

    public void PublishJobState(PlayerRuntimeData data, PlayerCharacter player)
    {
        if (data == null || player == null)
            return;

        EventBus.Publish(new PlayerJobStateChangedEvent
        {
            Target = player,
            CharacterId = player.CharacterId,
            CurrentJob = data.CurrentJob,
            IsJobAdvancementAvailable = data.HasPendingJobAdvancement
        });
    }

    public void PublishInventoryChanged(PlayerCharacter player, string itemId)
    {
        if (player == null || string.IsNullOrWhiteSpace(itemId))
            return;

        EventBus.Publish(new PlayerInventoryChangedEvent
        {
            Target = player,
            CharacterId = player.CharacterId,
            ItemId = itemId,
            Count = inventoryService.GetCount(itemId)
        });

        if (itemId == ItemDatabase.RedPotionId || itemId == ItemDatabase.BluePotionId)
            PublishConsumablesChanged(player);
    }

    public void PublishAllInventoryEntries(PlayerCharacter player)
    {
        if (player == null)
            return;

        IReadOnlyList<InventoryEntry> entries = inventoryService.Entries;
        if (entries == null)
            return;

        foreach (InventoryEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                continue;

            EventBus.Publish(new PlayerInventoryChangedEvent
            {
                Target = player,
                CharacterId = player.CharacterId,
                ItemId = entry.ItemId,
                Count = entry.Count
            });
        }
    }

    public void PublishEquipmentChanged(PlayerCharacter player, EquipmentSlotType slot, string inventoryEntryId, string itemId, bool isEquipped)
    {
        if (player == null)
            return;

        EventBus.Publish(new PlayerEquipmentChangedEvent
        {
            Target = player,
            CharacterId = player.CharacterId,
            Slot = slot,
            InventoryEntryId = string.IsNullOrWhiteSpace(inventoryEntryId) ? string.Empty : inventoryEntryId.Trim(),
            ItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim(),
            IsEquipped = isEquipped
        });
    }

    public void PublishAllEquippedItems(PlayerCharacter player)
    {
        if (player == null || equipmentService == null)
            return;

        IReadOnlyList<EquippedItemEntry> equippedItems = equipmentService.EquippedItems;
        if (equippedItems == null)
            return;

        for (int index = 0; index < equippedItems.Count; index++)
        {
            EquippedItemEntry equippedItem = equippedItems[index];
            if (equippedItem == null)
                continue;

            InventoryEntry inventoryEntry = inventoryService.GetEntryById(equippedItem.InventoryEntryId);
            PublishEquipmentChanged(
                player,
                equippedItem.Slot,
                equippedItem.InventoryEntryId,
                inventoryEntry != null ? inventoryEntry.ItemId : string.Empty,
                true);
        }
    }

    public void PublishConsumablesChanged(PlayerCharacter player)
    {
        if (player == null)
            return;

        EventBus.Publish(new PlayerConsumablesChangedEvent
        {
            Target = player,
            CharacterId = player.CharacterId,
            RedPotions = inventoryService.GetCount(ItemDatabase.RedPotionId),
            BluePotions = inventoryService.GetCount(ItemDatabase.BluePotionId)
        });
    }

    public void PublishCurrencyChanged(PlayerRuntimeData data, PlayerCharacter player)
    {
        if (data == null || player == null)
            return;

        EventBus.Publish(new PlayerCurrencyChangedEvent
        {
            Target = player,
            CharacterId = player.CharacterId,
            Mesos = data.Mesos
        });
    }
}
