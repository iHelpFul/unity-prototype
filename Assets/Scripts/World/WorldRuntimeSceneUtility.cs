using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldRuntimeSceneUtility
{
    public static bool TryResolveSceneName(string mapId, out string sceneName)
    {
        string normalizedMapId = MapRegistry.NormalizeMapId(mapId);
        if (string.IsNullOrEmpty(normalizedMapId))
        {
            sceneName = string.Empty;
            return false;
        }

        if (MapRegistry.TryResolveSceneNameFromLoadedRegistries(normalizedMapId, out sceneName))
            return !string.IsNullOrWhiteSpace(sceneName);

        sceneName = normalizedMapId;
        return true;
    }

    public static PlayerCharacter FindLocalPlayer(FindObjectsInactive includeInactive)
    {
        PlayerCharacter[] players = Object.FindObjectsByType<PlayerCharacter>(
            includeInactive,
            FindObjectsSortMode.None);

        for (int index = 0; index < players.Length; index++)
        {
            PlayerCharacter player = players[index];
            if (player != null && player.IsLocalPlayer)
                return player;
        }

        return null;
    }

    public static bool TryPlacePlayerAtSpawn(PlayerCharacter player, string spawnId)
    {
        if (player == null)
            return false;

        SceneSpawnPoint spawnPoint = SceneSpawnPoint.FindById(spawnId);
        if (spawnPoint == null)
            spawnPoint = SceneSpawnPoint.FindDefault();

        if (spawnPoint == null)
            return false;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null && controller.enabled)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);

        if (controller != null)
            controller.enabled = true;

        return true;
    }

    public static void BeginSceneTransition(
        GameBootstrap bootstrap,
        PlayerCharacter requester,
        string sourcePortalId,
        string targetMapId,
        string targetSpawnId)
    {
        bootstrap?.MapSession?.SetPendingMapTransition(targetMapId, targetSpawnId);
        string characterId = PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, requester);

        EventBus.Publish(new MapTransitionStartedEvent
        {
            Requester = requester,
            CharacterId = characterId,
            SourcePortalId = sourcePortalId ?? string.Empty,
            TargetMapId = targetMapId,
            TargetSpawnId = targetSpawnId
        });
    }

    public static IEnumerator WaitForSceneLoadAndSettle(AsyncOperation loadOperation, float settleTime)
    {
        while (!loadOperation.isDone)
            yield return null;

        if (settleTime > 0f)
            yield return new WaitForSecondsRealtime(settleTime);
        else
            yield return null;
    }

    public static PlayerCharacter CompleteSceneTransition(
        GameBootstrap bootstrap,
        string sceneName,
        string targetMapId,
        string targetSpawnId,
        FindObjectsInactive includeInactive,
        bool logWarnings,
        string warningContext = "WorldRuntimeSceneUtility")
    {
        PlayerCharacter player = FindLocalPlayer(includeInactive);
        string characterId = PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, player);
        if (player != null)
        {
            BindSceneRuntimeContext(bootstrap, player, includeInactive);

            if (!TryPlacePlayerAtSpawn(player, targetSpawnId) && logWarnings)
            {
                Debug.LogWarning(
                    $"{warningContext} could not find spawn '{targetSpawnId}' in scene '{SceneManager.GetActiveScene().name}'.");
            }
        }
        else if (logWarnings)
        {
            Debug.LogWarning($"{warningContext} could not find a local PlayerCharacter in the loaded scene.");
        }

        bootstrap?.MapSession?.CompleteMapTransition(targetMapId, targetSpawnId);
        bootstrap?.CharacterSession?.PublishSessionState(player);

        EventBus.Publish(new MapTransitionCompletedEvent
        {
            Player = player,
            CharacterId = characterId,
            TargetMapId = targetMapId,
            TargetSpawnId = targetSpawnId,
            SceneName = sceneName
        });

        return player;
    }

    public static void BindSceneRuntimeContext(
        GameBootstrap bootstrap,
        PlayerCharacter player,
        FindObjectsInactive includeInactive)
    {
        if (bootstrap == null)
            return;

        PlayerCharacter localPlayer = player;
        if (localPlayer == null || !localPlayer.IsLocalPlayer)
            localPlayer = FindLocalPlayer(includeInactive);

        if (localPlayer != null && localPlayer.IsLocalPlayer)
            BindLocalPlayerSession(bootstrap, localPlayer);

        BindSceneHudControllers(bootstrap, localPlayer, includeInactive);
    }

    public static void BindLocalPlayerSession(GameBootstrap bootstrap, PlayerCharacter player)
    {
        if (bootstrap == null || player == null || !player.IsLocalPlayer)
            return;

        player.BindBootstrap(bootstrap);

        PlayerFacade facade = player.GetComponent<PlayerFacade>();
        if (facade != null)
            facade.BindBootstrap(bootstrap);

        PlayerAppearanceController appearanceController = player.GetComponent<PlayerAppearanceController>();
        if (appearanceController != null)
            appearanceController.BindBootstrap(bootstrap);

        NetworkPlayerPrototypeAvatar networkAvatar = player.GetComponent<NetworkPlayerPrototypeAvatar>();
        if (networkAvatar != null)
            networkAvatar.BindBootstrap(bootstrap);
    }

    private static void BindSceneHudControllers(
        GameBootstrap bootstrap,
        PlayerCharacter player,
        FindObjectsInactive includeInactive)
    {
        PlayerHUDController[] hudControllers = Object.FindObjectsByType<PlayerHUDController>(
            includeInactive,
            FindObjectsSortMode.None);

        for (int index = 0; index < hudControllers.Length; index++)
        {
            PlayerHUDController hudController = hudControllers[index];
            if (hudController != null)
                hudController.BindRuntimeContext(bootstrap, player);
        }

        PlayerInventoryPanelController[] inventoryPanels = Object.FindObjectsByType<PlayerInventoryPanelController>(
            includeInactive,
            FindObjectsSortMode.None);

        for (int index = 0; index < inventoryPanels.Length; index++)
        {
            PlayerInventoryPanelController inventoryPanel = inventoryPanels[index];
            if (inventoryPanel != null)
                inventoryPanel.BindRuntimeContext(bootstrap, player);
        }

        GameplayNotificationFeedController[] notificationFeeds = Object.FindObjectsByType<GameplayNotificationFeedController>(
            includeInactive,
            FindObjectsSortMode.None);

        for (int index = 0; index < notificationFeeds.Length; index++)
        {
            GameplayNotificationFeedController notificationFeed = notificationFeeds[index];
            if (notificationFeed != null)
                notificationFeed.BindTrackedPlayer(player);
        }
    }
}
