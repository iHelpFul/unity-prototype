using System.Text;
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
    [SerializeField] private TextMeshProUGUI questSummaryText;
    [SerializeField] private TextMeshProUGUI completionInstructionText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI npcProgressText;
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private TextMeshProUGUI statusText;

    private Coroutine slideCoroutine;
    private bool isOpen;

    private void Start()
    {
        closeButton?.onClick.RemoveAllListeners();
        closeButton?.onClick.AddListener(Hide);

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        isOpen = false;
        SetStaticPosition(closedAnchoredPosition);
        SetRootActive(false);
        ClearTexts();
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

        if (questSummaryText != null)
            questSummaryText.text = string.IsNullOrWhiteSpace(quest.QuestSummary)
                ? "Quest summary unavailable."
                : quest.QuestSummary;

        if (completionInstructionText != null)
            completionInstructionText.text = string.IsNullOrWhiteSpace(quest.CompletionInstruction)
                ? "No completion instruction."
                : quest.CompletionInstruction;

        if (objectiveText != null)
            objectiveText.text = BuildObjectiveText(quest);

        if (npcProgressText != null)
            npcProgressText.text = BuildNpcProgressText(quest);

        if (rewardsText != null)
            rewardsText.text = BuildRewardsText(quest);

        string status = BuildStatusText(quest);
        SetStatus(status);
    }

    private static string BuildObjectiveText(NpcQuestLogQuestEntry quest)
    {
        if (quest == null || quest.ObjectiveProgress == null || quest.ObjectiveProgress.Count == 0)
            return "No objectives.";

        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < quest.ObjectiveProgress.Count; index++)
        {
            NpcQuestLogObjectiveProgressSnapshot objective = quest.ObjectiveProgress[index];
            string objectiveName = ResolveObjectiveDisplayName(objective);
            string objectiveHint = string.IsNullOrWhiteSpace(objective.DisplayHint)
                ? string.Empty
                : $" - {objective.DisplayHint}";

            builder.AppendLine($"{objectiveName}: {objective.CurrentAmount}/{objective.RequiredAmount}{objectiveHint}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildRewardsText(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
            return "Rewards: -";

        if (quest.HideRewardsUntilCompletion && quest.ProgressStatus != PlayerQuestProgressStatus.Completed)
            return "Rewards:\n- Hidden until quest completion.";

        if (quest == null || quest.RewardSummaries == null || quest.RewardSummaries.Count == 0)
            return "Rewards:\n- None";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Rewards:");
        for (int index = 0; index < quest.RewardSummaries.Count; index++)
        {
            string reward = quest.RewardSummaries[index];
            if (!string.IsNullOrWhiteSpace(reward))
                builder.AppendLine($"- {reward}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildStatusText(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder("Status: ");
        switch (quest.ProgressStatus)
        {
            case PlayerQuestProgressStatus.Accepted:
                builder.Append("In Progress");
                break;
            case PlayerQuestProgressStatus.Completed:
                builder.Append("Completed");
                break;
            default:
                builder.Append("Available");
                break;
        }

        if (quest.ProgressStatus == PlayerQuestProgressStatus.Completed && quest.CompletionCount > 0)
            builder.Append($" | Completed {quest.CompletionCount} time(s)");

        if (quest.MinimumPlayerLevel > 1)
            builder.Append($" | Lvl {quest.MinimumPlayerLevel}+");

        return builder.ToString();
    }

    private static string BuildNpcProgressText(NpcQuestLogQuestEntry quest)
    {
        if (quest == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(quest.CompletionNpcDisplayName))
            return $"Complete at: {quest.CompletionNpcDisplayName}";

        if (!string.IsNullOrWhiteSpace(quest.StarterNpcDisplayName))
            return $"Given by: {quest.StarterNpcDisplayName}";

        return string.Empty;
    }

    private void SetStatus(string value, string fallback = null)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(value) ? (fallback ?? string.Empty) : value;
    }

    private static string ResolveObjectiveTargetName(NpcQuestLogObjectiveProgressSnapshot objective)
    {
        if (objective == null)
            return "Objective";

        if (!string.IsNullOrWhiteSpace(objective.DisplayLabel))
            return objective.DisplayLabel;

        string targetLabel = string.IsNullOrWhiteSpace(objective.TargetId) ? "Unknown" : objective.TargetId;

        if (objective.ObjectiveType == NpcQuestObjectiveType.CollectItem
            && ItemDatabase.TryGetDefinition(targetLabel, out ItemDefinition definition)
            && definition != null)
        {
            targetLabel = definition.DisplayName;
        }

        if (objective.ObjectiveType == NpcQuestObjectiveType.KillEnemy)
        {
            if (TryResolveEnemyTypeDisplayName(targetLabel, out string enemyName))
                targetLabel = enemyName;
            else
                targetLabel = $"Enemy [{targetLabel}]";
        }

        string prefix = objective.ObjectiveType == NpcQuestObjectiveType.CollectItem ? "Collect" : "Kill";
        return $"{prefix} {targetLabel}";
    }

    private static string ResolveObjectiveDisplayName(NpcQuestLogObjectiveProgressSnapshot objective)
    {
        return ResolveObjectiveTargetName(objective);
    }

    private static bool TryResolveEnemyTypeDisplayName(string targetId, out string enemyName)
    {
        enemyName = string.Empty;
        if (string.IsNullOrWhiteSpace(targetId))
            return false;

        foreach (EnemyType enemyType in System.Enum.GetValues(typeof(EnemyType)))
        {
            if (enemyType.ToString().Equals(targetId, System.StringComparison.OrdinalIgnoreCase))
            {
                enemyName = enemyType.ToString();
                return true;
            }
        }

        return false;
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
        if (titleText != null)
            titleText.text = "Quest Details";

        if (questSummaryText != null)
            questSummaryText.text = string.Empty;

        if (completionInstructionText != null)
            completionInstructionText.text = string.Empty;

        if (objectiveText != null)
            objectiveText.text = "No quest selected.";

        if (npcProgressText != null)
            npcProgressText.text = string.Empty;

        if (rewardsText != null)
            rewardsText.text = "Rewards: -";

        SetStatus("No selected quest.");
    }
}
