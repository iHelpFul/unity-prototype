using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcQuestLogDetailPanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Vector2 openAnchoredPosition = Vector2.zero;
    [SerializeField] private Vector2 closedAnchoredPosition = new Vector2(420f, 0f);
    [SerializeField] private float slideDuration = 0.2f;
    [SerializeField] private Button closeButton;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image npcPortraitImage;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI questSummaryText;
    [SerializeField] private TextMeshProUGUI completionInstructionText;
    [SerializeField] private Transform objectiveListRoot;
    [SerializeField] private NpcQuestObjectiveProgressRowView objectiveRowTemplate;
    [SerializeField] private TextMeshProUGUI objectiveEmptyText;
    [SerializeField] private TextMeshProUGUI npcProgressText;
    [SerializeField] private Transform rewardsListRoot;
    [SerializeField] private NpcQuestRewardEntryView rewardEntryTemplate;
    [SerializeField] private TextMeshProUGUI rewardsEmptyText;
    [SerializeField] private TextMeshProUGUI statusText;

    private Coroutine slideCoroutine;
    private bool isOpen;
    private readonly List<NpcQuestObjectiveProgressRowView> spawnedObjectiveRows = new List<NpcQuestObjectiveProgressRowView>();
    private readonly List<NpcQuestRewardEntryView> spawnedRewardEntries = new List<NpcQuestRewardEntryView>();

    private void Start()
    {
        closeButton?.onClick.RemoveAllListeners();
        closeButton?.onClick.AddListener(Hide);

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (objectiveRowTemplate != null)
            objectiveRowTemplate.gameObject.SetActive(false);

        if (rewardEntryTemplate != null)
            rewardEntryTemplate.gameObject.SetActive(false);

        isOpen = false;
        SetStaticPosition(closedAnchoredPosition);
        SetRootActive(false);
        ClearTexts();
    }

    private void OnDisable()
    {
        ClearObjectiveRows();
        ClearRewardEntries();
    }

    public void Show(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
        {
            Hide();
            return;
        }

        SetRootActive(true);
        SetStatus(string.Empty);
        BindQuest(quest);
        StartSlideAnimation(openAnchoredPosition, true);
    }

    public void Hide()
    {
        if (!isOpen)
            return;

        StartSlideAnimation(closedAnchoredPosition, false);
    }

    public void SetPositionOffset(Vector2 openOffset, Vector2 closedOffset)
    {
        openAnchoredPosition = openOffset;
        closedAnchoredPosition = closedOffset;

        if (!isOpen)
            SetStaticPosition(closedAnchoredPosition);
        else
            SetStaticPosition(openAnchoredPosition);
    }

    private void BindQuest(NpcQuestLogQuestEntry quest)
    {
        if (titleText != null)
            titleText.text = quest.QuestTitle;

        BindNpc(quest);

        if (questSummaryText != null)
            questSummaryText.text = string.IsNullOrWhiteSpace(quest.QuestSummary)
                ? "Quest summary unavailable."
                : quest.QuestSummary;

        if (completionInstructionText != null)
            completionInstructionText.text = string.IsNullOrWhiteSpace(quest.CompletionInstruction)
                ? "No completion instruction."
                : quest.CompletionInstruction;

        BindObjectives(quest);

        if (npcProgressText != null)
            npcProgressText.text = BuildNpcProgressText(quest);

        BindRewards(quest);

        string status = BuildStatusText(quest);
        SetStatus(status);
    }

    private void BindNpc(NpcQuestLogQuestEntry quest)
    {
        if (npcNameText != null)
        {
            npcNameText.text = quest != null && !string.IsNullOrWhiteSpace(quest.DetailNpcDisplayName)
                ? quest.DetailNpcDisplayName
                : string.Empty;
            npcNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(npcNameText.text));
        }

        if (npcPortraitImage != null)
        {
            Sprite portrait = quest != null ? quest.DetailNpcPortrait : null;
            npcPortraitImage.sprite = portrait;
            npcPortraitImage.enabled = portrait != null;
        }
    }

    private void BindObjectives(NpcQuestLogQuestEntry quest)
    {
        ClearObjectiveRows();
        if (objectiveListRoot == null || objectiveRowTemplate == null)
        {
            return;
        }

        IReadOnlyList<NpcQuestLogObjectiveProgressSnapshot> objectives = quest != null ? quest.ObjectiveProgress : null;
        if (objectives == null || objectives.Count == 0)
        {
            SetOptionalText(objectiveEmptyText, "No objectives.");
            return;
        }

        SetOptionalText(objectiveEmptyText, string.Empty);

        for (int index = 0; index < objectives.Count; index++)
        {
            NpcQuestLogObjectiveProgressSnapshot objective = objectives[index];
            if (objective == null)
                continue;

            NpcQuestObjectiveProgressRowView row = Instantiate(objectiveRowTemplate, objectiveListRoot);
            row.gameObject.SetActive(true);
            row.Bind(objective);
            spawnedObjectiveRows.Add(row);
        }

        if (spawnedObjectiveRows.Count == 0)
            SetOptionalText(objectiveEmptyText, "No objectives.");
    }

    private void BindRewards(NpcQuestLogQuestEntry quest)
    {
        ClearRewardEntries();
        if (rewardsListRoot == null || rewardEntryTemplate == null)
        {
            return;
        }

        if (quest == null)
        {
            SetOptionalText(rewardsEmptyText, "No rewards.");
            return;
        }

        if (quest.HideRewardsUntilCompletion && quest.ProgressStatus != PlayerQuestProgressStatus.Completed)
        {
            SetOptionalText(rewardsEmptyText, "Rewards hidden until quest completion.");
            return;
        }

        if (quest.Rewards == null || quest.Rewards.Count == 0)
        {
            SetOptionalText(rewardsEmptyText, "No rewards.");
            return;
        }

        SetOptionalText(rewardsEmptyText, string.Empty);

        for (int index = 0; index < quest.Rewards.Count; index++)
        {
            NpcQuestLogRewardSnapshot reward = quest.Rewards[index];
            if (reward == null)
                continue;

            NpcQuestRewardEntryView row = Instantiate(rewardEntryTemplate, rewardsListRoot);
            row.gameObject.SetActive(true);
            row.Bind(reward);
            spawnedRewardEntries.Add(row);
        }

        if (spawnedRewardEntries.Count == 0)
            SetOptionalText(rewardsEmptyText, "No rewards.");
    }

    private static string BuildStatusText(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
            return string.Empty;

        string status = quest.ProgressStatus switch
        {
            PlayerQuestProgressStatus.Accepted => "In Progress",
            PlayerQuestProgressStatus.Completed => "Completed",
            _ => "Available"
        };

        string result = $"Status: {status}";

        if (quest.ProgressStatus == PlayerQuestProgressStatus.Completed && quest.CompletionCount > 0)
            result += $" | Completed {quest.CompletionCount} time(s)";

        if (quest.MinimumPlayerLevel > 1)
            result += $" | Lvl {quest.MinimumPlayerLevel}+";

        return result;
    }

    private static string BuildNpcProgressText(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(quest.DetailNpcDisplayName))
        {
            string prefix = quest.ProgressStatus == PlayerQuestProgressStatus.Completed
                ? "Completed at"
                : quest.ProgressStatus == PlayerQuestProgressStatus.Accepted
                    ? "Complete at"
                    : "Given by";

            return $"{prefix}: {quest.DetailNpcDisplayName}";
        }

        return string.Empty;
    }

    private void SetStatus(string value, string fallback = null)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(value) ? (fallback ?? string.Empty) : value;
    }

    private static void SetOptionalText(TextMeshProUGUI text, string value)
    {
        if (text == null)
            return;

        text.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        text.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
    }

    private void ClearObjectiveRows()
    {
        for (int index = 0; index < spawnedObjectiveRows.Count; index++)
        {
            NpcQuestObjectiveProgressRowView row = spawnedObjectiveRows[index];
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedObjectiveRows.Clear();
    }

    private void ClearRewardEntries()
    {
        for (int index = 0; index < spawnedRewardEntries.Count; index++)
        {
            NpcQuestRewardEntryView row = spawnedRewardEntries[index];
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedRewardEntries.Clear();
    }

    private void StartSlideAnimation(Vector2 targetPosition, bool openState)
    {
        if (panelRect == null)
        {
            isOpen = openState;
            if (!openState)
                SetRootActive(false);
            else
                SetRootActive(true);
            return;
        }

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        if (slideDuration <= 0f)
        {
            panelRect.anchoredPosition = targetPosition;
            isOpen = openState;
            if (!openState)
                SetRootActive(false);
            else
                SetRootActive(true);
            return;
        }

        slideCoroutine = StartCoroutine(AnimateSlide(targetPosition, openState));
    }

    private System.Collections.IEnumerator AnimateSlide(Vector2 targetPosition, bool openState)
    {
        if (panelRect == null)
        {
            isOpen = openState;
            yield break;
        }

        SetRootActive(true);

        Vector2 startPosition = panelRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            panelRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        panelRect.anchoredPosition = targetPosition;
        isOpen = openState;
        slideCoroutine = null;

        if (!openState)
            SetRootActive(false);
    }

    private void SetStaticPosition(Vector2 anchoredPosition)
    {
        if (panelRect == null)
            return;

        panelRect.anchoredPosition = anchoredPosition;
    }

    private void SetRootActive(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);
    }

    private void ClearTexts()
    {
        ClearObjectiveRows();
        ClearRewardEntries();

        if (titleText != null)
            titleText.text = "Quest Details";

        if (npcPortraitImage != null)
        {
            npcPortraitImage.sprite = null;
            npcPortraitImage.enabled = false;
        }

        if (npcNameText != null)
        {
            npcNameText.text = string.Empty;
            npcNameText.gameObject.SetActive(false);
        }

        if (questSummaryText != null)
            questSummaryText.text = string.Empty;

        if (completionInstructionText != null)
            completionInstructionText.text = string.Empty;

        SetOptionalText(objectiveEmptyText, string.Empty);

        if (npcProgressText != null)
            npcProgressText.text = string.Empty;

        SetOptionalText(rewardsEmptyText, string.Empty);

        SetStatus("No selected quest.");
    }
}
