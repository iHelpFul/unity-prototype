public static class CharacterStarterEquipmentFactory
{
    public static void ApplyStarterEquipment(CharacterSaveData character)
    {
        if (character?.RuntimeData == null || character.Appearance == null)
            return;

        PlayerInventoryModule inventory = new PlayerInventoryModule(character.RuntimeData);
        PlayerEquipmentModule equipment = new PlayerEquipmentModule(character.RuntimeData, inventory);

        TryGrantOverall(character.Appearance, inventory, equipment);
        TryGrantWeapon(character.Appearance, inventory, equipment);
    }

    private static void TryGrantOverall(
        CharacterAppearanceData appearance,
        PlayerInventoryModule inventory,
        PlayerEquipmentModule equipment)
    {
        if (appearance == null || string.IsNullOrWhiteSpace(appearance.OutfitId))
            return;

        if (!ItemDatabase.TryFindEquipmentDefinition(EquipmentSlotType.Overall, appearance.OutfitId, out ItemDefinition definition))
            return;

        string entryId = inventory.AddEquipmentItem(definition.ItemId, new ItemEquipmentAppearanceData
        {
            OutfitId = appearance.OutfitId,
            ApplyPrimaryClothingColor = true,
            PrimaryClothingColor = appearance.PrimaryClothingColor,
            ApplySecondaryClothingColor = true,
            SecondaryClothingColor = appearance.SecondaryClothingColor
        });

        if (!string.IsNullOrWhiteSpace(entryId))
        {
            equipment.TryEquipEntry(entryId, out _, out _);
            appearance.OutfitId = "Body01";
        }
    }

    private static void TryGrantWeapon(
        CharacterAppearanceData appearance,
        PlayerInventoryModule inventory,
        PlayerEquipmentModule equipment)
    {
        if (appearance == null || string.IsNullOrWhiteSpace(appearance.WeaponRightId) || appearance.WeaponRightId == "None")
            return;

        if (!ItemDatabase.TryFindEquipmentDefinition(EquipmentSlotType.WeaponRight, appearance.WeaponRightId, out ItemDefinition definition))
            return;

        string entryId = inventory.AddEquipmentItem(definition.ItemId, new ItemEquipmentAppearanceData
        {
            WeaponRightId = appearance.WeaponRightId,
            ApplyWeaponRightColor = true,
            WeaponRightColor = appearance.WeaponRightColor
        });

        if (!string.IsNullOrWhiteSpace(entryId))
        {
            equipment.TryEquipEntry(entryId, out _, out _);
            appearance.WeaponRightId = "None";
        }
    }
}
