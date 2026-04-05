using System.Collections.Generic;
using UnityEngine;

public static class ItemDatabase
{
    public const string RedPotionId = "red_potion";
    public const string BluePotionId = "blue_potion";
    public const string SlimeGelId = "slime_gel";
    public const string TurtleShellId = "turtle_shell";

    private const string ResourcePath = "GameData/ItemDatabase";

    private static Dictionary<string, ItemDefinition> items;
    private static ItemDatabaseAsset asset;

    public static bool TryGetDefinition(string itemId, out ItemDefinition definition)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(itemId))
        {
            definition = null;
            return false;
        }

        return items.TryGetValue(itemId.Trim(), out definition);
    }

    public static ItemDefinition GetDefinition(string itemId)
    {
        return TryGetDefinition(itemId, out ItemDefinition definition)
            ? definition
            : null;
    }

    public static IReadOnlyList<ItemDefinition> GetAllDefinitions()
    {
        EnsureLoaded();
        return new List<ItemDefinition>(items.Values);
    }

    public static bool TryFindEquipmentDefinition(EquipmentSlotType slot, string appearanceId, out ItemDefinition definition)
    {
        EnsureLoaded();
        definition = null;

        if (slot == EquipmentSlotType.None || string.IsNullOrWhiteSpace(appearanceId))
            return false;

        string normalizedAppearanceId = appearanceId.Trim();

        foreach (KeyValuePair<string, ItemDefinition> pair in items)
        {
            ItemDefinition candidate = pair.Value;
            if (candidate == null || !candidate.IsEquipment || candidate.EquipmentSlot != slot)
                continue;

            if (MatchesAppearanceId(candidate.EquipmentAppearance, slot, normalizedAppearanceId))
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    public static string GetConsumableItemId(PlayerConsumableType consumableType)
    {
        return consumableType switch
        {
            PlayerConsumableType.RedPotion => RedPotionId,
            PlayerConsumableType.BluePotion => BluePotionId,
            _ => string.Empty
        };
    }

    public static void ResetCache()
    {
        items = null;
        asset = null;
    }

    private static void EnsureLoaded()
    {
        if (items != null)
            return;

        asset = Resources.Load<ItemDatabaseAsset>(ResourcePath);
        items = BuildLookup(asset != null && asset.Items != null && asset.Items.Count > 0
            ? asset.Items
            : System.Array.Empty<ItemDefinition>());

        IReadOnlyList<ItemDefinition> fallbackDefinitions = CreateFallbackDefinitions();
        for (int index = 0; index < fallbackDefinitions.Count; index++)
        {
            ItemDefinition definition = fallbackDefinitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId) || items.ContainsKey(definition.ItemId))
                continue;

            items[definition.ItemId] = definition;
        }
    }

    private static Dictionary<string, ItemDefinition> BuildLookup(IReadOnlyList<ItemDefinition> definitions)
    {
        Dictionary<string, ItemDefinition> lookup = new Dictionary<string, ItemDefinition>();

        if (definitions == null)
            return lookup;

        for (int index = 0; index < definitions.Count; index++)
        {
            ItemDefinition definition = definitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId))
                continue;

            lookup[definition.ItemId] = definition;
        }

        return lookup;
    }

    private static IReadOnlyList<ItemDefinition> CreateFallbackDefinitions()
    {
        List<ItemDefinition> fallbackDefinitions = new List<ItemDefinition>
        {
            ItemDefinition.CreateTransient(
                RedPotionId,
                "Red Potion",
                ItemCategory.Consumable,
                100,
                50,
                25,
                50,
                0,
                new Color(0.94f, 0.32f, 0.36f),
                new Color(1f, 0.77f, 0.8f)),
            ItemDefinition.CreateTransient(
                BluePotionId,
                "Blue Potion",
                ItemCategory.Consumable,
                100,
                60,
                30,
                0,
                30,
                new Color(0.22f, 0.56f, 0.96f),
                new Color(0.72f, 0.9f, 1f)),
            ItemDefinition.CreateTransient(
                SlimeGelId,
                "Slime Gel",
                ItemCategory.Etc,
                200,
                0,
                6,
                0,
                0,
                new Color(0.49f, 0.83f, 0.39f),
                new Color(0.84f, 1f, 0.74f)),
            ItemDefinition.CreateTransient(
                TurtleShellId,
                "Turtle Shell",
                ItemCategory.Etc,
                200,
                0,
                10,
                0,
                0,
                new Color(0.46f, 0.57f, 0.7f),
                new Color(0.84f, 0.91f, 1f))
        };

        fallbackDefinitions.Add(CreateFallbackEquipmentDefinition(
            "eq_overall_body01",
            "Body 01 Overall",
            EquipmentSlotType.Overall,
            new ItemEquipmentAppearanceData
            {
                OutfitId = "Body01"
            }));

        fallbackDefinitions.Add(CreateFallbackEquipmentDefinition(
            "eq_overall_body02",
            "Body 02 Overall",
            EquipmentSlotType.Overall,
            new ItemEquipmentAppearanceData
            {
                OutfitId = "Body02"
            }));

        fallbackDefinitions.Add(CreateFallbackEquipmentDefinition(
            "eq_overall_body03",
            "Body 03 Overall",
            EquipmentSlotType.Overall,
            new ItemEquipmentAppearanceData
            {
                OutfitId = "Body03"
            }));

        fallbackDefinitions.Add(CreateFallbackEquipmentDefinition(
            "eq_weapon_right_ohs01_stick",
            "Stick",
            EquipmentSlotType.WeaponRight,
            new ItemEquipmentAppearanceData
            {
                WeaponRightId = "OHS01_Stick",
                ApplyWeaponRightColor = true,
                WeaponRightColor = Color.white
            }));

        fallbackDefinitions.Add(CreateFallbackEquipmentDefinition(
            "eq_weapon_right_ohs03_sword",
            "Sword 03",
            EquipmentSlotType.WeaponRight,
            new ItemEquipmentAppearanceData
            {
                WeaponRightId = "OHS03_Sword",
                ApplyWeaponRightColor = true,
                WeaponRightColor = Color.white
            }));

        fallbackDefinitions.Add(CreateFallbackEquipmentDefinition(
            "eq_weapon_right_wand01",
            "Wand 01",
            EquipmentSlotType.WeaponRight,
            new ItemEquipmentAppearanceData
            {
                WeaponRightId = "Wand01",
                ApplyWeaponRightColor = true,
                WeaponRightColor = Color.white
            }));

        return fallbackDefinitions;
    }

    private static ItemDefinition CreateFallbackEquipmentDefinition(
        string itemId,
        string displayName,
        EquipmentSlotType slot,
        ItemEquipmentAppearanceData appearance)
    {
        ItemDefinition definition = ItemDefinition.CreateTransient(
            itemId,
            displayName,
            ItemCategory.Equipment,
            1,
            0,
            0,
            0,
            0,
            Color.white,
            Color.white);
        definition.ConfigureEquipment(slot, new ItemStatModifierData(), appearance);
        return definition;
    }

    private static bool MatchesAppearanceId(ItemEquipmentAppearanceData appearance, EquipmentSlotType slot, string appearanceId)
    {
        if (appearance == null || string.IsNullOrWhiteSpace(appearanceId))
            return false;

        return GetAppearanceIdForSlot(appearance, slot) == appearanceId;
    }

    private static string GetAppearanceIdForSlot(ItemEquipmentAppearanceData appearance, EquipmentSlotType slot)
    {
        if (appearance == null)
            return string.Empty;

        return slot switch
        {
            EquipmentSlotType.Overall => NormalizeId(appearance.OutfitId),
            EquipmentSlotType.WeaponRight => NormalizeId(appearance.WeaponRightId),
            EquipmentSlotType.Hat => NormalizeId(appearance.HatId),
            EquipmentSlotType.Cape => NormalizeId(appearance.CapeId),
            EquipmentSlotType.Horns => NormalizeId(appearance.HornsId),
            EquipmentSlotType.Accessory => NormalizeId(appearance.AccessoryId),
            EquipmentSlotType.NinjaMask => NormalizeId(appearance.NinjaMaskId),
            EquipmentSlotType.Mustache => NormalizeId(appearance.MustacheId),
            _ => string.Empty
        };
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
