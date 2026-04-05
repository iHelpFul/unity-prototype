public struct PlayerEquipmentChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public EquipmentSlotType Slot;
    public string InventoryEntryId;
    public string ItemId;
    public bool IsEquipped;
}
