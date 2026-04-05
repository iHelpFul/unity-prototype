using System.Text;
using TMPro;
using UnityEngine;

public class CharacterSelectionPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TextMeshProUGUI accountNameText;
    [SerializeField] private TextMeshProUGUI selectedCharacterText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI[] slotTexts;
    [SerializeField] private CharacterCreationPanelController creationPanel;

    private CharacterSelectionSnapshot activeSnapshot;

    private void Start()
    {
        SetPanelVisible(true);
        SetForegroundVisible(true);
        RefreshView();
        RefreshState();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CharacterSelectionStateChangedEvent>(OnSelectionStateChanged);
        EventBus.Subscribe<CharacterCreationPanelVisibilityChangedEvent>(OnCreationPanelVisibilityChanged);
        EventBus.Subscribe<CharacterFlowResultEvent>(OnFlowResult);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSelectionStateChangedEvent>(OnSelectionStateChanged);
        EventBus.Unsubscribe<CharacterCreationPanelVisibilityChangedEvent>(OnCreationPanelVisibilityChanged);
        EventBus.Unsubscribe<CharacterFlowResultEvent>(OnFlowResult);
    }

    public void RefreshState()
    {
        EventBus.Publish(new CharacterSelectionRefreshRequestEvent());
    }

    public void SelectSlot(int slotIndex)
    {
        EventBus.Publish(new CharacterSlotSelectRequestEvent
        {
            SlotIndex = slotIndex
        });
    }

    public void EnterWorld()
    {
        EventBus.Publish(new CharacterEnterWorldRequestEvent());
    }

    public void OpenCreateForSlot(int slotIndex)
    {
        SetForegroundVisible(false);

        if (creationPanel != null)
            creationPanel.OpenForSlot(slotIndex);
    }

    private void OnSelectionStateChanged(CharacterSelectionStateChangedEvent e)
    {
        activeSnapshot = e.Snapshot;
        RefreshView();
    }

    private void OnFlowResult(CharacterFlowResultEvent e)
    {
        if (statusText != null && !string.IsNullOrWhiteSpace(e.Message))
            statusText.text = e.Message;
    }

    private void OnCreationPanelVisibilityChanged(CharacterCreationPanelVisibilityChangedEvent e)
    {
        SetForegroundVisible(!e.IsVisible);
    }

    private void RefreshView()
    {
        if (accountNameText != null)
            accountNameText.text = activeSnapshot != null
                ? $"Account: {activeSnapshot.Username}"
                : "Account: LocalAccount";

        if (selectedCharacterText != null)
            selectedCharacterText.text = BuildSelectedCharacterText();

        if (slotTexts != null)
        {
            for (int index = 0; index < slotTexts.Length; index++)
            {
                if (slotTexts[index] == null)
                    continue;

                CharacterSlotSnapshot slot = FindSlot(index);
                slotTexts[index].text = BuildSlotText(index, slot);
            }
        }

        if (statusText != null && activeSnapshot == null)
            statusText.text = string.Empty;
    }

    private string BuildSelectedCharacterText()
    {
        if (activeSnapshot == null)
            return "Selected: None";

        CharacterSlotSnapshot slot = FindSlot(activeSnapshot.SelectedSlotIndex);
        if (slot == null || !slot.IsOccupied)
            return activeSnapshot.IsEnteringWorld ? "Entering world..." : "Selected: None";

        StringBuilder builder = new StringBuilder();
        builder.Append("Selected: ");
        builder.Append(slot.Nickname);
        builder.Append("  |  Lv. ");
        builder.Append(slot.Level);
        builder.Append(" ");
        builder.Append(slot.JobDisplayName);

        if (activeSnapshot.IsEnteringWorld)
            builder.Append("  |  Entering...");

        return builder.ToString();
    }

    private string BuildSlotText(int slotIndex, CharacterSlotSnapshot slot)
    {
        int displayIndex = slotIndex + 1;

        if (slot == null || !slot.IsOccupied)
            return $"Slot {displayIndex}\nEmpty";

        StringBuilder builder = new StringBuilder();
        builder.Append("Slot ");
        builder.Append(displayIndex);

        if (slot.IsSelected)
            builder.Append("  [Selected]");

        builder.AppendLine();
        builder.Append(slot.Nickname);
        builder.Append("  |  Lv. ");
        builder.Append(slot.Level);
        builder.Append(" ");
        builder.AppendLine(slot.JobDisplayName);
        builder.Append("Map: ");
        builder.Append(string.IsNullOrWhiteSpace(slot.CurrentMapId) ? "-" : slot.CurrentMapId);
        return builder.ToString();
    }

    private CharacterSlotSnapshot FindSlot(int slotIndex)
    {
        if (activeSnapshot?.Slots == null)
            return null;

        for (int index = 0; index < activeSnapshot.Slots.Count; index++)
        {
            CharacterSlotSnapshot slot = activeSnapshot.Slots[index];
            if (slot != null && slot.SlotIndex == slotIndex)
                return slot;
        }

        return null;
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
    }

    private void SetForegroundVisible(bool isVisible)
    {
        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.alpha = isVisible ? 1f : 0f;
        panelCanvasGroup.interactable = isVisible;
        panelCanvasGroup.blocksRaycasts = isVisible;
    }
}
