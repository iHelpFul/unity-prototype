using System.Collections.Generic;
using UnityEngine;

public enum NpcQuestDialogueKind
{
    Intro,
    InProgress,
    Completion
}

[CreateAssetMenu(menuName = "Game Data/NPC/Quest Definition")]
public class NpcQuestDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string questId = "quest_default";
    [SerializeField] private string questTitle = "New Quest";
    [SerializeField] private bool isNarrativeOnly;

    [Header("NPC Flow")]
    [Tooltip("Who can offer this quest.")]
    [SerializeField] private List<string> starterNpcIds = new List<string>();
    [Tooltip("Who can complete this quest.")]
    [SerializeField] private List<string> completionNpcIds = new List<string>();

    [Header("Flow")]
    [TextArea(2, 6)]
    [SerializeField] private List<string> introPages = new List<string>();
    [TextArea(2, 6)]
    [SerializeField] private List<string> inProgressPages = new List<string>();
    [TextArea(2, 6)]
    [SerializeField] private List<string> completionPages = new List<string>();

    [Header("Requirements")]
    [SerializeField] private int minimumPlayerLevel;
    [SerializeField] private bool repeatable;

    [Header("Rewards")]
    [SerializeField] private bool hideRewardsUntilCompletion = true;
    [SerializeField] private int expReward;
    [SerializeField] private int mesosReward;
    [SerializeField] private List<NpcQuestRewardItem> rewardItems = new List<NpcQuestRewardItem>();
    [SerializeField] private List<NpcQuestObjective> objectives = new List<NpcQuestObjective>();

    public string QuestId => string.IsNullOrWhiteSpace(questId) ? questTitle : questId.Trim();
    public string QuestTitle => questTitle;
    public bool IsNarrativeOnly => isNarrativeOnly;
    public IReadOnlyList<string> StarterNpcIds => starterNpcIds;
    public IReadOnlyList<string> CompletionNpcIds => completionNpcIds;
    public IReadOnlyList<string> IntroPages => introPages;
    public IReadOnlyList<string> InProgressPages => inProgressPages;
    public IReadOnlyList<string> CompletionPages => completionPages;
    public int MinimumPlayerLevel => minimumPlayerLevel;
    public bool Repeatable => repeatable;
    public bool HideRewardsUntilCompletion => hideRewardsUntilCompletion;
    public int ExpReward => expReward;
    public int MesosReward => mesosReward;
    public IReadOnlyList<NpcQuestRewardItem> RewardItems => rewardItems;
    public IReadOnlyList<NpcQuestObjective> Objectives => objectives;

    public bool IsStarterNpc(string npcId)
    {
        if (starterNpcIds == null || starterNpcIds.Count == 0)
            return true;

        string normalizedNpcId = NormalizeToken(npcId);
        if (string.IsNullOrWhiteSpace(normalizedNpcId))
            return false;

        for (int index = 0; index < starterNpcIds.Count; index++)
        {
            if (NormalizeToken(starterNpcIds[index]) == normalizedNpcId)
                return true;
        }

        return false;
    }

    public bool IsCompletionNpc(string npcId)
    {
        if (completionNpcIds == null || completionNpcIds.Count == 0)
            return true;

        string normalizedNpcId = NormalizeToken(npcId);
        if (string.IsNullOrWhiteSpace(normalizedNpcId))
            return false;

        for (int index = 0; index < completionNpcIds.Count; index++)
        {
            if (NormalizeToken(completionNpcIds[index]) == normalizedNpcId)
                return true;
        }

        return false;
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }
}

[System.Serializable]
public enum NpcQuestObjectiveType
{
    KillEnemy,
    CollectItem
}

[System.Serializable]
public sealed class NpcQuestObjective
{
    [SerializeField] private NpcQuestObjectiveType type;
    [SerializeField] private string targetId = string.Empty;
    [SerializeField] private int requiredAmount = 1;

    public NpcQuestObjectiveType Type => type;
    public string TargetId => targetId;
    public int RequiredAmount => requiredAmount;
}

[System.Serializable]
public sealed class NpcQuestRewardItem
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int amount = 1;

    public ItemDefinition Item => item;
    public int Amount => amount;
}
