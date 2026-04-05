using System.Collections.Generic;

public class PlayerSessionInventoryService
{
    private PlayerRuntimeData data;
    private PlayerInventoryModule inventory;

    public IReadOnlyList<InventoryEntry> Entries => inventory != null ? inventory.Entries : System.Array.Empty<InventoryEntry>();
    public PlayerInventoryModule InventoryModule => inventory;

    public void SetRuntimeData(PlayerRuntimeData runtimeData)
    {
        data = runtimeData;
        inventory = data != null ? new PlayerInventoryModule(data) : null;
    }

    public bool AddItem(string itemId, int amount)
    {
        if (data == null || amount <= 0)
            return false;

        if (!ItemDatabase.TryGetDefinition(itemId, out _))
            return false;

        return inventory != null && inventory.AddItem(itemId, amount);
    }

    public string AddEquipmentItem(string itemId, ItemEquipmentAppearanceData appearanceOverride = null)
    {
        if (data == null || inventory == null || !ItemDatabase.TryGetDefinition(itemId, out ItemDefinition definition) || !definition.IsEquipment)
            return string.Empty;

        return inventory.AddEquipmentItem(itemId, appearanceOverride);
    }

    public bool RemoveItem(string itemId, int amount, IReadOnlyList<EquippedItemEntry> equippedItems = null)
    {
        if (data == null || amount <= 0 || inventory == null)
            return false;

        return inventory.RemoveItem(itemId, amount, equippedItems);
    }

    public int GetCount(string itemId)
    {
        return inventory != null ? inventory.GetCount(itemId) : 0;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return inventory != null && inventory.HasItem(itemId, amount);
    }

    public InventoryEntry GetEntryById(string entryId)
    {
        return inventory != null ? inventory.GetEntryById(entryId) : null;
    }

    public bool RemoveEntry(string entryId)
    {
        return inventory != null && inventory.RemoveEntry(entryId);
    }

    public void MigrateLegacyConsumables()
    {
        if (data == null || inventory == null)
            return;

        if (data.RedPotionCount > 0)
        {
            inventory.AddItem(ItemDatabase.RedPotionId, data.RedPotionCount);
            data.RedPotionCount = 0;
        }

        if (data.BluePotionCount > 0)
        {
            inventory.AddItem(ItemDatabase.BluePotionId, data.BluePotionCount);
            data.BluePotionCount = 0;
        }
    }
}
