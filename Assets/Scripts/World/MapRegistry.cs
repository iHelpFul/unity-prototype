using System;
using System.Collections.Generic;
using UnityEngine;

public class MapRegistry : MonoBehaviour
{
    [SerializeField] private List<MapSceneBinding> sceneBindings = new List<MapSceneBinding>();

    public static string NormalizeMapId(string rawMapId)
    {
        return string.IsNullOrWhiteSpace(rawMapId)
            ? string.Empty
            : rawMapId.Trim();
    }

    public bool TryResolveSceneName(string mapId, out string sceneName)
    {
        string normalizedMapId = NormalizeMapId(mapId);
        if (string.IsNullOrEmpty(normalizedMapId))
        {
            sceneName = string.Empty;
            return false;
        }

        foreach (MapSceneBinding binding in sceneBindings)
        {
            if (!string.Equals(NormalizeMapId(binding.MapId), normalizedMapId, StringComparison.OrdinalIgnoreCase))
                continue;

            sceneName = string.IsNullOrWhiteSpace(binding.SceneName)
                ? normalizedMapId
                : binding.SceneName.Trim();
            return true;
        }

        sceneName = string.Empty;
        return false;
    }

    public static bool TryResolveSceneNameFromLoadedRegistries(string mapId, out string sceneName)
    {
        string normalizedMapId = NormalizeMapId(mapId);
        if (string.IsNullOrEmpty(normalizedMapId))
        {
            sceneName = string.Empty;
            return false;
        }

        MapRegistry[] registries = FindObjectsByType<MapRegistry>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int index = 0; index < registries.Length; index++)
        {
            MapRegistry registry = registries[index];
            if (registry != null && registry.TryResolveSceneName(normalizedMapId, out sceneName))
                return true;
        }

        sceneName = normalizedMapId;
        return true;
    }

    [Serializable]
    private struct MapSceneBinding
    {
        public string MapId;
        public string SceneName;
    }
}
