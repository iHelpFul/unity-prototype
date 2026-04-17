using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcQuestPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image questGiverPortraitImage;

    [SerializeField] private Transform questSelectionRoot;
    [SerializeField] private Button questOptionButtonTemplate;
    [SerializeField] private TextMeshProUGUI questSelectionEmptyText;

    private enum QuestDialogueMode
    {
        Selection,
        Intro,
        InProgress,
        Completion
    }

    private enum QuestPanelAction
    {
        None,
        Ok,
        Accept,
        Complete
    }

    private NpcQuestPromptSnapshot activeSnapshot;
    private NpcQuestPromptEntry activeQuest;
    private QuestDialogueMode dialogueMode;
    private QuestPanelAction actionType;
    private static readonly IReadOnlyList<string> EmptyPages = new string[0];
    private int currentPageIndex;
    private List<Button> spawnedOptionButtons = new List<Button>();

    private void Start()
    {
        SetPanelVisible(false);
        RefreshView();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcQuestOpenedEvent>(OnQuestOpened);
        EventBus.Subscribe<NpcQuestClosedEvent>(OnQuestClosed);
        EventBus.Subscribe<NpcQuestActionResultEvent>(OnQuestActionResult);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcQuestOpenedEvent>(OnQuestOpened);
        EventBus.Unsubscribe<NpcQuestClosedEvent>(OnQuestClosed);
        EventBus.Unsubscribe<NpcQuestActionResultEvent>(OnQuestActionResult);

        ClearQuestButtons();
    }

    public void RequestPrevPage()
    {
        if (dialogueMode == QuestDialogueMode.Selection)
            return;

        if (currentPageIndex <= 0)
            return;

        currentPageIndex--;
        RefreshView();
    }

    public void RequestNextPage()
    {
        if (dialogueMode == QuestDialogueMode.Selection)
            return;

        IReadOnlyList<string> pages = GetCurrentPages();
        if (pages.Count == 0 || currentPageIndex >= pages.Count - 1)
            return;

        currentPageIndex++;
        RefreshView();
    }

    public void RequestAction()
    {
        if (activeSnapshot == null || activeQuest == null)
        {
            ClosePanel();
            return;
        }

        if (actionType == QuestPanelAction.Ok)
        {
            ClosePanel();
            return;
        }

        if (actionType == QuestPanelAction.Accept)
        {
            EventBus.Publish(new NpcQuestAcceptRequestEvent
            {
                Requester = activeSnapshot.Player,
                CharacterId = activeSnapshot.CharacterId,
                NpcId = activeSnapshot.NpcId,
                QuestId = activeQuest.QuestId
            });
            return;
        }

        if (actionType == QuestPanelAction.Complete)
        {
            EventBus.Publish(new NpcQuestCompleteRequestEvent
            {
                Requester = activeSnapshot.Player,
                CharacterId = activeSnapshot.CharacterId,
                NpcId = activeSnapshot.NpcId,
                QuestId = activeQuest.QuestId
            });
        }
    }

    public void ClosePanel()
    {
        if (activeSnapshot == null || string.IsNullOrWhiteSpace(activeSnapshot.CharacterId))
            return;

        EventBus.Publish(new NpcQuestCloseRequestEvent
        {
            Requester = activeSnapshot.Player,
            CharacterId = activeSnapshot.CharacterId,
            NpcId = activeSnapshot.NpcId
        });
    }

    private void OnQuestOpened(NpcQuestOpenedEvent e)
    {
        activeSnapshot = e.Snapshot;
        SetStatus(string.Empty);

        if (activeSnapshot == null)
        {
            CloseAndClearState();
            return;
        }

        SetPanelVisible(true);

        IReadOnlyList<NpcQuestPromptEntry> quests = activeSnapshot.Quests;
        if (quests == null || quests.Count == 0)
        {
            EnterSelectionMode();
            RefreshQuestGiverPortrait();
            return;
        }

        if (activeSnapshot.HasMultipleQuests)
        {
            EnterSelectionMode();
            RefreshQuestGiverPortrait();
            return;
        }

        EnterQuest(quests[0], ResolveQuestMode(quests[0]));
    }

    private void OnQuestClosed(NpcQuestClosedEvent e)
    {
        if (!MatchesActiveQuest(e.Player, e.CharacterId, e.NpcId))
            return;

        CloseAndClearState();
    }

    private void OnQuestActionResult(NpcQuestActionResultEvent e)
    {
        if (!MatchesActiveQuest(e.Player, e.CharacterId, e.NpcId))
            return;

        if (!string.IsNullOrWhiteSpace(e.QuestId)
            && activeQuest != null
            && activeQuest.QuestId != e.QuestId)
        {
            NpcQuestPromptEntry matchingQuest = FindQuestById(e.QuestId);
            if (matchingQuest != null)
                activeQuest = matchingQuest;
        }

        SetStatus(e.Message);

        if (!e.IsSuccess)
            return;

        if (e.ActionType == NpcQuestActionType.Accept)
        {
            if (activeQuest != null && !activeQuest.IsNarrativeOnly)
                activeQuest.UpdateProgressStatus(PlayerQuestProgressStatus.Accepted);

            TryEnterQuestModeAfterAccept(activeQuest);
            if (activeQuest != null && !activeQuest.SupportsCompletion && !activeQuest.SupportsInProgress)
                actionType = QuestPanelAction.Ok;
            RefreshView();
            return;
        }

        if (e.ActionType == NpcQuestActionType.Complete)
        {
            if (activeQuest != null)
                activeQuest.UpdateProgressStatus(PlayerQuestProgressStatus.Completed);

            if (activeQuest != null && activeQuest.SupportsCompletion)
            {
                dialogueMode = QuestDialogueMode.Completion;
                currentPageIndex = 0;
            }

            actionType = QuestPanelAction.Ok;
            RefreshView();
        }
    }

    private void EnterSelectionMode()
    {
        dialogueMode = QuestDialogueMode.Selection;
        activeQuest = null;
        currentPageIndex = 0;
        RefreshSelectionButtons();
        if (titleText != null)
            titleText.text = "Available Dialogues";
        SetActionAndNavigationButtons();
    }

    private void EnterQuest(NpcQuestPromptEntry quest, QuestDialogueMode mode)
    {
        if (quest == null)
            return;

        activeQuest = quest;
        dialogueMode = ResolveQuestMode(mode, quest, false);
        currentPageIndex = 0;
        RefreshQuestGiverPortrait(quest.StarterNpcPortrait);

        if (titleText != null)
            titleText.text = quest.QuestTitle;

        ConfigureActionForMode();
        ClearQuestButtons();
        RefreshView();
    }

    private void TryEnterQuestModeAfterAccept(NpcQuestPromptEntry quest)
    {
        if (quest == null)
            return;

        dialogueMode = ResolveQuestMode(QuestDialogueMode.InProgress, quest, true);
        if (dialogueMode == QuestDialogueMode.Selection)
            dialogueMode = QuestDialogueMode.Intro;

        currentPageIndex = 0;
        ConfigureActionForMode();
        ClearQuestButtons();
        RefreshView();
    }

    private QuestDialogueMode ResolveQuestMode(NpcQuestPromptEntry quest)
    {
        return ResolveQuestMode(QuestDialogueMode.Intro, quest, false);
    }

    private QuestDialogueMode ResolveQuestMode(
        QuestDialogueMode fallbackMode,
        NpcQuestPromptEntry quest,
        bool forceAcceptedMode)
    {
        if (quest == null)
            return QuestDialogueMode.Intro;

        bool isAccepted = forceAcceptedMode || quest.ProgressStatus == PlayerQuestProgressStatus.Accepted;
        if (!isAccepted)
            return fallbackMode;

        if (quest.SupportsInProgress)
            return QuestDialogueMode.InProgress;

        return QuestDialogueMode.InProgress;
    }

    private void RefreshSelectionButtons()
    {
        ClearQuestButtons();

        if (questSelectionRoot == null || questOptionButtonTemplate == null || activeSnapshot == null)
            return;

        IReadOnlyList<NpcQuestPromptEntry> quests = activeSnapshot.Quests;
        if (quests == null || quests.Count == 0)
        {
            SetSelectionMessage("No available conversations.");
            return;
        }

        SetSelectionMessage(string.Empty);

        for (int index = 0; index < quests.Count; index++)
        {
            NpcQuestPromptEntry quest = quests[index];
            if (quest == null)
                continue;

            Button button = Instantiate(questOptionButtonTemplate, questSelectionRoot);
            button.gameObject.SetActive(true);
            spawnedOptionButtons.Add(button);
            ConfigureOptionButton(button, quest);
        }
    }

    private void ConfigureOptionButton(Button button, NpcQuestPromptEntry quest)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnQuestOptionSelected(quest));

        TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonLabel != null)
            buttonLabel.text = BuildQuestLabel(quest);
    }

    private void OnQuestOptionSelected(NpcQuestPromptEntry quest)
    {
        if (quest == null || activeSnapshot == null)
            return;

        EnterQuest(quest, ResolveQuestMode(quest));
        RefreshView();
    }

    private void RefreshView()
    {
        if (dialogueMode == QuestDialogueMode.Selection)
        {
            RefreshQuestGiverPortrait();
            if (npcNameText != null)
                npcNameText.text = activeSnapshot != null ? activeSnapshot.NpcName : "NPC";

            if (titleText != null)
                titleText.text = "Available Dialogues";

            if (bodyText != null)
                bodyText.text = "Select one option";

            if (pageIndicatorText != null)
                pageIndicatorText.text = string.Empty;

            SetActionAndNavigationButtons();
            return;
        }

        if (activeQuest == null || activeSnapshot == null)
        {
            RefreshQuestGiverPortrait();
            SetPanelVisible(false);
            return;
        }

        RefreshQuestGiverPortrait(activeQuest.StarterNpcPortrait);

        if (npcNameText != null)
            npcNameText.text = activeSnapshot.NpcName;

        if (titleText != null)
            titleText.text = activeQuest.QuestTitle;

        if (bodyText != null)
            bodyText.text = GetCurrentPageText();

        if (pageIndicatorText != null)
            pageIndicatorText.text = SetupPageIndicator();

        SetActionAndNavigationButtons();
    }

    private string SetupPageIndicator()
    {
        IReadOnlyList<string> pages = GetCurrentPages();
        if (pages == null || pages.Count <= 1)
            return string.Empty;

        return $"{currentPageIndex + 1}/{pages.Count}";
    }

    private void SetActionAndNavigationButtons()
    {
        ConfigurePageButtons();
        ConfigureActionButton();
        ConfigureCloseButton();
        ShowSelectionStateButtons(dialogueMode == QuestDialogueMode.Selection);
    }

    private void ConfigurePageButtons()
    {
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(dialogueMode != QuestDialogueMode.Selection);
            prevButton.interactable = dialogueMode != QuestDialogueMode.Selection && currentPageIndex > 0;
        }

        if (nextButton != null)
        {
            IReadOnlyList<string> pages = GetCurrentPages();
            bool hasManyPages = pages != null && pages.Count > 1;
            nextButton.gameObject.SetActive(dialogueMode != QuestDialogueMode.Selection);
            nextButton.interactable = dialogueMode != QuestDialogueMode.Selection && hasManyPages && currentPageIndex < pages.Count - 1;
        }
    }

    private void ConfigureActionButton()
    {
        if (actionButton == null)
            return;

        actionButton.gameObject.SetActive(
            dialogueMode != QuestDialogueMode.Selection
            && IsLastPage()
            && actionType != QuestPanelAction.None);

        if (actionButtonText != null)
        {
            string label = actionType switch
            {
                QuestPanelAction.Accept => "Accept",
                QuestPanelAction.Complete => "Complete",
                _ => "OK"
            };
            actionButtonText.text = label;
        }
    }

    private void ConfigureCloseButton()
    {
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
    }

    private void ShowSelectionStateButtons(bool isSelectionMode)
    {
        if (prevButton != null)
            prevButton.gameObject.SetActive(!isSelectionMode);

        if (nextButton != null)
            nextButton.gameObject.SetActive(!isSelectionMode);

        if (isSelectionMode && actionButton != null)
            actionButton.gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
    }

    private void ConfigureActionForMode()
    {
        if (activeQuest == null)
        {
            actionType = QuestPanelAction.None;
            return;
        }

        if (dialogueMode == QuestDialogueMode.Completion
            || dialogueMode == QuestDialogueMode.InProgress
            || (dialogueMode == QuestDialogueMode.Intro && activeQuest.ProgressStatus == PlayerQuestProgressStatus.Accepted))
            actionType = QuestPanelAction.Complete;
        else if (activeQuest.IsNarrativeOnly)
            actionType = QuestPanelAction.Ok;
        else
            actionType = QuestPanelAction.Accept;
    }

    private bool IsLastPage()
    {
        IReadOnlyList<string> pages = GetCurrentPages();
        if (pages == null || pages.Count == 0)
            return true;

        return currentPageIndex >= pages.Count - 1;
    }

    private string GetCurrentPageText()
    {
        IReadOnlyList<string> pages = GetCurrentPages();
        if (pages == null || pages.Count == 0)
        {
            if (dialogueMode == QuestDialogueMode.Completion)
                return "No completion text available.";
            if (dialogueMode == QuestDialogueMode.InProgress)
                return "No in-progress text available.";

            return activeQuest != null && activeQuest.IsNarrativeOnly
                ? "No dialogue text available."
                : "No quest details available.";
        }

        if (currentPageIndex < 0)
            currentPageIndex = 0;

        if (currentPageIndex >= pages.Count)
            currentPageIndex = pages.Count - 1;

        return pages[currentPageIndex];
    }

    private IReadOnlyList<string> GetCurrentPages()
    {
        if (activeQuest == null)
            return EmptyPages;

        if (dialogueMode == QuestDialogueMode.Completion)
            return activeQuest.CompletionPages;
        if (dialogueMode == QuestDialogueMode.InProgress)
            return activeQuest.InProgressPages;

        return activeQuest.IntroPages;
    }

    private string BuildQuestLabel(NpcQuestPromptEntry quest)
    {
        if (quest == null)
            return string.Empty;

        if (quest.MinimumPlayerLevel <= 1)
            return quest.QuestTitle;

        return $"{quest.QuestTitle} (Lvl {quest.MinimumPlayerLevel}+)";
    }

    private NpcQuestPromptEntry FindQuestById(string questId)
    {
        if (activeSnapshot == null || activeSnapshot.Quests == null)
            return null;

        string normalizedQuestId = string.IsNullOrWhiteSpace(questId) ? string.Empty : questId.Trim();
        for (int index = 0; index < activeSnapshot.Quests.Count; index++)
        {
            NpcQuestPromptEntry candidate = activeSnapshot.Quests[index];
            if (candidate == null)
                continue;

            if (candidate.QuestId == normalizedQuestId)
                return candidate;
        }

        return null;
    }

    private void SetSelectionMessage(string message)
    {
        if (questSelectionEmptyText != null)
            questSelectionEmptyText.text = message;

        if (string.IsNullOrWhiteSpace(message) && questSelectionEmptyText != null)
            questSelectionEmptyText.gameObject.SetActive(false);
        else if (questSelectionEmptyText != null)
            questSelectionEmptyText.gameObject.SetActive(true);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }

    private void RefreshQuestGiverPortrait(Sprite portrait = null)
    {
        if (questGiverPortraitImage == null)
            return;

        if (portrait == null)
        {
            questGiverPortraitImage.sprite = null;
            questGiverPortraitImage.enabled = false;
        }
        else
        {
            questGiverPortraitImage.sprite = portrait;
            questGiverPortraitImage.enabled = true;
        }
    }

    private bool MatchesActiveQuest(PlayerCharacter player, string characterId, string npcId)
    {
        if (activeSnapshot == null)
            return false;

        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(
            activeSnapshot.Player,
            activeSnapshot.CharacterId,
            player,
            characterId))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(npcId) || npcId == activeSnapshot.NpcId;
    }

    private void CloseAndClearState()
    {
        activeSnapshot = null;
        activeQuest = null;
        dialogueMode = QuestDialogueMode.Selection;
        currentPageIndex = 0;
        actionType = QuestPanelAction.None;
        ClearQuestButtons();
        SetStatus(string.Empty);
        SetPanelVisible(false);
        RefreshView();
    }

    private void ClearQuestButtons()
    {
        for (int index = 0; index < spawnedOptionButtons.Count; index++)
        {
            Button button = spawnedOptionButtons[index];
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            Destroy(button.gameObject);
        }

        spawnedOptionButtons.Clear();
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
    }
}
