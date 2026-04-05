using System.Collections.Generic;

public static class CharacterAppearanceResolver
{
    public static CharacterAppearanceData Resolve(
        CharacterAppearanceData baseAppearance,
        IReadOnlyList<InventoryEntry> inventoryEntries,
        IReadOnlyList<EquippedItemEntry> equippedItems)
    {
        CharacterAppearanceData resolvedAppearance = baseAppearance != null
            ? baseAppearance.Clone()
            : new CharacterAppearanceData();

        if (inventoryEntries == null || equippedItems == null)
            return resolvedAppearance;

        for (int index = 0; index < equippedItems.Count; index++)
        {
            EquippedItemEntry equippedItem = equippedItems[index];
            if (equippedItem == null || string.IsNullOrWhiteSpace(equippedItem.InventoryEntryId))
                continue;

            InventoryEntry inventoryEntry = FindInventoryEntry(inventoryEntries, equippedItem.InventoryEntryId);
            if (inventoryEntry == null || !ItemDatabase.TryGetDefinition(inventoryEntry.ItemId, out ItemDefinition definition))
                continue;

            ItemEquipmentAppearanceData effectiveAppearance = definition.EquipmentAppearance != null
                ? definition.EquipmentAppearance.Clone()
                : new ItemEquipmentAppearanceData();

            effectiveAppearance.Merge(inventoryEntry.AppearanceOverride);
            effectiveAppearance.ApplyTo(resolvedAppearance);
        }

        return resolvedAppearance;
    }

    private static InventoryEntry FindInventoryEntry(IReadOnlyList<InventoryEntry> inventoryEntries, string inventoryEntryId)
    {
        string normalizedEntryId = inventoryEntryId.Trim();

        for (int index = 0; index < inventoryEntries.Count; index++)
        {
            InventoryEntry inventoryEntry = inventoryEntries[index];
            if (inventoryEntry != null && inventoryEntry.EntryId == normalizedEntryId)
                return inventoryEntry;
        }

        return null;
    }
}
