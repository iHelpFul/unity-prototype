using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryEntryButtonController : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI metaText;
    [SerializeField] private Image background;
    [SerializeField] private Color selectedColor = new Color(0.28f, 0.42f, 0.7f, 0.95f);
    [SerializeField] private Color normalColor = new Color(0.12f, 0.12f, 0.14f, 0.85f);

    private string entryId;
    private System.Action<string> clickHandler;

    public void Bind(
        InventoryEntry entry,
        ItemDefinition definition,
        bool isSelected,
        bool isEquipped,
        System.Action<string> onClicked)
    {
        entryId = entry != null ? entry.EntryId : string.Empty;
        clickHandler = onClicked;

        if (nameText != null)
            nameText.text = definition != null ? definition.DisplayName : (entry != null ? entry.ItemId : "-");

        if (metaText != null)
            metaText.text = BuildMetaText(entry, definition, isEquipped);

        if (background != null)
            background.color = isSelected ? selectedColor : normalColor;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        clickHandler?.Invoke(entryId);
    }

    private static string BuildMetaText(InventoryEntry entry, ItemDefinition definition, bool isEquipped)
    {
        if (entry == null)
            return string.Empty;

        string category = definition != null ? definition.Category.ToString() : "Item";
        string countText = entry.Count > 1 ? $"x{entry.Count}" : "x1";
        string equippedText = isEquipped ? "  |  Equipped" : string.Empty;
        return $"{category}  |  {countText}{equippedText}";
    }
}
