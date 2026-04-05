using System.Collections.Generic;
using System;

public class PlayerInventoryModule
{
    private readonly PlayerRuntimeData data;

    public PlayerInventoryModule(PlayerRuntimeData runtimeData)
    {
        data = runtimeData;
        EnsureInventory();
    }

    public IReadOnlyList<InventoryEntry> Entries => data?.Inventory;

    public bool AddItem(string itemId, int amount)
    {
        if (data == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !ItemDatabase.TryGetDefinition(itemId, out ItemDefinition definition))
            return false;

        EnsureInventory();
        if (definition.IsEquipment || definition.MaxStack <= 1)
        {
            for (int index = 0; index < amount; index++)
                CreateInstanceEntry(itemId, null);

            return true;
        }

        InventoryEntry entry = GetOrCreateStackEntry(itemId);
        entry.Count += amount;
        return true;
    }

    public string AddEquipmentItem(string itemId, ItemEquipmentAppearanceData appearanceOverride = null)
    {
        if (data == null || string.IsNullOrWhiteSpace(itemId) || !ItemDatabase.TryGetDefinition(itemId, out ItemDefinition definition) || !definition.IsEquipment)
            return string.Empty;

        EnsureInventory();
        InventoryEntry entry = CreateInstanceEntry(itemId, appearanceOverride);
        return entry != null ? entry.EntryId : string.Empty;
    }

    public bool RemoveItem(string itemId, int amount, IReadOnlyList<EquippedItemEntry> equippedItems = null)
    {
        if (data == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !ItemDatabase.TryGetDefinition(itemId, out ItemDefinition definition))
            return false;

        EnsureInventory();
        if (!definition.IsEquipment && definition.MaxStack > 1)
        {
            InventoryEntry entry = FindStackEntry(itemId);
            if (entry == null || entry.Count < amount)
                return false;

            entry.Count -= amount;

            if (entry.Count <= 0)
                data.Inventory.Remove(entry);

            return true;
        }

        List<InventoryEntry> removableEntries = new List<InventoryEntry>();
        string normalizedItemId = itemId.Trim();

        for (int index = 0; index < data.Inventory.Count; index++)
        {
            InventoryEntry entry = data.Inventory[index];
            if (entry == null || entry.ItemId != normalizedItemId || IsEquipped(entry.EntryId, equippedItems))
                continue;

            removableEntries.Add(entry);
            if (removableEntries.Count >= amount)
                break;
        }

        if (removableEntries.Count < amount)
            return false;

        for (int index = 0; index < removableEntries.Count; index++)
            data.Inventory.Remove(removableEntries[index]);

        return true;
    }

    public bool RemoveEntry(string entryId)
    {
        if (data?.Inventory == null || string.IsNullOrWhiteSpace(entryId))
            return false;

        InventoryEntry entry = GetEntryById(entryId);
        if (entry == null)
            return false;

        return data.Inventory.Remove(entry);
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return GetCount(itemId) >= amount;
    }

    public int GetCount(string itemId)
    {
        if (data == null || string.IsNullOrWhiteSpace(itemId))
            return 0;

        EnsureInventory();
        string normalizedItemId = itemId.Trim();
        int count = 0;

        foreach (InventoryEntry entry in data.Inventory)
        {
            if (entry != null && entry.ItemId == normalizedItemId)
                count += Math.Max(1, entry.Count);
        }

        return count;
    }

    public InventoryEntry GetEntryById(string entryId)
    {
        if (data?.Inventory == null || string.IsNullOrWhiteSpace(entryId))
            return null;

        EnsureInventory();
        string normalizedEntryId = entryId.Trim();

        foreach (InventoryEntry entry in data.Inventory)
        {
            if (entry != null && entry.EntryId == normalizedEntryId)
                return entry;
        }

        return null;
    }

    private void EnsureInventory()
    {
        if (data != null && data.Inventory == null)
            data.Inventory = new List<InventoryEntry>();

        if (data?.Inventory == null)
            return;

        for (int index = data.Inventory.Count - 1; index >= 0; index--)
        {
            InventoryEntry entry = data.Inventory[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
            {
                data.Inventory.RemoveAt(index);
                continue;
            }

            entry.ItemId = entry.ItemId.Trim();
            entry.EntryId = string.IsNullOrWhiteSpace(entry.EntryId) ? Guid.NewGuid().ToString("N") : entry.EntryId.Trim();
            entry.Count = Math.Max(1, entry.Count);
            entry.AppearanceOverride?.Sanitize();
        }
    }

    private InventoryEntry GetOrCreateStackEntry(string itemId)
    {
        InventoryEntry entry = FindStackEntry(itemId);
        if (entry != null)
            return entry;

        entry = new InventoryEntry
        {
            EntryId = Guid.NewGuid().ToString("N"),
            ItemId = itemId.Trim(),
            Count = 0
        };

        data.Inventory.Add(entry);
        return entry;
    }

    private InventoryEntry CreateInstanceEntry(string itemId, ItemEquipmentAppearanceData appearanceOverride)
    {
        InventoryEntry entry = new InventoryEntry
        {
            EntryId = Guid.NewGuid().ToString("N"),
            ItemId = itemId.Trim(),
            Count = 1,
            AppearanceOverride = appearanceOverride != null ? appearanceOverride.Clone() : null
        };

        entry.AppearanceOverride?.Sanitize();
        data.Inventory.Add(entry);
        return entry;
    }

    private InventoryEntry FindStackEntry(string itemId)
    {
        if (data?.Inventory == null)
            return null;

        string normalizedItemId = itemId.Trim();

        foreach (InventoryEntry entry in data.Inventory)
        {
            if (entry != null && entry.ItemId == normalizedItemId)
                return entry;
        }

        return null;
    }

    private static bool IsEquipped(string entryId, IReadOnlyList<EquippedItemEntry> equippedItems)
    {
        if (equippedItems == null || string.IsNullOrWhiteSpace(entryId))
            return false;

        string normalizedEntryId = entryId.Trim();

        for (int index = 0; index < equippedItems.Count; index++)
        {
            EquippedItemEntry equippedItem = equippedItems[index];
            if (equippedItem != null && equippedItem.InventoryEntryId == normalizedEntryId)
                return true;
        }

        return false;
    }
}
