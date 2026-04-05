using System;
using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    public const string DefaultSpawnId = "default";

    [SerializeField] private string spawnId = DefaultSpawnId;
    [SerializeField] private bool isDefaultSpawn = true;

    public string SpawnId => NormalizeSpawnId(spawnId);
    public bool IsDefaultSpawn => isDefaultSpawn;

    public static string NormalizeSpawnId(string rawSpawnId)
    {
        return string.IsNullOrWhiteSpace(rawSpawnId)
            ? DefaultSpawnId
            : rawSpawnId.Trim();
    }

    public static SceneSpawnPoint FindById(string spawnId)
    {
        string normalizedSpawnId = NormalizeSpawnId(spawnId);
        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        SceneSpawnPoint fallback = null;

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            if (fallback == null && spawnPoint.IsDefaultSpawn)
                fallback = spawnPoint;

            if (string.Equals(spawnPoint.SpawnId, normalizedSpawnId, StringComparison.OrdinalIgnoreCase))
                return spawnPoint;
        }

        return normalizedSpawnId == DefaultSpawnId ? fallback : null;
    }

    public static SceneSpawnPoint FindDefault()
    {
        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && spawnPoint.IsDefaultSpawn)
                return spawnPoint;
        }

        return spawnPoints.Length > 0 ? spawnPoints[0] : null;
    }
}
