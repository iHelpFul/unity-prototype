using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryPanelController : MonoBehaviour
{
    [System.Serializable]
    private class EquipmentSlotView
    {
        public EquipmentSlotType Slot;
        public string DisplayName;
        public EquipmentSlotButtonController Controller;
    }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private PlayerCharacter trackedPlayer;
    [Header("Summary")]
    [SerializeField] private TextMeshProUGUI mesosText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI statusText;
    [Header("Inventory List")]
    [SerializeField] private Transform inventoryListRoot;
    [SerializeField] private InventoryEntryButtonController inventoryEntryTemplate;
    [Header("Equipment Slots")]
    [SerializeField] private EquipmentSlotView[] equipmentSlots;
    [Header("Selection")]
    [SerializeField] private TextMeshProUGUI selectedNameText;
    [SerializeField] private TextMeshProUGUI selectedBodyText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonLabel;

    private readonly List<InventoryEntryButtonController> spawnedEntryViews = new List<InventoryEntryButtonController>();

    private string selectedEntryId = string.Empty;
    private bool isVisible;

    private void Start()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(ExecuteSelectedAction);
        }

        SetVisible(false);
        RefreshView();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InventoryTogglePressedEvent>(OnInventoryTogglePressed);
        EventBus.Subscribe<PlayerInventoryChangedEvent>(OnInventoryChanged);
        EventBus.Subscribe<PlayerEquipmentChangedEvent>(OnEquipmentChanged);
        EventBus.Subscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        EventBus.Subscribe<PlayerManaChangedEvent>(OnManaChanged);
        EventBus.Subscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InventoryTogglePressedEvent>(OnInventoryTogglePressed);
        EventBus.Unsubscribe<PlayerInventoryChangedEvent>(OnInventoryChanged);
        EventBus.Unsubscribe<PlayerEquipmentChangedEvent>(OnEquipmentChanged);
        EventBus.Unsubscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        EventBus.Unsubscribe<PlayerManaChangedEvent>(OnManaChanged);
        EventBus.Unsubscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
    }

    public void TogglePanel()
    {
        SetVisible(!isVisible);
    }

    public void ShowPanel()
    {
        SetVisible(true);
    }

    public void HidePanel()
    {
        SetVisible(false);
    }

    public void ExecuteSelectedAction()
    {
        ResolveRuntimeContext();

        if (!isVisible || bootstrap == null || trackedPlayer == null || string.IsNullOrWhiteSpace(selectedEntryId))
            return;

        InventoryEntry entry = bootstrap.GetInventoryEntry(selectedEntryId);
        if (entry == null || !ItemDatabase.TryGetDefinition(entry.ItemId, out ItemDefinition definition))
        {
            SetStatus("That item is no longer available.");
            RefreshView();
            return;
        }

        if (definition.IsEquipment)
        {
            if (IsEntryEquipped(entry.EntryId))
            {
                if (!bootstrap.TryUnequipItem(trackedPlayer, definition.EquipmentSlot))
                    SetStatus("That item could not be unequipped.");
                else
                    SetStatus($"{definition.DisplayName} unequipped.");
            }
            else
            {
                if (!bootstrap.TryEquipInventoryEntry(trackedPlayer, entry.EntryId))
                    SetStatus("That item could not be equipped.");
                else
                    SetStatus($"{definition.DisplayName} equipped.");
            }

            RefreshView();
            return;
        }

        if (definition.Category == ItemCategory.Consumable)
        {
            if (!bootstrap.TryUseInventoryEntry(trackedPlayer, entry.EntryId))
                SetStatus("That consumable could not be used right now.");
            else
                SetStatus($"{definition.DisplayName} used.");

            RefreshView();
            return;
        }

        SetStatus("This item has no direct action yet.");
        RefreshView();
    }

    private void OnInventoryChanged(PlayerInventoryChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnInventoryTogglePressed(InventoryTogglePressedEvent e)
    {
        if (!IsTrackedPlayer(e.Player, e.CharacterId))
            return;

        TogglePanel();
    }

    private void OnEquipmentChanged(PlayerEquipmentChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnCurrencyChanged(PlayerCurrencyChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnHealthChanged(PlayerHealthChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnManaChanged(PlayerManaChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnMapTransitionCompleted(MapTransitionCompletedEvent e)
    {
        ResolveRuntimeContext();

        if (e.Player != null && e.Player.IsLocalPlayer)
            trackedPlayer = e.Player;
        else if (!PlayerRuntimeIdentityUtility.MatchesTrackedCharacter(
                     bootstrap,
                     trackedPlayer,
                     e.Player,
                     e.CharacterId))
            return;

        RefreshView();
    }

    private void RefreshView()
    {
        ResolveRuntimeContext();
        SetVisibleInternal(isVisible);

        if (bootstrap == null || bootstrap.PlayerData == null)
        {
            ClearInventoryList();
            RefreshEquipmentSlots();
            RefreshSelection();
            return;
        }

        RefreshSummary();
        RefreshInventoryList();
        RefreshEquipmentSlots();
        RefreshSelection();
    }

    private void RefreshSummary()
    {
        if (mesosText != null)
            mesosText.text = $"Mesos: {bootstrap.PlayerData.Mesos}";

        if (statsText == null)
            return;

        ItemStatModifierData bonuses = bootstrap.GetEquipmentStatBonuses();
        StringBuilder builder = new StringBuilder();
        builder.Append("STR +").Append(bonuses.Strength);
        builder.Append("  |  DEX +").Append(bonuses.Dexterity);
        builder.Append("  |  ATK +").Append(bonuses.WeaponAttack);
        builder.Append("  |  HP +").Append(bonuses.MaxHP);
        builder.Append("  |  MP +").Append(bonuses.MaxMP);
        statsText.text = builder.ToString();
    }

    private void RefreshInventoryList()
    {
        IReadOnlyList<InventoryEntry> entries = bootstrap.GetInventoryEntries();
        EnsureSelectedEntry(entries);
        ClearInventoryList();

        if (entries == null || inventoryListRoot == null || inventoryEntryTemplate == null)
            return;

        for (int index = 0; index < entries.Count; index++)
        {
            InventoryEntry entry = entries[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                continue;

            ItemDefinition definition = ItemDatabase.GetDefinition(entry.ItemId);
            InventoryEntryButtonController row = Instantiate(inventoryEntryTemplate, inventoryListRoot);
            row.gameObject.SetActive(true);
            row.Bind(
                entry,
                definition,
                entry.EntryId == selectedEntryId,
                IsEntryEquipped(entry.EntryId),
                OnEntrySelected);
            spawnedEntryViews.Add(row);
        }
    }

    private void RefreshEquipmentSlots()
    {
        if (equipmentSlots == null)
            return;

        for (int index = 0; index < equipmentSlots.Length; index++)
        {
            EquipmentSlotView view = equipmentSlots[index];
            if (view == null || view.Controller == null)
                continue;

            InventoryEntry equippedEntry = bootstrap != null ? bootstrap.GetEquippedEntry(view.Slot) : null;
            ItemDefinition definition = equippedEntry != null ? ItemDatabase.GetDefinition(equippedEntry.ItemId) : null;
            view.Controller.Bind(view.Slot, view.DisplayName, equippedEntry, definition, OnEquipmentSlotClicked);
        }
    }

    private void RefreshSelection()
    {
        InventoryEntry entry = bootstrap != null ? bootstrap.GetInventoryEntry(selectedEntryId) : null;
        ItemDefinition definition = entry != null ? ItemDatabase.GetDefinition(entry.ItemId) : null;

        if (selectedNameText != null)
            selectedNameText.text = definition != null ? definition.DisplayName : "No Item Selected";

        if (selectedBodyText != null)
            selectedBodyText.text = BuildSelectionBody(entry, definition);

        if (actionButton != null)
        {
            bool canAct = entry != null && definition != null && (definition.IsEquipment || definition.Category == ItemCategory.Consumable);
            actionButton.interactable = canAct;
        }

        if (actionButtonLabel != null)
            actionButtonLabel.text = BuildActionLabel(entry, definition);
    }

    private string BuildSelectionBody(InventoryEntry entry, ItemDefinition definition)
    {
        if (entry == null || definition == null)
            return "Select an inventory entry to inspect it.";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Item ID: {definition.ItemId}");
        builder.AppendLine($"Category: {definition.Category}");

        if (definition.IsEquipment)
        {
            builder.AppendLine($"Slot: {definition.EquipmentSlot}");
            builder.AppendLine($"Equipped: {(IsEntryEquipped(entry.EntryId) ? "Yes" : "No")}");
            AppendStatLine(builder, "STR", definition.EquipmentStatBonuses != null ? definition.EquipmentStatBonuses.Strength : 0);
            AppendStatLine(builder, "DEX", definition.EquipmentStatBonuses != null ? definition.EquipmentStatBonuses.Dexterity : 0);
            AppendStatLine(builder, "ATK", definition.EquipmentStatBonuses != null ? definition.EquipmentStatBonuses.WeaponAttack : 0);
            AppendStatLine(builder, "Max HP", definition.EquipmentStatBonuses != null ? definition.EquipmentStatBonuses.MaxHP : 0);
            AppendStatLine(builder, "Max MP", definition.EquipmentStatBonuses != null ? definition.EquipmentStatBonuses.MaxMP : 0);
        }
        else if (definition.Category == ItemCategory.Consumable)
        {
            builder.AppendLine($"Count: {entry.Count}");
            builder.AppendLine($"Restores HP: {definition.RestoreHP}");
            builder.AppendLine($"Restores MP: {definition.RestoreMP}");
        }
        else
        {
            builder.AppendLine($"Count: {entry.Count}");
            builder.AppendLine("No direct use action yet.");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendStatLine(StringBuilder builder, string label, int value)
    {
        if (value == 0)
            return;

        builder.AppendLine($"{label}: +{value}");
    }

    private string BuildActionLabel(InventoryEntry entry, ItemDefinition definition)
    {
        if (entry == null || definition == null)
            return "No Action";

        if (definition.IsEquipment)
            return IsEntryEquipped(entry.EntryId) ? "Unequip" : "Equip";

        if (definition.Category == ItemCategory.Consumable)
            return "Use";

        return "No Action";
    }

    private void OnEntrySelected(string entryId)
    {
        selectedEntryId = string.IsNullOrWhiteSpace(entryId) ? string.Empty : entryId.Trim();
        RefreshView();
    }

    private void OnEquipmentSlotClicked(EquipmentSlotType slot)
    {
        ResolveRuntimeContext();
        if (bootstrap == null || trackedPlayer == null)
            return;

        if (!bootstrap.TryUnequipItem(trackedPlayer, slot))
            SetStatus("That slot could not be unequipped.");
        else
            SetStatus($"{slot} unequipped.");

        RefreshView();
    }

    private void EnsureSelectedEntry(IReadOnlyList<InventoryEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            selectedEntryId = string.Empty;
            return;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            InventoryEntry entry = entries[index];
            if (entry != null && entry.EntryId == selectedEntryId)
                return;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            InventoryEntry entry = entries[index];
            if (entry != null)
            {
                selectedEntryId = entry.EntryId;
                return;
            }
        }

        selectedEntryId = string.Empty;
    }

    private bool IsEntryEquipped(string entryId)
    {
        if (bootstrap == null || string.IsNullOrWhiteSpace(entryId))
            return false;

        IReadOnlyList<EquippedItemEntry> equippedItems = bootstrap.GetEquippedItems();
        if (equippedItems == null)
            return false;

        string normalizedEntryId = entryId.Trim();
        for (int index = 0; index < equippedItems.Count; index++)
        {
            EquippedItemEntry equippedItem = equippedItems[index];
            if (equippedItem != null && equippedItem.InventoryEntryId == normalizedEntryId)
                return true;
        }

        return false;
    }

    private void ClearInventoryList()
    {
        for (int index = 0; index < spawnedEntryViews.Count; index++)
        {
            InventoryEntryButtonController view = spawnedEntryViews[index];
            if (view != null)
                Destroy(view.gameObject);
        }

        spawnedEntryViews.Clear();
    }

    private void ResolveRuntimeContext()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);

        if (trackedPlayer != null && trackedPlayer.IsLocalPlayer && trackedPlayer.gameObject.scene.IsValid())
            return;

        trackedPlayer = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
    }

    private bool IsTrackedPlayer(PlayerCharacter player, string characterId)
    {
        ResolveRuntimeContext();
        return PlayerRuntimeIdentityUtility.MatchesTrackedCharacter(
            bootstrap,
            trackedPlayer,
            player,
            characterId);
    }

    private void SetVisible(bool shouldShow)
    {
        isVisible = shouldShow;
        SetVisibleInternal(shouldShow);

        if (shouldShow)
            RefreshView();
    }

    private void SetVisibleInternal(bool shouldShow)
    {
        if (panelRoot != null)
            panelRoot.SetActive(shouldShow);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }
}
