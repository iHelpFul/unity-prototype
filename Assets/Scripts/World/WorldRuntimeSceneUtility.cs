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
        bootstrap?.SetPendingMapTransition(targetMapId, targetSpawnId);
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

        bootstrap?.CompleteMapTransition(targetMapId, targetSpawnId);
        bootstrap?.PublishSessionState(player);

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
}
