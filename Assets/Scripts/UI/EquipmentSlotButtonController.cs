using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotButtonController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionLabel;

    private EquipmentSlotType slot;
    private System.Action<EquipmentSlotType> clickHandler;

    public void Bind(
        EquipmentSlotType slotType,
        string slotDisplayName,
        InventoryEntry equippedEntry,
        ItemDefinition equippedDefinition,
        System.Action<EquipmentSlotType> onClicked)
    {
        slot = slotType;
        clickHandler = onClicked;

        if (slotNameText != null)
            slotNameText.text = string.IsNullOrWhiteSpace(slotDisplayName) ? slotType.ToString() : slotDisplayName;

        if (itemNameText != null)
            itemNameText.text = equippedDefinition != null ? equippedDefinition.DisplayName : "Empty";

        if (actionLabel != null)
            actionLabel.text = equippedEntry != null ? "Unequip" : "Empty";

        if (actionButton != null)
        {
            actionButton.interactable = equippedEntry != null;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        clickHandler?.Invoke(slot);
    }
}
