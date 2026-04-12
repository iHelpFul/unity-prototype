using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-969)]
public class NpcJobAdvancementService : MonoBehaviour
{
    [SerializeField] private GameBootstrap bootstrap;

    private NpcInteractable activeNpc;
    private NpcJobAdvancement activeAdvancement;
    private PlayerCharacter activePlayer;
    private string activeCharacterId;

    public bool IsAdvancementOpen => activeNpc != null && activeAdvancement != null && activePlayer != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        NpcJobAdvancementService[] services = FindObjectsByType<NpcJobAdvancementService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (services.Length > 0)
            return;

        GameObject serviceObject = new GameObject("[NpcJobAdvancementService]");
        serviceObject.AddComponent<NpcJobAdvancementService>();
    }

    private void Awake()
    {
        if (ShouldDestroyDuplicate())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        ResolveBootstrap();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcInteractionRequestEvent>(OnNpcInteractionRequested);
        EventBus.Subscribe<NpcJobAdvancementSelectRequestEvent>(OnSelectRequested);
        EventBus.Subscribe<NpcJobAdvancementCloseRequestEvent>(OnCloseRequested);
        EventBus.Subscribe<MapTransitionStartedEvent>(OnMapTransitionStarted);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcInteractionRequestEvent>(OnNpcInteractionRequested);
        EventBus.Unsubscribe<NpcJobAdvancementSelectRequestEvent>(OnSelectRequested);
        EventBus.Unsubscribe<NpcJobAdvancementCloseRequestEvent>(OnCloseRequested);
        EventBus.Unsubscribe<MapTransitionStartedEvent>(OnMapTransitionStarted);
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        ResetActiveState();
    }

    private void OnNpcInteractionRequested(NpcInteractionRequestEvent e)
    {
        if (e.Requester == null || !e.Requester.IsLocalPlayer || e.Npc == null)
            return;

        NpcJobAdvancement advancement = e.Npc.GetComponent<NpcJobAdvancement>();
        if (advancement == null)
            return;

        if (IsSameActiveWindow(e.Requester, e.CharacterId, e.Npc))
        {
            CloseAdvancement();
            return;
        }

        OpenAdvancement(e.Requester, e.CharacterId, e.Npc, advancement);
    }

    private void OnSelectRequested(NpcJobAdvancementSelectRequestEvent e)
    {
        if (!ValidateActiveRequest(e.Requester, e.CharacterId, e.NpcId))
            return;

        if (!activeAdvancement.OffersJob(e.TargetJob))
        {
            PublishResult(e.TargetJob, false, "That job is not offered here.");
            return;
        }

        ResolveBootstrap();
        PlayerSessionJobApplicationService jobSession = bootstrap != null ? bootstrap.JobSession : null;
        if (jobSession == null)
        {
            PublishResult(e.TargetJob, false, "Player session is not ready.");
            return;
        }

        if (!jobSession.TryAdvanceToJob(activePlayer, e.TargetJob, activeAdvancement.RequiredLevel, out string resultMessage))
        {
            PublishResult(e.TargetJob, false, resultMessage);
            PublishAdvancementStateChanged();
            return;
        }

        PublishResult(e.TargetJob, true, resultMessage);
        PublishJobAdvancedNotification(e.TargetJob);
        PublishAdvancementStateChanged();
        CloseAdvancement();
    }

    private void OnCloseRequested(NpcJobAdvancementCloseRequestEvent e)
    {
        if (!ValidateActiveRequest(e.Requester, e.CharacterId, e.NpcId))
            return;

        CloseAdvancement();
    }

    private void OnMapTransitionStarted(MapTransitionStartedEvent e)
    {
        if (IsAdvancementOpen && MatchesActivePlayer(e.Requester, e.CharacterId))
            CloseAdvancement();
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        if (IsAdvancementOpen && MatchesActivePlayer(e.Target, e.CharacterId))
            CloseAdvancement();
    }

    private void OpenAdvancement(PlayerCharacter player, string characterId, NpcInteractable npc, NpcJobAdvancement advancement)
    {
        activePlayer = player;
        activeCharacterId = ResolveCharacterId(player, characterId);
        activeNpc = npc;
        activeAdvancement = advancement;

        NpcJobAdvancementSnapshot snapshot = BuildSnapshot();
        if (snapshot == null)
        {
            ResetActiveState();
            return;
        }

        EventBus.Publish(new NpcJobAdvancementOpenedEvent
        {
            Snapshot = snapshot
        });
    }

    private void CloseAdvancement()
    {
        if (!IsAdvancementOpen)
            return;

        EventBus.Publish(new NpcJobAdvancementClosedEvent
        {
            Player = activePlayer,
            CharacterId = activeCharacterId,
            NpcId = activeNpc != null ? activeNpc.NpcId : string.Empty
        });

        ResetActiveState();
    }

    private void PublishAdvancementStateChanged()
    {
        NpcJobAdvancementSnapshot snapshot = BuildSnapshot();
        if (snapshot == null)
            return;

        EventBus.Publish(new NpcJobAdvancementOpenedEvent
        {
            Snapshot = snapshot
        });
    }

    private NpcJobAdvancementSnapshot BuildSnapshot()
    {
        if (!IsAdvancementOpen)
            return null;

        ResolveBootstrap();
        PlayerSessionJobApplicationService jobSession = bootstrap != null ? bootstrap.JobSession : null;
        if (jobSession == null || !jobSession.HasActivePlayerData)
            return null;

        List<NpcJobAdvancementOption> options = new List<NpcJobAdvancementOption>();
        bool canAdvanceAtAll = false;
        string statusMessage = string.Empty;

        foreach (PlayerJobType jobType in activeAdvancement.OfferedJobs)
        {
            bool isAvailable = jobSession.CanAdvanceToJob(
                jobType,
                activeAdvancement.RequiredLevel,
                out string optionMessage);

            if (string.IsNullOrWhiteSpace(statusMessage))
                statusMessage = optionMessage;

            if (isAvailable)
                canAdvanceAtAll = true;

            options.Add(new NpcJobAdvancementOption(
                jobType,
                GameBootstrap.FormatJobName(jobType),
                isAvailable));
        }

        if (canAdvanceAtAll)
            statusMessage = "Choose your first job.";
        else if (string.IsNullOrWhiteSpace(statusMessage))
            statusMessage = $"Reach level {activeAdvancement.RequiredLevel} as a Novice first.";

        return new NpcJobAdvancementSnapshot(
            activePlayer,
            activeCharacterId,
            activeNpc.NpcId,
            activeNpc.DisplayName,
            activeAdvancement.RequiredLevel,
            activePlayer.CurrentJob,
            canAdvanceAtAll,
            statusMessage,
            options);
    }

    private void PublishResult(PlayerJobType targetJob, bool isSuccess, string message)
    {
        EventBus.Publish(new NpcJobAdvancementResultEvent
        {
            Player = activePlayer,
            CharacterId = activeCharacterId,
            NpcId = activeNpc != null ? activeNpc.NpcId : string.Empty,
            TargetJob = targetJob,
            IsSuccess = isSuccess,
            Message = message
        });
    }

    private void PublishJobAdvancedNotification(PlayerJobType targetJob)
    {
        if (activePlayer == null)
            return;

        EventBus.Publish(new GameplayNotificationEvent
        {
            Target = activePlayer,
            CharacterId = activeCharacterId,
            Category = GameplayNotificationCategory.Item,
            Message = $"Job Advanced: {GameBootstrap.FormatJobName(targetJob)}"
        });
    }

    private bool ValidateActiveRequest(PlayerCharacter requester, string characterId, string npcId)
    {
        if (!IsAdvancementOpen)
            return false;

        if (requester != null && !requester.IsLocalPlayer)
            return false;

        if (!MatchesActivePlayer(requester, characterId))
            return false;

        if (!string.IsNullOrWhiteSpace(npcId) && npcId.Trim() != activeNpc.NpcId)
            return false;

        return true;
    }

    private bool IsSameActiveWindow(PlayerCharacter requester, string characterId, NpcInteractable npc)
    {
        return IsAdvancementOpen && MatchesActivePlayer(requester, characterId) && npc == activeNpc;
    }

    private void ResolveBootstrap()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
    }

    private void ResetActiveState()
    {
        activeNpc = null;
        activeAdvancement = null;
        activePlayer = null;
        activeCharacterId = string.Empty;
    }

    private bool ShouldDestroyDuplicate()
    {
        NpcJobAdvancementService[] services = FindObjectsByType<NpcJobAdvancementService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        NpcJobAdvancementService keeper = this;

        foreach (NpcJobAdvancementService service in services)
        {
            if (service == null)
                continue;

            if (service.GetInstanceID() < keeper.GetInstanceID())
                keeper = service;
        }

        return keeper != this;
    }

    private bool MatchesActivePlayer(PlayerCharacter player, string characterId)
    {
        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            activePlayer,
            activeCharacterId,
            player,
            characterId);
    }

    private static string ResolveCharacterId(PlayerCharacter player, string characterId)
    {
        if (player != null)
            return PlayerRuntimeIdentityUtility.NormalizeCharacterId(player.CharacterId);

        return PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
    }
}
