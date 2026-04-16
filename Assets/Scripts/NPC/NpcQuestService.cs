using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-968)]
public class NpcQuestService : MonoBehaviour
{
    [SerializeField] private GameBootstrap bootstrap;
    private readonly NpcQuestActionProcessor actionProcessor = new NpcQuestActionProcessor();

    private NpcInteractable activeNpc;
    private NpcQuestProvider activeQuestProvider;
    private PlayerCharacter activePlayer;
    private string activeCharacterId;
    private bool isQuestLogOpen;
    private PlayerCharacter activeLogPlayer;
    private string activeLogCharacterId;

    public bool IsQuestWindowOpen => activeNpc != null && activeQuestProvider != null && activePlayer != null;
    public bool IsQuestLogOpen => isQuestLogOpen;

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
    }

    private void Awake()
    {
        if (ShouldDestroyDuplicate())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (bootstrap != null)
            BindBootstrap(bootstrap);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcInteractionRequestEvent>(OnNpcInteractionRequested);
        EventBus.Subscribe<NpcQuestCloseRequestEvent>(OnQuestCloseRequested);
        EventBus.Subscribe<NpcQuestAcceptRequestEvent>(OnQuestAcceptRequested);
        EventBus.Subscribe<NpcQuestCompleteRequestEvent>(OnQuestCompleteRequested);
        EventBus.Subscribe<NpcQuestLogRequestEvent>(OnQuestLogRequested);
        EventBus.Subscribe<NpcQuestLogRefreshRequestEvent>(OnQuestLogRefreshRequested);
        EventBus.Subscribe<NpcQuestLogCloseRequestEvent>(OnQuestLogCloseRequested);
        EventBus.Subscribe<MapTransitionStartedEvent>(OnMapTransitionStarted);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcInteractionRequestEvent>(OnNpcInteractionRequested);
        EventBus.Unsubscribe<NpcQuestCloseRequestEvent>(OnQuestCloseRequested);
        EventBus.Unsubscribe<NpcQuestAcceptRequestEvent>(OnQuestAcceptRequested);
        EventBus.Unsubscribe<NpcQuestCompleteRequestEvent>(OnQuestCompleteRequested);
        EventBus.Unsubscribe<NpcQuestLogRequestEvent>(OnQuestLogRequested);
        EventBus.Unsubscribe<NpcQuestLogRefreshRequestEvent>(OnQuestLogRefreshRequested);
        EventBus.Unsubscribe<NpcQuestLogCloseRequestEvent>(OnQuestLogCloseRequested);
        EventBus.Unsubscribe<MapTransitionStartedEvent>(OnMapTransitionStarted);
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        ResetActiveQuest();
        CloseQuestLogWindow();
    }

    private void OnNpcInteractionRequested(NpcInteractionRequestEvent e)
    {
        if (e.Requester == null || !e.Requester.IsLocalPlayer || e.Npc == null)
            return;

        ResolveBootstrap();

        NpcQuestProvider questProvider = e.Npc.GetComponent<NpcQuestProvider>();
        if (questProvider == null)
            return;

        if (IsSameActiveQuestWindow(e.Requester, e.CharacterId, e.Npc))
        {
            CloseQuestWindow();
            return;
        }

        OpenQuestWindow(e.Requester, e.CharacterId, e.Npc, questProvider);
    }

    private void OnQuestCloseRequested(NpcQuestCloseRequestEvent e)
    {
        if (!ValidateActiveQuestRequest(e.Requester, e.CharacterId, e.NpcId))
            return;

        CloseQuestWindow();
    }

    private void OnQuestAcceptRequested(NpcQuestAcceptRequestEvent e)
    {
        if (!ValidateQuestActionRequest(e.Requester, e.CharacterId, e.NpcId, e.QuestId, out NpcQuestDefinition quest))
            return;

        ResolveBootstrap();

        NpcQuestActionResult result = actionProcessor.ProcessAction(
            NpcQuestActionType.Accept,
            e.Requester,
            ResolveActivePlayerData(),
            quest,
            bootstrap != null ? bootstrap.InventorySession : null,
            bootstrap != null ? bootstrap.CurrencySession : null,
            bootstrap);

        PublishQuestActionResult(result);
        if (result != null && result.IsSuccess)
            CloseQuestWindow();
    }

    private void OnQuestCompleteRequested(NpcQuestCompleteRequestEvent e)
    {
        if (!ValidateQuestActionRequest(e.Requester, e.CharacterId, e.NpcId, e.QuestId, out NpcQuestDefinition quest))
            return;

        ResolveBootstrap();

        NpcQuestActionResult result = actionProcessor.ProcessAction(
            NpcQuestActionType.Complete,
            e.Requester,
            ResolveActivePlayerData(),
            quest,
            bootstrap != null ? bootstrap.InventorySession : null,
            bootstrap != null ? bootstrap.CurrencySession : null,
            bootstrap);

        PublishQuestActionResult(result);
    }

    private void OnQuestLogRequested(NpcQuestLogRequestEvent e)
    {
        if (!ValidatePlayerRequestForLog(e.Player, e.CharacterId))
            return;

        OpenQuestLogWindow(e.Player, e.CharacterId);
    }

    private void OnQuestLogRefreshRequested(NpcQuestLogRefreshRequestEvent e)
    {
        if (!IsQuestLogOpen || !MatchesActiveLogPlayer(e.Player, e.CharacterId))
            return;

        PublishQuestLogSnapshot();
    }

    private void OnQuestLogCloseRequested(NpcQuestLogCloseRequestEvent e)
    {
        if (!IsQuestLogOpen || !MatchesActiveLogPlayer(e.Requester, e.CharacterId))
            return;

        CloseQuestLogWindow();
    }

    private void OnMapTransitionStarted(MapTransitionStartedEvent e)
    {
        if (IsQuestWindowOpen && MatchesActivePlayer(e.Requester, e.CharacterId))
            CloseQuestWindow();

        if (IsQuestLogOpen && MatchesActiveLogPlayer(e.Requester, e.CharacterId))
            CloseQuestLogWindow();
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        if (IsQuestWindowOpen && MatchesActivePlayer(e.Target, e.CharacterId))
            CloseQuestWindow();

        if (IsQuestLogOpen && MatchesActiveLogPlayer(e.Target, e.CharacterId))
            CloseQuestLogWindow();
    }

    private void OpenQuestWindow(
        PlayerCharacter player,
        string characterId,
        NpcInteractable npc,
        NpcQuestProvider questProvider)
    {
        activePlayer = player;
        activeCharacterId = ResolveCharacterId(player, characterId);
        activeNpc = npc;
        activeQuestProvider = questProvider;

        NpcQuestPromptSnapshot snapshot = BuildSnapshot();
        if (snapshot == null)
        {
            ResetActiveQuest();
            return;
        }

        EventBus.Publish(new NpcQuestOpenedEvent
        {
            Snapshot = snapshot
        });
    }

    private void CloseQuestWindow()
    {
        if (!IsQuestWindowOpen)
            return;

        EventBus.Publish(new NpcQuestClosedEvent
        {
            Player = activePlayer,
            CharacterId = activeCharacterId,
            NpcId = activeNpc != null ? activeNpc.NpcId : string.Empty
        });

        ResetActiveQuest();
    }

    private void OpenQuestLogWindow(PlayerCharacter player, string characterId)
    {
        activeLogPlayer = player;
        activeLogCharacterId = ResolveCharacterId(player, characterId);
        isQuestLogOpen = true;

        EventBus.Publish(new NpcQuestLogOpenedEvent
        {
            Snapshot = BuildQuestLogSnapshot()
        });

        PublishQuestLogSnapshot();
    }

    private void CloseQuestLogWindow()
    {
        if (!IsQuestLogOpen)
            return;

        EventBus.Publish(new NpcQuestLogClosedEvent
        {
            Player = activeLogPlayer,
            CharacterId = activeLogCharacterId
        });

        activeLogPlayer = null;
        activeLogCharacterId = string.Empty;
        isQuestLogOpen = false;
    }

    private void PublishQuestLogSnapshot()
    {
        if (!IsQuestLogOpen)
            return;

        EventBus.Publish(new NpcQuestLogRefreshEvent
        {
            Snapshot = BuildQuestLogSnapshot()
        });
    }

    private NpcQuestLogSnapshot BuildQuestLogSnapshot()
    {
        PlayerRuntimeData playerData = ResolveRuntimeDataForActiveLogPlayer();
        List<NpcQuestDefinition> questDefinitions = CollectQuestDefinitionsFromScene();

        return actionProcessor.BuildQuestLogSnapshot(
            activeLogPlayer,
            playerData,
            questDefinitions,
            bootstrap != null ? bootstrap.InventorySession : null);
    }

    private NpcQuestPromptSnapshot BuildSnapshot()
    {
        if (!IsQuestWindowOpen || activeQuestProvider == null || activeNpc == null)
            return null;

        IReadOnlyList<NpcQuestDefinition> availableQuests = activeQuestProvider.Quests;
        if (availableQuests == null || availableQuests.Count == 0)
            return null;

        PlayerRuntimeData playerData = ResolveActivePlayerData();
        List<NpcQuestPromptEntry> entries = new List<NpcQuestPromptEntry>();
        for (int index = 0; index < availableQuests.Count; index++)
        {
            NpcQuestDefinition quest = availableQuests[index];
            if (quest == null)
                continue;

            if (!CanShowQuest(quest, playerData, out PlayerQuestProgressEntry progress))
                continue;

            entries.Add(new NpcQuestPromptEntry(
                quest.QuestId,
                quest.QuestTitle,
                quest.IsNarrativeOnly,
                progress != null ? progress.Status : PlayerQuestProgressStatus.None,
                quest.Repeatable,
                quest.MinimumPlayerLevel,
                quest.IntroPages,
                quest.InProgressPages,
                quest.CompletionPages));
        }

        if (entries.Count == 0)
            return null;

        return new NpcQuestPromptSnapshot(
            activePlayer,
            activeCharacterId,
            activeNpc.NpcId,
            activeNpc.DisplayName,
            entries.Count > 1,
            entries);
    }

    private bool CanShowQuest(
        NpcQuestDefinition quest,
        PlayerRuntimeData playerData,
        out PlayerQuestProgressEntry progress)
    {
        progress = null;

        if (quest == null)
            return false;

        string activeNpcId = activeNpc != null ? activeNpc.NpcId : string.Empty;

        if (playerData == null || playerData.QuestProgress == null)
            return quest.IsStarterNpc(activeNpcId);

        progress = FindProgress(playerData, quest.QuestId);
        if (progress == null)
            return quest.IsStarterNpc(activeNpcId);

        if (progress.Status == PlayerQuestProgressStatus.Accepted)
            return quest.IsCompletionNpc(activeNpcId);

        if (progress.Status == PlayerQuestProgressStatus.Completed)
            return quest.Repeatable && quest.IsStarterNpc(activeNpcId);

        return true;
    }

    private PlayerQuestProgressEntry FindProgress(PlayerRuntimeData playerData, string questId)
    {
        if (playerData == null || playerData.QuestProgress == null || string.IsNullOrWhiteSpace(questId))
            return null;

        for (int index = 0; index < playerData.QuestProgress.Count; index++)
        {
            PlayerQuestProgressEntry candidate = playerData.QuestProgress[index];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.QuestId))
                continue;

            if (candidate.QuestId == questId.Trim())
                return candidate;
        }

        return null;
    }

    private bool ValidateActiveQuestRequest(PlayerCharacter requester, string characterId, string npcId)
    {
        if (!IsQuestWindowOpen)
            return false;

        if (requester != null && !requester.IsLocalPlayer)
            return false;

        if (!MatchesActivePlayer(requester, characterId))
            return false;

        return string.IsNullOrWhiteSpace(npcId) || npcId.Trim() == activeNpc.NpcId;
    }

    private bool ValidateQuestActionRequest(
        PlayerCharacter requester,
        string characterId,
        string npcId,
        string questId,
        out NpcQuestDefinition quest)
    {
        quest = null;

        if (!ValidateActiveQuestRequest(requester, characterId, npcId))
            return false;

        if (activeQuestProvider == null)
            return false;

        if (!activeQuestProvider.TryGetQuest(questId, out quest))
            return false;

        return quest != null;
    }

    private bool IsSameActiveQuestWindow(PlayerCharacter requester, string characterId, NpcInteractable npc)
    {
        return IsQuestWindowOpen
            && MatchesActivePlayer(requester, characterId)
            && npc == activeNpc;
    }

    private bool MatchesActivePlayer(PlayerCharacter player, string characterId)
    {
        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            activePlayer,
            activeCharacterId,
            player,
            characterId);
    }

    private bool MatchesActiveLogPlayer(PlayerCharacter player, string characterId)
    {
        if (!IsQuestLogOpen)
            return false;

        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            activeLogPlayer,
            activeLogCharacterId,
            player,
            characterId);
    }

    private bool ValidatePlayerRequestForLog(PlayerCharacter player, string characterId)
    {
        if (player == null || !player.IsLocalPlayer)
            return false;

        if (!IsQuestLogOpen)
            return true;

        return MatchesActiveLogPlayer(player, characterId);
    }

    private bool ShouldDestroyDuplicate()
    {
        NpcQuestService[] services = FindObjectsByType<NpcQuestService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        NpcQuestService keeper = this;
        foreach (NpcQuestService service in services)
        {
            if (service == null)
                continue;

            if (service.GetInstanceID() < keeper.GetInstanceID())
                keeper = service;
        }

        return keeper != this;
    }

    private void ResetActiveQuest()
    {
        activeNpc = null;
        activeQuestProvider = null;
        activePlayer = null;
        activeCharacterId = string.Empty;
    }

    private static List<NpcQuestDefinition> CollectQuestDefinitionsFromScene()
    {
        NpcQuestProvider[] questProviders = Object.FindObjectsByType<NpcQuestProvider>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        List<NpcQuestDefinition> questDefinitions = new List<NpcQuestDefinition>();
        if (questProviders == null || questProviders.Length == 0)
            return questDefinitions;

        HashSet<string> dedupe = new HashSet<string>();
        for (int index = 0; index < questProviders.Length; index++)
        {
            NpcQuestProvider provider = questProviders[index];
            if (provider == null || provider.Quests == null)
                continue;

            for (int questIndex = 0; questIndex < provider.Quests.Count; questIndex++)
            {
                NpcQuestDefinition quest = provider.Quests[questIndex];
                if (quest == null)
                    continue;

                string normalizedQuestId = NormalizeQuestId(quest.QuestId);
                if (string.IsNullOrWhiteSpace(normalizedQuestId))
                    continue;

                if (!dedupe.Add(normalizedQuestId))
                    continue;

                questDefinitions.Add(quest);
            }
        }

        return questDefinitions;
    }

    private static string NormalizeQuestId(string questId)
    {
        return (questId ?? string.Empty).Trim();
    }

    private static string ResolveCharacterId(PlayerCharacter player, string characterId)
    {
        if (player != null)
            return PlayerRuntimeIdentityUtility.NormalizeCharacterId(player.CharacterId);

        return PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
    }

    private PlayerRuntimeData ResolveActivePlayerData()
    {
        GameBootstrap resolvedBootstrap = ResolveBootstrap();
        if (resolvedBootstrap == null || resolvedBootstrap.CharacterSession == null)
            return ResolveRuntimeDataFromActivePlayer();

        return resolvedBootstrap.CharacterSession.PlayerData;
    }

    private PlayerRuntimeData ResolveRuntimeDataForActiveLogPlayer()
    {
        if (activeLogPlayer == null)
            return ResolveActivePlayerData();

        GameBootstrap resolvedBootstrap = ResolveBootstrap();
        if (resolvedBootstrap != null && resolvedBootstrap.CharacterSession != null)
            return resolvedBootstrap.CharacterSession.PlayerData;

        PlayerRuntimeStateController runtimeState = activeLogPlayer.GetComponent<PlayerRuntimeStateController>();
        return runtimeState != null ? runtimeState.RuntimeData : null;
    }

    private PlayerRuntimeData ResolveRuntimeDataFromActivePlayer()
    {
        if (activePlayer == null)
            return null;

        PlayerRuntimeStateController runtimeState = activePlayer.GetComponent<PlayerRuntimeStateController>();
        return runtimeState != null ? runtimeState.RuntimeData : null;
    }

    private GameBootstrap ResolveBootstrap()
    {
        if (bootstrap != null && bootstrap.CharacterSession != null)
        {
            return bootstrap;
        }

        GameBootstrap readyBootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
        if (readyBootstrap != null && readyBootstrap.CharacterSession != null)
        {
            bootstrap = readyBootstrap;
            return bootstrap;
        }

        return bootstrap;
    }

    private void PublishQuestActionResult(
        NpcQuestActionResult result)
    {
        if (result == null)
            return;

        EventBus.Publish(new NpcQuestActionResultEvent
        {
            Player = activePlayer,
            CharacterId = activeCharacterId,
            NpcId = activeNpc != null ? activeNpc.NpcId : string.Empty,
            QuestId = result.QuestId,
            ActionType = result.ActionType,
            IsSuccess = result.IsSuccess,
            Message = result.Message
        });
    }
}
