using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NpcQuestActionProcessor
{
    public NpcQuestLogSnapshot BuildQuestLogSnapshot(
        PlayerCharacter player,
        PlayerRuntimeData playerData,
        IReadOnlyList<NpcQuestDefinition> questDefinitions,
        PlayerSessionInventoryApplicationService inventorySession = null,
        QuestUiIconCatalog questUiIconCatalog = null)
    {
        List<NpcQuestLogQuestEntry> available = new List<NpcQuestLogQuestEntry>();
        List<NpcQuestLogQuestEntry> inProgress = new List<NpcQuestLogQuestEntry>();
        List<NpcQuestLogQuestEntry> completed = new List<NpcQuestLogQuestEntry>();

        string characterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(player != null ? player.CharacterId : string.Empty);

        if (playerData == null || questDefinitions == null)
            return new NpcQuestLogSnapshot(player, characterId, available, inProgress, completed);

        HashSet<string> visited = new HashSet<string>();
        for (int index = 0; index < questDefinitions.Count; index++)
        {
            NpcQuestDefinition quest = questDefinitions[index];
            if (quest == null)
                continue;

            string questId = NormalizeQuestId(quest.QuestId);
            if (string.IsNullOrWhiteSpace(questId))
                continue;

            string normalizedQuestId = NormalizeToken(questId);
            if (string.IsNullOrWhiteSpace(normalizedQuestId) || !visited.Add(normalizedQuestId))
                continue;

            if (quest.IsNarrativeOnly)
                continue;

            if (playerData.Level < quest.MinimumPlayerLevel)
                continue;

            PlayerQuestProgressEntry progress = ResolveProgress(playerData, questId);
            NpcQuestLogQuestEntry entry = BuildLogQuestEntry(
                quest,
                progress,
                ResolveProgressData(
                    quest,
                    progress,
                    characterId,
                    inventorySession,
                    ResolveKillTracker(),
                    questUiIconCatalog));

            if (entry.ProgressStatus == PlayerQuestProgressStatus.Accepted)
            {
                inProgress.Add(entry);
            }
            else if (entry.ProgressStatus == PlayerQuestProgressStatus.Completed)
            {
                completed.Add(entry);
            }
            else
            {
                available.Add(entry);
            }
        }

        return new NpcQuestLogSnapshot(player, characterId, available, inProgress, completed);
    }

    public NpcQuestActionResult ProcessAction(
        NpcQuestActionType actionType,
        PlayerCharacter player,
        PlayerRuntimeData playerData,
        NpcQuestDefinition quest,
        PlayerSessionInventoryApplicationService inventorySession,
        PlayerSessionCurrencyApplicationService currencySession,
        GameBootstrap bootstrap)
    {
        if (player == null)
        {
            return NpcQuestActionResult.Fail(
                actionType,
                quest != null ? quest.QuestId : string.Empty,
                "Invalid player.");
        }

        if (playerData == null)
        {
            return NpcQuestActionResult.Fail(
                actionType,
                quest != null ? quest.QuestId : string.Empty,
                "Player data is not ready.");
        }

        if (quest == null)
        {
            return NpcQuestActionResult.Fail(
                actionType,
                string.Empty,
                "Quest is not available.");
        }

        string questId = NormalizeQuestId(quest.QuestId);
        if (string.IsNullOrWhiteSpace(questId))
        {
            return NpcQuestActionResult.Fail(
                actionType,
                quest.QuestId,
                "Quest data is missing an identifier.");
        }

        if (playerData.Level < quest.MinimumPlayerLevel)
        {
            return NpcQuestActionResult.Fail(
                actionType,
                quest.QuestId,
                $"You need to be at least level {quest.MinimumPlayerLevel}.");
        }

        PlayerQuestProgressEntry progress = ResolveProgress(playerData, questId);

        if (actionType == NpcQuestActionType.Accept)
        {
            NpcQuestActionResult acceptResult = ProcessAccept(
                quest,
                progress,
                player,
                playerData,
                questId,
                inventorySession);
            if (acceptResult.IsSuccess)
                bootstrap?.CharacterSession?.Save();

            return acceptResult;
        }

        if (actionType == NpcQuestActionType.Complete)
        {
            NpcQuestActionResult completeResult = ProcessComplete(
                quest,
                progress,
                player,
                playerData,
                inventorySession,
                currencySession,
                bootstrap);

            if (completeResult.IsSuccess)
                bootstrap?.CharacterSession?.Save();

            return completeResult;
        }

        return NpcQuestActionResult.Fail(
            actionType,
            quest.QuestId,
            "Unknown quest action.");
    }

    private static NpcQuestLogQuestEntry BuildLogQuestEntry(
        NpcQuestDefinition quest,
        PlayerQuestProgressEntry progress,
        NpcQuestProgressData progressData)
    {
        return new NpcQuestLogQuestEntry(
            quest.QuestId,
            quest.QuestTitle,
            quest.IsNarrativeOnly,
            quest.MinimumPlayerLevel,
            quest.Repeatable,
            quest.HideRewardsUntilCompletion,
            quest.ExpReward,
            quest.MesosReward,
            progress != null ? progress.Status : PlayerQuestProgressStatus.None,
            progress != null ? progress.CompletionCount : 0,
            progressData.ObjectiveProgress,
            progressData.Rewards,
            quest.QuestSummary,
            quest.CompletionInstruction,
            ResolveDetailNpcDisplayName(quest, progress != null ? progress.Status : PlayerQuestProgressStatus.None),
            ResolveDetailNpcPortrait(quest, progress != null ? progress.Status : PlayerQuestProgressStatus.None));
    }

    private static NpcQuestProgressData ResolveProgressData(
        NpcQuestDefinition quest,
        PlayerQuestProgressEntry progress,
        string characterId,
        PlayerSessionInventoryApplicationService inventorySession,
        KillTrackerSystem killTracker,
        QuestUiIconCatalog questUiIconCatalog)
    {
        return new NpcQuestProgressData(
            BuildObjectiveProgressSnapshots(
                quest,
                progress,
                characterId,
                inventorySession,
                killTracker),
            BuildRewardSnapshots(quest, progress, questUiIconCatalog));
    }

    private static IReadOnlyList<NpcQuestLogObjectiveProgressSnapshot> BuildObjectiveProgressSnapshots(
        NpcQuestDefinition quest,
        PlayerQuestProgressEntry progress,
        string characterId,
        PlayerSessionInventoryApplicationService inventorySession,
        KillTrackerSystem killTracker)
    {
        if (quest == null || quest.Objectives == null || quest.Objectives.Count == 0)
            return System.Array.Empty<NpcQuestLogObjectiveProgressSnapshot>();

        List<NpcQuestLogObjectiveProgressSnapshot> progressSnapshots = new List<NpcQuestLogObjectiveProgressSnapshot>(quest.Objectives.Count);
        bool isCompletedQuest = progress != null && progress.Status == PlayerQuestProgressStatus.Completed;

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            NpcQuestObjective objective = quest.Objectives[index];
            if (objective == null)
                continue;

            string targetId = ResolveObjectiveTargetId(objective);
            int requiredAmount = Mathf.Max(1, objective.RequiredAmount);
            string objectiveLabel = ResolveObjectiveDisplayLabel(objective, targetId);
            string objectiveHint = objective.DisplayHint;
            Sprite objectiveIcon = ResolveObjectiveIcon(objective);
            int baseline = 0;

            if (progress != null && progress.ObjectiveProgress != null)
                TryResolveObjectiveBaseline(
                    progress,
                    objective,
                    index,
                    targetId,
                    NormalizeToken(targetId),
                    requiredAmount,
                out baseline);

            if (isCompletedQuest)
            {
                progressSnapshots.Add(new NpcQuestLogObjectiveProgressSnapshot(
                    objective.Type,
                    targetId,
                    requiredAmount,
                    requiredAmount,
                    objectiveLabel,
                    objectiveHint,
                    objectiveIcon));
                continue;
            }

            if (objective.Type == NpcQuestObjectiveType.KillEnemy
                && objective.TargetEnemy != null
                && killTracker != null)
            {
                EnemyType enemyType = objective.TargetEnemy.EnemyType;
                int currentKills = killTracker.GetKillCountForCharacter(characterId, enemyType);
                progressSnapshots.Add(new NpcQuestLogObjectiveProgressSnapshot(
                    objective.Type,
                    targetId,
                    Mathf.Max(0, currentKills - baseline),
                    requiredAmount,
                    objectiveLabel,
                    objectiveHint,
                    objectiveIcon));
                continue;
            }

            if (objective.Type == NpcQuestObjectiveType.CollectItem && !string.IsNullOrWhiteSpace(targetId))
            {
                int currentItems = inventorySession != null ? inventorySession.GetInventoryCount(targetId) : 0;
                progressSnapshots.Add(new NpcQuestLogObjectiveProgressSnapshot(
                    objective.Type,
                    targetId,
                    Mathf.Max(0, currentItems - baseline),
                    requiredAmount,
                    objectiveLabel,
                    objectiveHint,
                    objectiveIcon));
                continue;
            }

            progressSnapshots.Add(new NpcQuestLogObjectiveProgressSnapshot(
                objective.Type,
                targetId,
                0,
                requiredAmount,
                objectiveLabel,
                objectiveHint,
                objectiveIcon));
        }

        return progressSnapshots;
    }

    private static Sprite ResolveObjectiveIcon(NpcQuestObjective objective)
    {
        if (objective == null)
            return null;

        if (objective.Type == NpcQuestObjectiveType.CollectItem
            && objective.TargetItem != null)
        {
            return objective.TargetItem.Icon;
        }

        if (objective.Type == NpcQuestObjectiveType.KillEnemy
            && objective.TargetEnemy != null)
        {
            return objective.TargetEnemy.Icon;
        }

        return null;
    }

    private static string ResolveObjectiveDisplayLabel(NpcQuestObjective objective, string targetId)
    {
        if (objective == null)
            return "Objective";

        if (!string.IsNullOrWhiteSpace(objective.DisplayLabel))
            return objective.DisplayLabel.Trim();

        if (objective.Type == NpcQuestObjectiveType.CollectItem)
        {
            string itemName = objective.TargetItem != null && !string.IsNullOrWhiteSpace(objective.TargetItem.DisplayName)
                ? objective.TargetItem.DisplayName
                : (!string.IsNullOrWhiteSpace(targetId) ? targetId : "Item");

            return $"Collect {itemName}";
        }

        if (objective.Type == NpcQuestObjectiveType.KillEnemy)
        {
            string enemyName = objective.TargetEnemy != null
                ? objective.TargetEnemy.EnemyType.ToString()
                : (!string.IsNullOrWhiteSpace(targetId) ? targetId : "Enemy");

            return $"Kill {enemyName}";
        }

        return !string.IsNullOrWhiteSpace(targetId) ? targetId : "Objective";
    }

    private static IReadOnlyList<NpcQuestLogRewardSnapshot> BuildRewardSnapshots(
        NpcQuestDefinition quest,
        PlayerQuestProgressEntry progress,
        QuestUiIconCatalog questUiIconCatalog)
    {
        if (quest == null)
            return System.Array.Empty<NpcQuestLogRewardSnapshot>();

        if (quest.HideRewardsUntilCompletion && (progress == null || progress.Status != PlayerQuestProgressStatus.Completed))
            return System.Array.Empty<NpcQuestLogRewardSnapshot>();

        List<NpcQuestLogRewardSnapshot> rewards = new List<NpcQuestLogRewardSnapshot>();

        if (quest.MesosReward > 0)
        {
            rewards.Add(new NpcQuestLogRewardSnapshot(
                NpcQuestLogRewardType.Mesos,
                "Mesos",
                quest.MesosReward,
                questUiIconCatalog != null ? questUiIconCatalog.MesosIcon : null));
        }

        if (quest.ExpReward > 0)
        {
            rewards.Add(new NpcQuestLogRewardSnapshot(
                NpcQuestLogRewardType.Exp,
                "EXP",
                quest.ExpReward,
                questUiIconCatalog != null ? questUiIconCatalog.ExpIcon : null));
        }

        if (quest.RewardItems == null || quest.RewardItems.Count == 0)
            return rewards;

        for (int index = 0; index < quest.RewardItems.Count; index++)
        {
            NpcQuestRewardItem reward = quest.RewardItems[index];
            if (reward == null || reward.Item == null || reward.Amount <= 0)
                continue;

            rewards.Add(new NpcQuestLogRewardSnapshot(
                NpcQuestLogRewardType.Item,
                reward.Item.DisplayName,
                Mathf.Max(1, reward.Amount),
                reward.Item.Icon));
        }

        return rewards;
    }

    private static NpcQuestActionResult ProcessAccept(
        NpcQuestDefinition quest,
        PlayerQuestProgressEntry progress,
        PlayerCharacter player,
        PlayerRuntimeData playerData,
        string questId,
        PlayerSessionInventoryApplicationService inventorySession)
    {
        if (quest.IsNarrativeOnly)
        {
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Accept,
                quest.QuestId,
                "This interaction is for dialogue only.");
        }

        if (progress != null)
        {
            if (!quest.Repeatable && progress.Status == PlayerQuestProgressStatus.Completed)
            {
                return NpcQuestActionResult.Fail(
                    NpcQuestActionType.Accept,
                    quest.QuestId,
                    "This quest has already been completed.");
            }

            if (progress.Status == PlayerQuestProgressStatus.Accepted)
            {
                return NpcQuestActionResult.Fail(
                    NpcQuestActionType.Accept,
                    quest.QuestId,
                    "Quest already accepted.");
            }
        }

        PlayerQuestProgressEntry progressEntry = EnsureProgress(playerData, questId, PlayerQuestProgressStatus.Accepted);
        InitializeObjectiveProgress(progressEntry, quest, player, inventorySession);

        return NpcQuestActionResult.Ok(
            NpcQuestActionType.Accept,
            quest.QuestId,
            $"Quest '{quest.QuestTitle}' accepted.");
    }

    private static NpcQuestActionResult ProcessComplete(
        NpcQuestDefinition quest,
        PlayerQuestProgressEntry progress,
        PlayerCharacter player,
        PlayerRuntimeData playerData,
        PlayerSessionInventoryApplicationService inventorySession,
        PlayerSessionCurrencyApplicationService currencySession,
        GameBootstrap bootstrap)
    {
        if (progress == null || progress.Status != PlayerQuestProgressStatus.Accepted)
        {
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Complete,
                quest.QuestId,
                "Quest must be accepted before it can be completed.");
        }

        if (!quest.Repeatable && progress.Status == PlayerQuestProgressStatus.Completed)
        {
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Complete,
                quest.QuestId,
                "This quest has already been completed.");
        }

        NpcQuestActionResult objectiveCheck = ValidateObjectivesForCompletion(
            quest,
            progress,
            player,
            inventorySession,
            out Dictionary<string, int> collectRequirements);
        if (!objectiveCheck.IsSuccess)
            return objectiveCheck;

        if (!TryConsumeCollectRequirements(collectRequirements, player, inventorySession))
        {
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Complete,
                quest.QuestId,
                "Could not consume required items.");
        }

        if (quest.MesosReward > 0 && currencySession != null)
            currencySession.AddMesos(player, quest.MesosReward);

        if (quest.RewardItems != null)
        {
            for (int index = 0; index < quest.RewardItems.Count; index++)
            {
                NpcQuestRewardItem reward = quest.RewardItems[index];
                if (reward == null || reward.Item == null || reward.Amount <= 0)
                    continue;

                inventorySession?.AddInventoryItem(player, reward.Item.ItemId, reward.Amount);
            }
        }

        if (quest.ExpReward > 0)
        {
            if (bootstrap != null)
            {
                new PlayerProgressionModule(playerData, player, bootstrap).AddExp(quest.ExpReward);
            }
            else
            {
                playerData.CurrentExp += quest.ExpReward;
            }
        }

        MarkCompleted(progress);

        return NpcQuestActionResult.Ok(
            NpcQuestActionType.Complete,
            quest.QuestId,
            $"Quest '{quest.QuestTitle}' completed.");
    }

    private static NpcQuestActionResult ValidateObjectivesForCompletion(
        NpcQuestDefinition quest,
        PlayerQuestProgressEntry progress,
        PlayerCharacter player,
        PlayerSessionInventoryApplicationService inventorySession,
        out Dictionary<string, int> collectRequirements)
    {
        collectRequirements = new Dictionary<string, int>();

        if (quest == null)
        {
            collectRequirements = null;
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Complete,
                string.Empty,
                "Quest is not available.");
        }

        if (quest.Objectives == null || quest.Objectives.Count == 0)
        {
            return NpcQuestActionResult.Ok(
                NpcQuestActionType.Complete,
                quest.QuestId,
                "No objective requirements.");
        }

        int trackedObjectiveCount = 0;
        for (int objectiveIndex = 0; objectiveIndex < quest.Objectives.Count; objectiveIndex++)
        {
            if (quest.Objectives[objectiveIndex] != null)
                trackedObjectiveCount++;
        }

        if (progress.ObjectiveProgress == null || progress.ObjectiveProgress.Count < trackedObjectiveCount)
        {
            collectRequirements = null;
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Complete,
                quest.QuestId,
                "Quest progress is not initialized properly. Re-accept the quest.");
        }

        if (player == null || inventorySession == null)
        {
            collectRequirements = null;
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Complete,
                quest.QuestId,
                "Quest completion services are not ready.");
        }

        if (progress == null)
        {
            collectRequirements = null;
            return NpcQuestActionResult.Fail(
                NpcQuestActionType.Complete,
                quest.QuestId,
                "Quest progress is missing.");
        }

        Dictionary<EnemyType, int> usedKillProgress = new Dictionary<EnemyType, int>();
        Dictionary<string, int> usedCollectProgress = new Dictionary<string, int>();
        KillTrackerSystem tracker = ResolveKillTracker();
        string characterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(player.CharacterId);

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            NpcQuestObjective objective = quest.Objectives[index];
            if (objective == null)
                continue;

            int requiredAmount = Mathf.Max(1, objective.RequiredAmount);
            string targetId = ResolveObjectiveTargetId(objective);
            string objectiveKey = NormalizeToken(targetId);
            if (!TryResolveObjectiveBaseline(
                progress,
                objective,
                index,
                targetId,
                objectiveKey,
                requiredAmount,
                out int baselineValue))
            {
                return NpcQuestActionResult.Fail(
                    NpcQuestActionType.Complete,
                    quest.QuestId,
                    "Quest progress data is incomplete. Re-accept the quest.");
            }

            if (objective.Type == NpcQuestObjectiveType.KillEnemy)
            {
                if (player == null)
                {
                    return NpcQuestActionResult.Fail(
                        NpcQuestActionType.Complete,
                        quest.QuestId,
                        "Player data is missing for kill objective validation.");
                }

                if (objective.TargetEnemy == null)
                {
                    return NpcQuestActionResult.Fail(
                        NpcQuestActionType.Complete,
                        quest.QuestId,
                        "Kill objective references an unknown enemy type.");
                }

                EnemyType enemyType = objective.TargetEnemy.EnemyType;

                if (tracker == null)
                {
                    collectRequirements = null;
                    return NpcQuestActionResult.Fail(
                        NpcQuestActionType.Complete,
                        quest.QuestId,
                        "Kill tracker is unavailable.");
                }

                int currentKills = tracker.GetKillCountForCharacter(characterId, enemyType);
                int availableKills = Mathf.Max(0, currentKills - baselineValue);
                int consumedKills = usedKillProgress.TryGetValue(enemyType, out int used) ? used : 0;
                if (availableKills - consumedKills < requiredAmount)
                {
                    return NpcQuestActionResult.Fail(
                        NpcQuestActionType.Complete,
                        quest.QuestId,
                        $"Kill requirement not met for {enemyType}. {Mathf.Max(0, availableKills - consumedKills)}/{requiredAmount}.");
                }

                usedKillProgress[enemyType] = consumedKills + requiredAmount;
                continue;
            }

            if (objective.Type == NpcQuestObjectiveType.CollectItem)
            {
                if (objective.TargetItem == null || string.IsNullOrWhiteSpace(targetId))
                {
                    return NpcQuestActionResult.Fail(
                        NpcQuestActionType.Complete,
                        quest.QuestId,
                        "Quest collect objective is missing a target item.");
                }

                int currentItems = inventorySession.GetInventoryCount(targetId);
                int availableItems = Mathf.Max(0, currentItems);
                int consumedItems = usedCollectProgress.TryGetValue(objectiveKey, out int used) ? used : 0;
                if (availableItems - consumedItems < requiredAmount)
                {
                    return NpcQuestActionResult.Fail(
                        NpcQuestActionType.Complete,
                        quest.QuestId,
                        "Collect requirement not met yet.");
                }

                usedCollectProgress[objectiveKey] = consumedItems + requiredAmount;
                collectRequirements[targetId] = usedCollectProgress[objectiveKey];
            }
        }

        return NpcQuestActionResult.Ok(
            NpcQuestActionType.Complete,
            quest.QuestId,
            "Requirements met.");
    }

    private static string NormalizeQuestId(string questId)
    {
        return (questId ?? string.Empty).Trim();
    }

    private static PlayerQuestProgressEntry ResolveProgress(
        PlayerRuntimeData playerData,
        string questId)
    {
        if (playerData == null || playerData.QuestProgress == null || string.IsNullOrWhiteSpace(questId))
            return null;

        for (int index = 0; index < playerData.QuestProgress.Count; index++)
        {
            PlayerQuestProgressEntry entry = playerData.QuestProgress[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.QuestId))
                continue;

            if (entry.QuestId == questId)
                return entry;
        }

        return null;
    }

    private static PlayerQuestProgressEntry EnsureProgress(
        PlayerRuntimeData playerData,
        string questId,
        PlayerQuestProgressStatus status)
    {
        if (playerData == null || string.IsNullOrWhiteSpace(questId))
            return null;

        playerData.QuestProgress ??= new List<PlayerQuestProgressEntry>();

        PlayerQuestProgressEntry entry = ResolveProgress(playerData, questId);
        if (entry != null)
        {
            entry.ObjectiveProgress ??= new List<PlayerQuestObjectiveProgress>();
            entry.Status = status;
            return entry;
        }

        entry = new PlayerQuestProgressEntry
        {
            QuestId = questId,
            Status = status,
            CompletionCount = 0,
            ObjectiveProgress = new List<PlayerQuestObjectiveProgress>()
        };
        playerData.QuestProgress.Add(entry);

        return entry;
    }

    private static void InitializeObjectiveProgress(
        PlayerQuestProgressEntry progress,
        NpcQuestDefinition quest,
        PlayerCharacter player,
        PlayerSessionInventoryApplicationService inventorySession)
    {
        if (progress == null || quest == null || quest.Objectives == null)
            return;

        progress.ObjectiveProgress ??= new List<PlayerQuestObjectiveProgress>();
        progress.ObjectiveProgress.Clear();

        string characterId = player != null ? PlayerRuntimeIdentityUtility.NormalizeCharacterId(player.CharacterId) : string.Empty;
        KillTrackerSystem tracker = ResolveKillTracker();

        for (int index = 0; index < quest.Objectives.Count; index++)
        {
            NpcQuestObjective objective = quest.Objectives[index];
            if (objective == null)
                continue;

            int requiredAmount = Mathf.Max(1, objective.RequiredAmount);
            string targetId = ResolveObjectiveTargetId(objective);
            int baselineValue = 0;

            if (objective.Type == NpcQuestObjectiveType.KillEnemy)
            {
                if (objective.TargetEnemy != null)
                {
                    EnemyType enemyType = objective.TargetEnemy.EnemyType;
                    baselineValue = tracker != null
                        ? tracker.GetKillCountForCharacter(characterId, enemyType)
                        : 0;
                }
            }

            if (objective.Type == NpcQuestObjectiveType.CollectItem
                && objective.TargetItem != null
                && !string.IsNullOrWhiteSpace(targetId))
            {
                baselineValue = 0;
            }

            progress.ObjectiveProgress.Add(new PlayerQuestObjectiveProgress
            {
                ObjectiveIndex = index,
                ObjectiveType = objective.Type,
                TargetId = targetId,
                BaselineValue = baselineValue,
                RequiredAmount = requiredAmount
            });
        }
    }

    private static bool TryResolveObjectiveBaseline(
        PlayerQuestProgressEntry progress,
        NpcQuestObjective objective,
        int objectiveIndex,
        string targetId,
        string normalizedTargetId,
        int requiredAmount,
        out int baselineValue)
    {
        baselineValue = 0;

        if (progress == null || progress.ObjectiveProgress == null)
            return false;

        string normalizedObjectiveTarget = string.IsNullOrWhiteSpace(normalizedTargetId)
            ? NormalizeToken(targetId)
            : normalizedTargetId;

        for (int index = 0; index < progress.ObjectiveProgress.Count; index++)
        {
            PlayerQuestObjectiveProgress objectiveProgress = progress.ObjectiveProgress[index];
            if (objectiveProgress == null)
                continue;

            if (objectiveProgress.ObjectiveIndex == objectiveIndex
                && objectiveProgress.ObjectiveType == objective.Type
                && NormalizeToken(objectiveProgress.TargetId) == normalizedObjectiveTarget
                && objectiveProgress.RequiredAmount == requiredAmount)
            {
                baselineValue = objectiveProgress.BaselineValue;
                return true;
            }
        }

        for (int index = 0; index < progress.ObjectiveProgress.Count; index++)
        {
            PlayerQuestObjectiveProgress objectiveProgress = progress.ObjectiveProgress[index];
            if (objectiveProgress == null)
                continue;

            if (objectiveProgress.ObjectiveType == objective.Type
                && NormalizeToken(objectiveProgress.TargetId) == normalizedObjectiveTarget
                && objectiveProgress.RequiredAmount == requiredAmount)
            {
                baselineValue = objectiveProgress.BaselineValue;
                return true;
            }
        }

        return false;
    }

    private static string ResolveObjectiveTargetId(NpcQuestObjective objective)
    {
        if (objective == null)
            return string.Empty;

        if (objective.Type == NpcQuestObjectiveType.CollectItem)
            return objective.TargetItem != null ? (objective.TargetItem.ItemId ?? string.Empty).Trim() : string.Empty;

        if (objective.Type == NpcQuestObjectiveType.KillEnemy)
            return objective.TargetEnemy != null ? objective.TargetEnemy.EnemyType.ToString() : string.Empty;

        return string.Empty;
    }

    private static string ResolveNpcDisplayName(NpcDefinition npcDefinition)
    {
        return npcDefinition != null ? npcDefinition.DisplayName : string.Empty;
    }

    private static Sprite ResolveNpcPortrait(NpcDefinition npcDefinition)
    {
        return npcDefinition != null ? npcDefinition.Portrait : null;
    }

    private static string ResolveDetailNpcDisplayName(
        NpcQuestDefinition quest,
        PlayerQuestProgressStatus progressStatus)
    {
        return ResolveNpcDisplayName(ResolveDetailNpcDefinition(quest, progressStatus));
    }

    private static Sprite ResolveDetailNpcPortrait(
        NpcQuestDefinition quest,
        PlayerQuestProgressStatus progressStatus)
    {
        return ResolveNpcPortrait(ResolveDetailNpcDefinition(quest, progressStatus));
    }

    private static NpcDefinition ResolveDetailNpcDefinition(
        NpcQuestDefinition quest,
        PlayerQuestProgressStatus progressStatus)
    {
        if (quest == null)
            return null;

        NpcDefinition starterNpc = quest.PrimaryStarterNpc;
        NpcDefinition completionNpc = quest.PrimaryCompletionNpc;

        if (progressStatus == PlayerQuestProgressStatus.Accepted
            || progressStatus == PlayerQuestProgressStatus.Completed)
        {
            return completionNpc != null ? completionNpc : starterNpc;
        }

        return starterNpc != null ? starterNpc : completionNpc;
    }

    private static bool TryConsumeCollectRequirements(
        Dictionary<string, int> collectRequirements,
        PlayerCharacter player,
        PlayerSessionInventoryApplicationService inventorySession)
    {
        if (collectRequirements == null || collectRequirements.Count == 0)
            return true;

        if (player == null || inventorySession == null)
            return false;

        foreach (KeyValuePair<string, int> entry in collectRequirements)
        {
            int requiredAmount = Mathf.Max(0, entry.Value);
            if (requiredAmount <= 0)
                continue;

            if (!inventorySession.TryRemoveInventoryItem(player, entry.Key, requiredAmount))
                return false;
        }

        return true;
    }

    private static void MarkCompleted(PlayerQuestProgressEntry progress)
    {
        if (progress == null)
            return;

        progress.Status = PlayerQuestProgressStatus.Completed;
        progress.CompletionCount++;
    }

    private readonly struct NpcQuestProgressData
    {
        public IReadOnlyList<NpcQuestLogObjectiveProgressSnapshot> ObjectiveProgress { get; }
        public IReadOnlyList<NpcQuestLogRewardSnapshot> Rewards { get; }

        public NpcQuestProgressData(
            IReadOnlyList<NpcQuestLogObjectiveProgressSnapshot> objectiveProgress,
            IReadOnlyList<NpcQuestLogRewardSnapshot> rewards)
        {
            ObjectiveProgress = objectiveProgress ?? System.Array.Empty<NpcQuestLogObjectiveProgressSnapshot>();
            Rewards = rewards ?? System.Array.Empty<NpcQuestLogRewardSnapshot>();
        }
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private static KillTrackerSystem ResolveKillTracker()
    {
        KillTrackerSystem[] trackers = UnityEngine.Object.FindObjectsByType<KillTrackerSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (trackers == null || trackers.Length == 0)
            return null;

        return trackers[0];
    }
}

public sealed class NpcQuestActionResult
{
    public NpcQuestActionType ActionType { get; }
    public string QuestId { get; }
    public bool IsSuccess { get; }
    public string Message { get; }

    public NpcQuestActionResult(
        NpcQuestActionType actionType,
        string questId,
        bool isSuccess,
        string message)
    {
        ActionType = actionType;
        QuestId = questId;
        IsSuccess = isSuccess;
        Message = message;
    }

    public static NpcQuestActionResult Ok(
        NpcQuestActionType actionType,
        string questId,
        string message)
    {
        return new NpcQuestActionResult(actionType, questId, true, message);
    }

    public static NpcQuestActionResult Fail(
        NpcQuestActionType actionType,
        string questId,
        string message)
    {
        return new NpcQuestActionResult(actionType, questId, false, message);
    }
}
