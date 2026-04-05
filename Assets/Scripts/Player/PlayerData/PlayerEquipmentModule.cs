using System.Collections.Generic;

public class PlayerEquipmentModule
{
    private readonly PlayerRuntimeData data;
    private readonly PlayerInventoryModule inventory;

    public PlayerEquipmentModule(PlayerRuntimeData runtimeData, PlayerInventoryModule inventoryModule)
    {
        data = runtimeData;
        inventory = inventoryModule;
        EnsureEquipment();
    }

    public IReadOnlyList<EquippedItemEntry> EquippedItems => data?.EquippedItems;

    public bool TryEquipEntry(string inventoryEntryId, out EquipmentSlotType slot, out string replacedInventoryEntryId)
    {
        slot = EquipmentSlotType.None;
        replacedInventoryEntryId = string.Empty;

        if (data == null || inventory == null || string.IsNullOrWhiteSpace(inventoryEntryId))
            return false;

        InventoryEntry inventoryEntry = inventory.GetEntryById(inventoryEntryId);
        if (inventoryEntry == null || !ItemDatabase.TryGetDefinition(inventoryEntry.ItemId, out ItemDefinition definition) || !definition.IsEquipment)
            return false;

        slot = definition.EquipmentSlot;
        if (slot == EquipmentSlotType.None)
            return false;

        EnsureEquipment();
        RemoveEntryFromOtherSlots(inventoryEntryId);

        EquippedItemEntry existing = FindEquippedSlot(slot);
        if (existing != null)
        {
            if (existing.InventoryEntryId == inventoryEntryId)
                return true;

            replacedInventoryEntryId = existing.InventoryEntryId;
            existing.InventoryEntryId = inventoryEntryId.Trim();
            return true;
        }

        data.EquippedItems.Add(new EquippedItemEntry
        {
            Slot = slot,
            InventoryEntryId = inventoryEntryId.Trim()
        });
        return true;
    }

    public bool TryUnequip(EquipmentSlotType slot, out string unequippedInventoryEntryId)
    {
        unequippedInventoryEntryId = string.Empty;

        if (data?.EquippedItems == null || slot == EquipmentSlotType.None)
            return false;

        EquippedItemEntry entry = FindEquippedSlot(slot);
        if (entry == null)
            return false;

        unequippedInventoryEntryId = entry.InventoryEntryId;
        data.EquippedItems.Remove(entry);
        return true;
    }

    public bool IsEntryEquipped(string inventoryEntryId)
    {
        if (data?.EquippedItems == null || string.IsNullOrWhiteSpace(inventoryEntryId))
            return false;

        string normalizedEntryId = inventoryEntryId.Trim();

        for (int index = 0; index < data.EquippedItems.Count; index++)
        {
            EquippedItemEntry equippedItem = data.EquippedItems[index];
            if (equippedItem != null && equippedItem.InventoryEntryId == normalizedEntryId)
                return true;
        }

        return false;
    }

    public InventoryEntry GetEquippedEntry(EquipmentSlotType slot)
    {
        if (inventory == null)
            return null;

        EquippedItemEntry equippedItem = FindEquippedSlot(slot);
        return equippedItem != null ? inventory.GetEntryById(equippedItem.InventoryEntryId) : null;
    }

    public ItemStatModifierData GetTotalStatBonuses()
    {
        ItemStatModifierData totals = new ItemStatModifierData();

        if (data?.EquippedItems == null || inventory == null)
            return totals;

        for (int index = 0; index < data.EquippedItems.Count; index++)
        {
            EquippedItemEntry equippedItem = data.EquippedItems[index];
            if (equippedItem == null)
                continue;

            InventoryEntry inventoryEntry = inventory.GetEntryById(equippedItem.InventoryEntryId);
            if (inventoryEntry == null || !ItemDatabase.TryGetDefinition(inventoryEntry.ItemId, out ItemDefinition definition))
                continue;

            totals.Add(definition.EquipmentStatBonuses);
        }

        return totals;
    }

    private void EnsureEquipment()
    {
        if (data == null)
            return;

        data.EquippedItems ??= new List<EquippedItemEntry>();

        HashSet<EquipmentSlotType> occupiedSlots = new HashSet<EquipmentSlotType>();

        for (int index = data.EquippedItems.Count - 1; index >= 0; index--)
        {
            EquippedItemEntry entry = data.EquippedItems[index];
            if (entry == null || entry.Slot == EquipmentSlotType.None || string.IsNullOrWhiteSpace(entry.InventoryEntryId))
            {
                data.EquippedItems.RemoveAt(index);
                continue;
            }

            entry.InventoryEntryId = entry.InventoryEntryId.Trim();

            if (!occupiedSlots.Add(entry.Slot))
            {
                data.EquippedItems.RemoveAt(index);
                continue;
            }

            InventoryEntry inventoryEntry = inventory != null ? inventory.GetEntryById(entry.InventoryEntryId) : null;
            if (inventoryEntry == null || !ItemDatabase.TryGetDefinition(inventoryEntry.ItemId, out ItemDefinition definition) || !definition.IsEquipment || definition.EquipmentSlot != entry.Slot)
                data.EquippedItems.RemoveAt(index);
        }
    }

    private EquippedItemEntry FindEquippedSlot(EquipmentSlotType slot)
    {
        if (data?.EquippedItems == null)
            return null;

        for (int index = 0; index < data.EquippedItems.Count; index++)
        {
            EquippedItemEntry equippedItem = data.EquippedItems[index];
            if (equippedItem != null && equippedItem.Slot == slot)
                return equippedItem;
        }

        return null;
    }

    private void RemoveEntryFromOtherSlots(string inventoryEntryId)
    {
        if (data?.EquippedItems == null || string.IsNullOrWhiteSpace(inventoryEntryId))
            return;

        string normalizedEntryId = inventoryEntryId.Trim();

        for (int index = data.EquippedItems.Count - 1; index >= 0; index--)
        {
            EquippedItemEntry equippedItem = data.EquippedItems[index];
            if (equippedItem != null && equippedItem.InventoryEntryId == normalizedEntryId)
                data.EquippedItems.RemoveAt(index);
        }
    }
}
