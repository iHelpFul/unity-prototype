using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcQuestLogPanelController : MonoBehaviour
{
    [Header("Runtime Context")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private PlayerCharacter trackedPlayer;

    [Header("Layout")]
    [SerializeField] private Button availableTabButton;
    [SerializeField] private Button inProgressTabButton;
    [SerializeField] private Button completedTabButton;
    [SerializeField] private TextMeshProUGUI tabHintText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Quest List")]
    [SerializeField] private Transform questListRoot;
    [SerializeField] private Button questListEntryButtonTemplate;
    [SerializeField] private TextMeshProUGUI emptyListText;

    [Header("Quest Detail")]
    [SerializeField] private NpcQuestLogDetailPanelController detailPanel;

    private readonly List<Button> spawnedEntryButtons = new List<Button>();

    private NpcQuestLogSnapshot activeSnapshot;
    private NpcQuestLogTab activeTab = NpcQuestLogTab.Available;
    private NpcQuestLogQuestEntry activeQuest;

    private void Start()
    {
        SetPanelVisible(false);
        RefreshView();
    }

    public void BindRuntimeContext(GameBootstrap sessionBootstrap, PlayerCharacter player)
    {
        bootstrap = sessionBootstrap;

        if (player != null && player.IsLocalPlayer)
            trackedPlayer = player;

        RefreshView();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcQuestLogOpenedEvent>(OnQuestLogOpened);
        EventBus.Subscribe<NpcQuestLogClosedEvent>(OnQuestLogClosed);
        EventBus.Subscribe<NpcQuestLogRefreshEvent>(OnQuestLogRefreshed);
        EventBus.Subscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
        EventBus.Subscribe<QuestLogTogglePressedEvent>(OnQuestLogTogglePressed);
        EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerProgressChanged);
        EventBus.Subscribe<PlayerExpChangedEvent>(OnPlayerProgressChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcQuestLogOpenedEvent>(OnQuestLogOpened);
        EventBus.Unsubscribe<NpcQuestLogClosedEvent>(OnQuestLogClosed);
        EventBus.Unsubscribe<NpcQuestLogRefreshEvent>(OnQuestLogRefreshed);
        EventBus.Unsubscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
        EventBus.Unsubscribe<QuestLogTogglePressedEvent>(OnQuestLogTogglePressed);
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerProgressChanged);
        EventBus.Unsubscribe<PlayerExpChangedEvent>(OnPlayerProgressChanged);

        ClearQuestEntries();
        if (detailPanel != null)
            detailPanel.Hide();
    }

    public void ShowPanel()
    {
        if (trackedPlayer == null)
            return;

        EventBus.Publish(new NpcQuestLogRequestEvent
        {
            Player = trackedPlayer,
            CharacterId = trackedPlayer.CharacterId
        });
    }

    public void HidePanel()
    {
        if (trackedPlayer == null)
            return;

        EventBus.Publish(new NpcQuestLogCloseRequestEvent
        {
            Requester = trackedPlayer,
            CharacterId = trackedPlayer.CharacterId
        });
    }

    public void TogglePanel()
    {
        bool isVisible = panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;
        if (isVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public void RequestAvailableTab()
    {
        SetTab(NpcQuestLogTab.Available);
    }

    public void RequestInProgressTab()
    {
        SetTab(NpcQuestLogTab.InProgress);
    }

    public void RequestCompletedTab()
    {
        SetTab(NpcQuestLogTab.Completed);
    }

    public void RequestQuestDetails(NpcQuestLogQuestEntry quest)
    {
        if (quest == null || detailPanel == null)
            return;

        activeQuest = quest;
        detailPanel.Show(quest);
    }

    private void SetTab(NpcQuestLogTab tab)
    {
        if (activeTab == tab)
            return;

        activeTab = tab;
        activeQuest = null;
        RefreshTabButtons();
        ClearQuestEntries();
        RefreshView();
        RefreshDetailState();
    }

    private void OnPlayerProgressChanged(PlayerExpChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnPlayerProgressChanged(PlayerLevelUpEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnQuestLogTogglePressed(QuestLogTogglePressedEvent e)
    {
        if (!IsTrackedPlayer(e.Player, e.CharacterId))
            return;

        TogglePanel();
    }

    private void OnQuestLogOpened(NpcQuestLogOpenedEvent e)
    {
        if (e.Snapshot == null)
        {
            SetPanelVisible(false);
            return;
        }

        if (!IsTrackedPlayer(e.Snapshot.Player, e.Snapshot.CharacterId))
            return;

        activeSnapshot = e.Snapshot;
        SetPanelVisible(true);
        RefreshView();
    }

    private void OnQuestLogClosed(NpcQuestLogClosedEvent e)
    {
        if (!IsTrackedPlayer(e.Player, e.CharacterId))
            return;

        activeSnapshot = null;
        activeQuest = null;
        RefreshView();
        SetPanelVisible(false);
        if (detailPanel != null)
            detailPanel.Hide();
    }

    private void OnQuestLogRefreshed(NpcQuestLogRefreshEvent e)
    {
        if (e.Snapshot == null || !IsTrackedPlayer(e.Snapshot.Player, e.Snapshot.CharacterId))
            return;

        activeSnapshot = e.Snapshot;
        RefreshView();
    }

    private void OnMapTransitionCompleted(MapTransitionCompletedEvent e)
    {
        if (e.Player != null && e.Player.IsLocalPlayer)
            trackedPlayer = e.Player;

        if (!panelRoot || !panelRoot.activeSelf)
            return;

        RefreshView();
    }

    private void RefreshView()
    {
        SetPanelVisible(activeSnapshot != null);

        if (activeSnapshot == null)
        {
            ClearQuestEntries();
            SetStatus("No quest log data.");
            if (detailPanel != null)
                detailPanel.Hide();
            RefreshTabButtons();
            return;
        }

        if (availableTabButton == null || inProgressTabButton == null || completedTabButton == null)
            RefreshLegacyTabVisuals();

        UpdateTabContent();
        RefreshTabButtons();
        SetStatus(string.Empty);
    }

    private void UpdateTabContent()
    {
        IReadOnlyList<NpcQuestLogQuestEntry> quests = ResolveQuestCollection(activeTab);
        if (quests == null || quests.Count == 0)
        {
            ClearQuestEntries();
            if (emptyListText != null)
                emptyListText.text = BuildEmptyHint(activeTab);
            return;
        }

        ClearQuestEntries();
        if (emptyListText != null)
            emptyListText.text = string.Empty;

        for (int index = 0; index < quests.Count; index++)
        {
            NpcQuestLogQuestEntry quest = quests[index];
            if (quest == null)
                continue;

            Button rowButton = Instantiate(questListEntryButtonTemplate, questListRoot);
            rowButton.gameObject.SetActive(true);
            ConfigureRowButton(rowButton, quest);
            spawnedEntryButtons.Add(rowButton);
        }

        if (activeTab != NpcQuestLogTab.InProgress && detailPanel != null)
            detailPanel.Hide();
    }

    private void ConfigureRowButton(Button rowButton, NpcQuestLogQuestEntry quest)
    {
        if (rowButton == null || quest == null)
            return;

        TextMeshProUGUI rowText = rowButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (rowText != null)
            rowText.text = BuildEntryTitle(quest);

        rowButton.onClick.RemoveAllListeners();
        rowButton.onClick.AddListener(() => OnQuestSelected(quest));
    }

    private void OnQuestSelected(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
            return;

        if (detailPanel == null)
            return;

        RequestQuestDetails(quest);
    }

    private void RefreshTabButtons()
    {
        if (availableTabButton != null)
        {
            bool isAvailable = activeTab == NpcQuestLogTab.Available;
            availableTabButton.interactable = !isAvailable;
        }

        if (inProgressTabButton != null)
        {
            bool isInProgress = activeTab == NpcQuestLogTab.InProgress;
            inProgressTabButton.interactable = !isInProgress;
        }

        if (completedTabButton != null)
        {
            bool isCompleted = activeTab == NpcQuestLogTab.Completed;
            completedTabButton.interactable = !isCompleted;
        }

        if (tabHintText != null)
            tabHintText.text = activeTab switch
            {
                NpcQuestLogTab.Available => "Available quests",
                NpcQuestLogTab.InProgress => "In progress quests",
                _ => "Completed quests"
            };
    }

    private static void RefreshLegacyTabVisuals()
    {
        // Kept for compatibility when specific tab widgets are not assigned.
    }

    private void RefreshDetailState()
    {
        if (activeTab != NpcQuestLogTab.InProgress && detailPanel != null)
            detailPanel.Hide();

        if (detailPanel == null || activeQuest == null)
            return;

        detailPanel.Show(activeQuest);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }

    private static string BuildEmptyHint(NpcQuestLogTab tab)
    {
        return tab switch
        {
            NpcQuestLogTab.Available => "No available quests.",
            NpcQuestLogTab.InProgress => "No active quests.",
            _ => "No completed quests."
        };
    }

    private static string BuildEntryTitle(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
            return "Quest";

        string title = quest.QuestTitle;
        if (quest.MinimumPlayerLevel > 1)
            title += $" (Lvl {quest.MinimumPlayerLevel}+)";

        return title;
    }

    private IReadOnlyList<NpcQuestLogQuestEntry> ResolveQuestCollection(NpcQuestLogTab tab)
    {
        return tab switch
        {
            NpcQuestLogTab.InProgress => activeSnapshot != null ? activeSnapshot.InProgressQuests : null,
            NpcQuestLogTab.Completed => activeSnapshot != null ? activeSnapshot.CompletedQuests : null,
            _ => activeSnapshot != null ? activeSnapshot.AvailableQuests : null
        };
    }

    private void ClearQuestEntries()
    {
        for (int index = 0; index < spawnedEntryButtons.Count; index++)
        {
            Button entryButton = spawnedEntryButtons[index];
            if (entryButton != null)
            {
                entryButton.onClick.RemoveAllListeners();
                Destroy(entryButton.gameObject);
            }
        }

        spawnedEntryButtons.Clear();
    }

    private bool IsTrackedPlayer(PlayerCharacter player, string characterId)
    {
        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            trackedPlayer,
            trackedPlayer != null ? trackedPlayer.CharacterId : string.Empty,
            player,
            characterId);
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);
    }
}
