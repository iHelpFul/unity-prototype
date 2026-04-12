using UnityEngine;

public enum ItemCategory
{
    Consumable,
    Equipment,
    Etc
}

[CreateAssetMenu(menuName = "Game Data/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId = string.Empty;
    [SerializeField] private string displayName = "New Item";
    [SerializeField] private ItemCategory category = ItemCategory.Etc;
    [SerializeField] private int maxStack = 1;
    [SerializeField] private int buyPrice;
    [SerializeField] private int sellPrice;
    [SerializeField] private int restoreHP;
    [SerializeField] private int restoreMP;
    [SerializeField] private Color primaryColor = Color.white;
    [SerializeField] private Color accentColor = Color.white;
    [SerializeField] private GameObject worldLootPickupPrefab;
    [Header("Equipment")]
    [SerializeField] private EquipmentSlotType equipmentSlot = EquipmentSlotType.None;
    [SerializeField] private ItemStatModifierData equipmentStatBonuses = new ItemStatModifierData();
    [SerializeField] private ItemEquipmentAppearanceData equipmentAppearance = new ItemEquipmentAppearanceData();

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public ItemCategory Category => category;
    public bool IsEquipment => category == ItemCategory.Equipment;
    public int MaxStack => maxStack;
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;
    public int RestoreHP => restoreHP;
    public int RestoreMP => restoreMP;
    public Color PrimaryColor => primaryColor;
    public Color AccentColor => accentColor;
    public GameObject WorldLootPickupPrefab => worldLootPickupPrefab;
    public EquipmentSlotType EquipmentSlot => IsEquipment ? equipmentSlot : EquipmentSlotType.None;
    public ItemStatModifierData EquipmentStatBonuses => equipmentStatBonuses;
    public ItemEquipmentAppearanceData EquipmentAppearance => equipmentAppearance;

    public void Initialize(
        string newItemId,
        string newDisplayName,
        ItemCategory newCategory,
        int newMaxStack,
        int newBuyPrice,
        int newSellPrice,
        int newRestoreHP,
        int newRestoreMP,
        Color newPrimaryColor,
        Color newAccentColor)
    {
        itemId = newItemId;
        displayName = newDisplayName;
        category = newCategory;
        maxStack = newMaxStack;
        buyPrice = newBuyPrice;
        sellPrice = newSellPrice;
        restoreHP = newRestoreHP;
        restoreMP = newRestoreMP;
        primaryColor = newPrimaryColor;
        accentColor = newAccentColor;
        Sanitize();
    }

    public void ConfigureEquipment(
        EquipmentSlotType newEquipmentSlot,
        ItemStatModifierData newStatBonuses = null,
        ItemEquipmentAppearanceData newEquipmentAppearance = null)
    {
        category = ItemCategory.Equipment;
        equipmentSlot = newEquipmentSlot;
        equipmentStatBonuses = newStatBonuses != null ? newStatBonuses.Clone() : new ItemStatModifierData();
        equipmentAppearance = newEquipmentAppearance != null ? newEquipmentAppearance.Clone() : new ItemEquipmentAppearanceData();
        Sanitize();
    }

    public static ItemDefinition CreateTransient(
        string newItemId,
        string newDisplayName,
        ItemCategory newCategory,
        int newMaxStack,
        int newBuyPrice,
        int newSellPrice,
        int newRestoreHP,
        int newRestoreMP,
        Color newPrimaryColor,
        Color newAccentColor)
    {
        ItemDefinition definition = CreateInstance<ItemDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.Initialize(
            newItemId,
            newDisplayName,
            newCategory,
            newMaxStack,
            newBuyPrice,
            newSellPrice,
            newRestoreHP,
            newRestoreMP,
            newPrimaryColor,
            newAccentColor);
        return definition;
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        itemId = NormalizeId(itemId);
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Item" : displayName.Trim();
        maxStack = category == ItemCategory.Equipment ? 1 : Mathf.Max(1, maxStack);
        buyPrice = Mathf.Max(0, buyPrice);
        sellPrice = Mathf.Max(0, sellPrice);
        restoreHP = Mathf.Max(0, restoreHP);
        restoreMP = Mathf.Max(0, restoreMP);
        equipmentSlot = category == ItemCategory.Equipment ? equipmentSlot : EquipmentSlotType.None;
        equipmentStatBonuses ??= new ItemStatModifierData();
        equipmentAppearance ??= new ItemEquipmentAppearanceData();
        equipmentAppearance.Sanitize();
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
