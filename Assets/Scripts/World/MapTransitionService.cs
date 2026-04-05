using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-980)]
public class MapTransitionService : MonoBehaviour
{
    [SerializeField] private float postLoadSettleTime = 0.05f;
    [SerializeField] private GameBootstrap bootstrap;

    private Coroutine activeTransitionRoutine;

    public bool IsTransitioning { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        MapTransitionService[] services = FindObjectsByType<MapTransitionService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (services.Length > 0)
            return;

        GameObject transitionObject = new GameObject("[MapTransitionService]");
        transitionObject.AddComponent<MapTransitionService>();
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
        EventBus.Subscribe<MapTransitionRequestEvent>(OnMapTransitionRequested);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MapTransitionRequestEvent>(OnMapTransitionRequested);
    }

    private void OnMapTransitionRequested(MapTransitionRequestEvent e)
    {
        PlayerCharacter requester = ResolveRequester(e.Requester, e.CharacterId);
        if (requester == null)
            return;

        RequestTransition(requester, e.TargetMapId, e.TargetSpawnId, e.SourcePortalId);
    }

    public bool RequestTransition(PlayerCharacter requester, string targetMapId, string targetSpawnId, string sourcePortalId = "")
    {
        if (IsTransitioning || requester == null || !requester.IsLocalPlayer)
            return false;

        if (!WorldRuntimeSceneUtility.TryResolveSceneName(targetMapId, out string sceneName))
        {
            Debug.LogWarning($"MapTransitionService could not resolve a scene for map '{targetMapId}'.");
            return false;
        }

        string normalizedMapId = MapRegistry.NormalizeMapId(targetMapId);
        string normalizedSpawnId = SceneSpawnPoint.NormalizeSpawnId(targetSpawnId);

        activeTransitionRoutine = StartCoroutine(TransitionRoutine(
            requester,
            sceneName,
            normalizedMapId,
            normalizedSpawnId,
            sourcePortalId));

        return true;
    }

    private IEnumerator TransitionRoutine(
        PlayerCharacter requester,
        string sceneName,
        string targetMapId,
        string targetSpawnId,
        string sourcePortalId)
    {
        IsTransitioning = true;

        ResolveBootstrap();
        WorldRuntimeSceneUtility.BeginSceneTransition(
            bootstrap,
            requester,
            sourcePortalId,
            targetMapId,
            targetSpawnId);
        SetPlayerInteractive(requester, false);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            Debug.LogWarning($"MapTransitionService failed to start loading scene '{sceneName}'.");
            bootstrap?.ClearPendingMapTransition();
            SetPlayerInteractive(requester, true);
            ResetTransitionState();
            yield break;
        }

        yield return WorldRuntimeSceneUtility.WaitForSceneLoadAndSettle(loadOperation, postLoadSettleTime);

        ResolveBootstrap();
        PlayerCharacter player = WorldRuntimeSceneUtility.CompleteSceneTransition(
            bootstrap,
            sceneName,
            targetMapId,
            targetSpawnId,
            FindObjectsInactive.Exclude,
            true,
            "MapTransitionService");

        SetPlayerInteractive(player, true);
        ResetTransitionState();
    }

    private void SetPlayerInteractive(PlayerCharacter player, bool enabled)
    {
        if (player == null)
            return;

        PlayerInput playerInput = player.GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = enabled;

        PlayerInputAdapter inputAdapter = player.GetComponent<PlayerInputAdapter>();
        if (inputAdapter != null)
            inputAdapter.enabled = enabled;

        PlayerFacade playerFacade = player.GetComponent<PlayerFacade>();
        if (playerFacade != null)
            playerFacade.enabled = enabled;
    }

    private void ResolveBootstrap()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
    }

    private PlayerCharacter ResolveRequester(PlayerCharacter requester, string characterId)
    {
        if (requester != null && requester.IsLocalPlayer)
            return requester;

        PlayerCharacter localPlayer = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
        if (localPlayer == null)
            return null;

        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            localPlayer,
            localPlayer.CharacterId,
            requester,
            characterId)
            ? localPlayer
            : null;
    }

    private bool ShouldDestroyDuplicate()
    {
        MapTransitionService[] services = FindObjectsByType<MapTransitionService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        MapTransitionService keeper = this;

        foreach (MapTransitionService service in services)
        {
            if (service == null)
                continue;

            if (service.GetInstanceID() < keeper.GetInstanceID())
                keeper = service;
        }

        return keeper != this;
    }

    private void ResetTransitionState()
    {
        IsTransitioning = false;
        activeTransitionRoutine = null;
    }
}
