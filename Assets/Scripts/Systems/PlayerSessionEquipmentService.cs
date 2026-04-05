using System.Collections.Generic;

public class PlayerSessionEquipmentService
{
    private PlayerRuntimeData data;
    private PlayerSessionInventoryService inventoryService;
    private PlayerEquipmentModule equipment;

    public IReadOnlyList<EquippedItemEntry> EquippedItems => equipment != null ? equipment.EquippedItems : System.Array.Empty<EquippedItemEntry>();

    public void SetRuntimeData(PlayerRuntimeData runtimeData, PlayerSessionInventoryService inventorySessionService)
    {
        data = runtimeData;
        inventoryService = inventorySessionService;
        equipment = data != null && inventoryService != null
            ? new PlayerEquipmentModule(data, inventoryService.InventoryModule)
            : null;
    }

    public string AddEquipmentItem(string itemId, ItemEquipmentAppearanceData appearanceOverride = null, bool autoEquip = false)
    {
        if (inventoryService == null)
            return string.Empty;

        string entryId = inventoryService.AddEquipmentItem(itemId, appearanceOverride);
        if (string.IsNullOrWhiteSpace(entryId))
            return string.Empty;

        if (autoEquip)
            TryEquipEntry(entryId, out _, out _);

        return entryId;
    }

    public bool TryEquipEntry(string inventoryEntryId, out EquipmentSlotType slot, out string replacedInventoryEntryId)
    {
        if (equipment == null)
        {
            slot = EquipmentSlotType.None;
            replacedInventoryEntryId = string.Empty;
            return false;
        }

        return equipment.TryEquipEntry(inventoryEntryId, out slot, out replacedInventoryEntryId);
    }

    public bool TryUnequip(EquipmentSlotType slot, out string inventoryEntryId)
    {
        if (equipment == null)
        {
            inventoryEntryId = string.Empty;
            return false;
        }

        return equipment.TryUnequip(slot, out inventoryEntryId);
    }

    public bool IsEntryEquipped(string inventoryEntryId)
    {
        return equipment != null && equipment.IsEntryEquipped(inventoryEntryId);
    }

    public InventoryEntry GetEquippedEntry(EquipmentSlotType slot)
    {
        return equipment != null ? equipment.GetEquippedEntry(slot) : null;
    }

    public ItemStatModifierData GetTotalStatBonuses()
    {
        return equipment != null ? equipment.GetTotalStatBonuses() : new ItemStatModifierData();
    }

    public CharacterAppearanceData BuildResolvedAppearance(CharacterAppearanceData baseAppearance)
    {
        return CharacterAppearanceResolver.Resolve(
            baseAppearance,
            inventoryService != null ? inventoryService.Entries : System.Array.Empty<InventoryEntry>(),
            EquippedItems);
    }
}
