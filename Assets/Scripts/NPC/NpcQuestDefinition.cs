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

    [Header("Presentation")]
    [TextArea(2, 6)]
    [SerializeField] private string questSummary = "Complete this quest to receive rewards.";
    [TextArea(2, 6)]
    [SerializeField] private string progressionHint = string.Empty;
    [TextArea(2, 6)]
    [SerializeField] private string completionInstruction = string.Empty;

    [Header("NPC Flow")]
    [Tooltip("Who can offer this quest.")]
    [SerializeField] private List<NpcDefinition> starterNpcs = new List<NpcDefinition>();
    [Tooltip("Who can complete this quest.")]
    [SerializeField] private List<NpcDefinition> completionNpcs = new List<NpcDefinition>();

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
    public string QuestSummary => questSummary;
    public string ProgressionHint => progressionHint;
    public string CompletionInstruction => completionInstruction;
    public IReadOnlyList<NpcDefinition> StarterNpcs => starterNpcs;
    public IReadOnlyList<NpcDefinition> CompletionNpcs => completionNpcs;
    public NpcDefinition PrimaryStarterNpc => ResolvePrimaryNpcDefinition(starterNpcs);
    public NpcDefinition PrimaryCompletionNpc => ResolvePrimaryNpcDefinition(completionNpcs);
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

    public bool IsStarterNpc(NpcDefinition npcDefinition)
    {
        return MatchesNpcList(starterNpcs, npcDefinition);
    }

    public bool IsCompletionNpc(NpcDefinition npcDefinition)
    {
        return MatchesNpcList(completionNpcs, npcDefinition);
    }

    private static bool MatchesNpcList(IReadOnlyList<NpcDefinition> npcDefinitions, NpcDefinition npcDefinition)
    {
        if (npcDefinitions == null || npcDefinitions.Count == 0)
            return true;

        if (npcDefinition == null)
            return false;

        for (int index = 0; index < npcDefinitions.Count; index++)
        {
            if (AreSameNpcDefinition(npcDefinitions[index], npcDefinition))
                return true;
        }

        return false;
    }

    private static bool AreSameNpcDefinition(NpcDefinition left, NpcDefinition right)
    {
        if (left == null || right == null)
            return false;

        if (left == right)
            return true;

        return NormalizeToken(left.NpcId) == NormalizeToken(right.NpcId);
    }

    private static NpcDefinition ResolvePrimaryNpcDefinition(IReadOnlyList<NpcDefinition> npcDefinitions)
    {
        if (npcDefinitions == null)
            return null;

        for (int index = 0; index < npcDefinitions.Count; index++)
        {
            if (npcDefinitions[index] != null)
                return npcDefinitions[index];
        }

        return null;
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
    [SerializeField] private ItemDefinition targetItem;
    [SerializeField] private EnemyDefinition targetEnemy;
    [SerializeField] private int requiredAmount = 1;
    [TextArea(1, 4)]
    [SerializeField] private string displayLabel = string.Empty;
    [TextArea(1, 4)]
    [SerializeField] private string displayHint = string.Empty;

    public NpcQuestObjectiveType Type => type;
    public ItemDefinition TargetItem => type == NpcQuestObjectiveType.CollectItem ? targetItem : null;
    public EnemyDefinition TargetEnemy => type == NpcQuestObjectiveType.KillEnemy ? targetEnemy : null;
    public string TargetId => ResolveTargetId();
    public int RequiredAmount => requiredAmount;
    public string DisplayLabel => displayLabel;
    public string DisplayHint => displayHint;

    private string ResolveTargetId()
    {
        if (type == NpcQuestObjectiveType.CollectItem)
            return targetItem != null ? targetItem.ItemId : string.Empty;

        if (type == NpcQuestObjectiveType.KillEnemy)
            return targetEnemy != null ? targetEnemy.EnemyType.ToString() : string.Empty;

        return string.Empty;
    }
}

[System.Serializable]
public sealed class NpcQuestRewardItem
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int amount = 1;

    public ItemDefinition Item => item;
    public int Amount => amount;
}
