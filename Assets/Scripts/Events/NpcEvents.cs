using System.Collections.Generic;
using UnityEngine;

public enum NpcPromptType
{
    None = 0,
    Shop = 1,
    Job = 2,
    Quest = 3
}

public struct NpcPromptShownEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public NpcInteractable Npc;
    public string PrimaryText;
    public string SecondaryText;
}

public struct NpcPromptHiddenEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
}

public struct NpcInteractionRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public NpcInteractable Npc;
}

public struct NpcShopOpenedEvent
{
    public NpcShopStateSnapshot Snapshot;
}

public struct NpcShopStateChangedEvent
{
    public NpcShopStateSnapshot Snapshot;
}

public struct NpcShopClosedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
}

public struct NpcShopBuyRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public string ItemId;
    public int Amount;
}

public struct NpcShopSellRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public string ItemId;
    public int Amount;
}

public struct NpcShopCloseRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
}

public struct NpcJobAdvancementOpenedEvent
{
    public NpcJobAdvancementSnapshot Snapshot;
}

public struct NpcJobAdvancementClosedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
}

public struct NpcJobAdvancementSelectRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public PlayerJobType TargetJob;
}

public struct NpcJobAdvancementCloseRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
}

public struct NpcJobAdvancementResultEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
    public PlayerJobType TargetJob;
    public bool IsSuccess;
    public string Message;
}

public struct NpcQuestOpenedEvent
{
    public NpcQuestPromptSnapshot Snapshot;
}

public struct NpcQuestClosedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
}

public struct NpcQuestCloseRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
}

public enum NpcQuestActionType
{
    Accept,
    Complete
}

public struct NpcQuestAcceptRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public string QuestId;
}

public struct NpcQuestCompleteRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public string QuestId;
}

public struct NpcQuestActionResultEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
    public string QuestId;
    public NpcQuestActionType ActionType;
    public bool IsSuccess;
    public string Message;
}

public enum NpcQuestLogTab
{
    Available,
    InProgress,
    Completed
}

public struct NpcQuestLogRequestEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct NpcQuestLogCloseRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
}

public struct NpcQuestLogOpenedEvent
{
    public NpcQuestLogSnapshot Snapshot;
}

public struct NpcQuestLogClosedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct NpcQuestLogRefreshRequestEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct NpcQuestLogRefreshEvent
{
    public NpcQuestLogSnapshot Snapshot;
}

public sealed class NpcQuestLogObjectiveProgressSnapshot
{
    public NpcQuestObjectiveType ObjectiveType { get; }
    public string TargetId { get; }
    public int CurrentAmount { get; }
    public int RequiredAmount { get; }
    public string DisplayLabel { get; }
    public string DisplayHint { get; }
    public Sprite Icon { get; }

    public NpcQuestLogObjectiveProgressSnapshot(
        NpcQuestObjectiveType objectiveType,
        string targetId,
        int currentAmount,
        int requiredAmount,
        string displayLabel = "",
        string displayHint = "",
        Sprite icon = null)
    {
        ObjectiveType = objectiveType;
        TargetId = targetId ?? string.Empty;
        CurrentAmount = Mathf.Max(0, currentAmount);
        RequiredAmount = Mathf.Max(1, requiredAmount);
        DisplayLabel = displayLabel ?? string.Empty;
        DisplayHint = displayHint ?? string.Empty;
        Icon = icon;
    }

}

public enum NpcQuestLogRewardType
{
    Mesos,
    Exp,
    Item
}

public sealed class NpcQuestLogRewardSnapshot
{
    public NpcQuestLogRewardType RewardType { get; }
    public string DisplayLabel { get; }
    public int Amount { get; }
    public Sprite Icon { get; }

    public NpcQuestLogRewardSnapshot(
        NpcQuestLogRewardType rewardType,
        string displayLabel,
        int amount,
        Sprite icon = null)
    {
        RewardType = rewardType;
        DisplayLabel = displayLabel ?? string.Empty;
        Amount = Mathf.Max(0, amount);
        Icon = icon;
    }
}

public sealed class NpcQuestLogQuestEntry
{
    public string QuestId { get; }
    public string QuestTitle { get; }
    public bool IsNarrativeOnly { get; }
    public string QuestSummary { get; }
    public int MinimumPlayerLevel { get; }
    public bool IsRepeatable { get; }
    public PlayerQuestProgressStatus ProgressStatus { get; }
    public int CompletionCount { get; }
    public IReadOnlyList<NpcQuestLogObjectiveProgressSnapshot> ObjectiveProgress { get; }
    public IReadOnlyList<NpcQuestLogRewardSnapshot> Rewards { get; }
    public string CompletionInstruction { get; }
    public string DetailNpcDisplayName { get; }
    public Sprite DetailNpcPortrait { get; }
    public bool HideRewardsUntilCompletion { get; }
    public int ExpReward { get; }
    public int MesosReward { get; }

    public NpcQuestLogQuestEntry(
        string questId,
        string questTitle,
        bool isNarrativeOnly,
        int minimumPlayerLevel,
        bool isRepeatable,
        bool hideRewardsUntilCompletion,
        int expReward,
        int mesosReward,
        PlayerQuestProgressStatus progressStatus,
        int completionCount,
        IReadOnlyList<NpcQuestLogObjectiveProgressSnapshot> objectiveProgress,
        IReadOnlyList<NpcQuestLogRewardSnapshot> rewards,
        string questSummary,
        string completionInstruction,
        string detailNpcDisplayName,
        Sprite detailNpcPortrait)
    {
        QuestId = questId;
        QuestTitle = questTitle;
        IsNarrativeOnly = isNarrativeOnly;
        MinimumPlayerLevel = minimumPlayerLevel;
        IsRepeatable = isRepeatable;
        QuestSummary = questSummary ?? string.Empty;
        HideRewardsUntilCompletion = hideRewardsUntilCompletion;
        ExpReward = expReward;
        MesosReward = mesosReward;
        ProgressStatus = progressStatus;
        CompletionCount = completionCount;
        ObjectiveProgress = objectiveProgress;
        Rewards = rewards;
        CompletionInstruction = completionInstruction ?? string.Empty;
        DetailNpcDisplayName = detailNpcDisplayName ?? string.Empty;
        DetailNpcPortrait = detailNpcPortrait;
    }
}

public sealed class NpcQuestLogSnapshot
{
    public PlayerCharacter Player { get; }
    public string CharacterId { get; }
    public IReadOnlyList<NpcQuestLogQuestEntry> AvailableQuests { get; }
    public IReadOnlyList<NpcQuestLogQuestEntry> InProgressQuests { get; }
    public IReadOnlyList<NpcQuestLogQuestEntry> CompletedQuests { get; }

    public NpcQuestLogSnapshot(
        PlayerCharacter player,
        string characterId,
        IReadOnlyList<NpcQuestLogQuestEntry> availableQuests,
        IReadOnlyList<NpcQuestLogQuestEntry> inProgressQuests,
        IReadOnlyList<NpcQuestLogQuestEntry> completedQuests)
    {
        Player = player;
        CharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
        AvailableQuests = availableQuests;
        InProgressQuests = inProgressQuests;
        CompletedQuests = completedQuests;
    }
}

public sealed class NpcQuestPromptEntry
{
    public string QuestId { get; }
    public string QuestTitle { get; }
    public bool IsNarrativeOnly { get; }
    public string QuestSummary { get; }
    public PlayerQuestProgressStatus ProgressStatus { get; private set; }
    public bool IsRepeatable { get; }
    public int MinimumPlayerLevel { get; }
    public IReadOnlyList<string> IntroPages { get; }
    public IReadOnlyList<string> InProgressPages { get; }
    public IReadOnlyList<string> CompletionPages { get; }
    public bool SupportsInProgress { get; }
    public bool SupportsCompletion { get; }
    public string CompletionInstruction { get; }

    public NpcQuestPromptEntry(
        string questId,
        string questTitle,
        bool isNarrativeOnly,
        PlayerQuestProgressStatus progressStatus,
        bool isRepeatable,
        int minimumPlayerLevel,
        string questSummary,
        string completionInstruction,
        IReadOnlyList<string> introPages,
        IReadOnlyList<string> inProgressPages,
        IReadOnlyList<string> completionPages)
    {
        QuestId = questId;
        QuestTitle = questTitle;
        IsNarrativeOnly = isNarrativeOnly;
        ProgressStatus = progressStatus;
        IsRepeatable = isRepeatable;
        MinimumPlayerLevel = minimumPlayerLevel;
        QuestSummary = questSummary ?? string.Empty;
        CompletionInstruction = completionInstruction ?? string.Empty;
        IntroPages = introPages ?? System.Array.Empty<string>();
        InProgressPages = inProgressPages ?? System.Array.Empty<string>();
        CompletionPages = completionPages ?? System.Array.Empty<string>();
        SupportsInProgress = InProgressPages.Count > 0;
        SupportsCompletion = CompletionPages.Count > 0;
    }

    public void UpdateProgressStatus(PlayerQuestProgressStatus status)
    {
        ProgressStatus = status;
    }
}

public sealed class NpcQuestPromptSnapshot
{
    public PlayerCharacter Player { get; }
    public string CharacterId { get; }
    public string NpcId { get; }
    public string NpcName { get; }
    public Sprite NpcPortrait { get; }
    public bool HasMultipleQuests { get; }
    public IReadOnlyList<NpcQuestPromptEntry> Quests { get; }

    public NpcQuestPromptSnapshot(
        PlayerCharacter player,
        string characterId,
        string npcId,
        string npcName,
        Sprite npcPortrait,
        bool hasMultipleQuests,
        IReadOnlyList<NpcQuestPromptEntry> quests)
    {
        Player = player;
        CharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
        NpcId = npcId;
        NpcName = npcName;
        NpcPortrait = npcPortrait;
        HasMultipleQuests = hasMultipleQuests;
        Quests = quests;
    }
}

public enum NpcShopTransactionKind
{
    Buy,
    Sell
}

public struct NpcShopTransactionResultEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
    public string ItemId;
    public int Amount;
    public int MesosDelta;
    public NpcShopTransactionKind Kind;
    public bool IsSuccess;
    public string Message;
}

public sealed class NpcShopBuyEntry
{
    public string ItemId { get; }
    public string DisplayName { get; }
    public ItemCategory Category { get; }
    public int UnitPrice { get; }
    public int OwnedCount { get; }

    public NpcShopBuyEntry(
        string itemId,
        string displayName,
        ItemCategory category,
        int unitPrice,
        int ownedCount)
    {
        ItemId = itemId;
        DisplayName = displayName;
        Category = category;
        UnitPrice = unitPrice;
        OwnedCount = ownedCount;
    }
}

public sealed class NpcShopSellEntry
{
    public string ItemId { get; }
    public string DisplayName { get; }
    public ItemCategory Category { get; }
    public int OwnedCount { get; }
    public int UnitPrice { get; }

    public NpcShopSellEntry(
        string itemId,
        string displayName,
        ItemCategory category,
        int ownedCount,
        int unitPrice)
    {
        ItemId = itemId;
        DisplayName = displayName;
        Category = category;
        OwnedCount = ownedCount;
        UnitPrice = unitPrice;
    }
}

public sealed class NpcShopStateSnapshot
{
    public PlayerCharacter Player { get; }
    public string CharacterId { get; }
    public string NpcId { get; }
    public string VendorName { get; }
    public int PlayerMesos { get; }
    public bool BuysPlayerItems { get; }
    public IReadOnlyList<NpcShopBuyEntry> BuyEntries { get; }
    public IReadOnlyList<NpcShopSellEntry> SellEntries { get; }

    public NpcShopStateSnapshot(
        PlayerCharacter player,
        string characterId,
        string npcId,
        string vendorName,
        int playerMesos,
        bool buysPlayerItems,
        IReadOnlyList<NpcShopBuyEntry> buyEntries,
        IReadOnlyList<NpcShopSellEntry> sellEntries)
    {
        Player = player;
        CharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
        NpcId = npcId;
        VendorName = vendorName;
        PlayerMesos = playerMesos;
        BuysPlayerItems = buysPlayerItems;
        BuyEntries = buyEntries;
        SellEntries = sellEntries;
    }
}

public sealed class NpcJobAdvancementOption
{
    public PlayerJobType JobType { get; }
    public string DisplayName { get; }
    public bool IsAvailable { get; }

    public NpcJobAdvancementOption(PlayerJobType jobType, string displayName, bool isAvailable)
    {
        JobType = jobType;
        DisplayName = displayName;
        IsAvailable = isAvailable;
    }
}

public sealed class NpcJobAdvancementSnapshot
{
    public PlayerCharacter Player { get; }
    public string CharacterId { get; }
    public string NpcId { get; }
    public string NpcName { get; }
    public int RequiredLevel { get; }
    public PlayerJobType CurrentJob { get; }
    public bool CanAdvance { get; }
    public string StatusMessage { get; }
    public IReadOnlyList<NpcJobAdvancementOption> Options { get; }

    public NpcJobAdvancementSnapshot(
        PlayerCharacter player,
        string characterId,
        string npcId,
        string npcName,
        int requiredLevel,
        PlayerJobType currentJob,
        bool canAdvance,
        string statusMessage,
        IReadOnlyList<NpcJobAdvancementOption> options)
    {
        Player = player;
        CharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
        NpcId = npcId;
        NpcName = npcName;
        RequiredLevel = requiredLevel;
        CurrentJob = currentJob;
        CanAdvance = canAdvance;
        StatusMessage = statusMessage;
        Options = options;
    }
}
