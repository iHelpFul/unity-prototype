using UnityEditor;
using UnityEngine;

public static class GameDataAssetGenerator
{
    [MenuItem("Tools/Game Data/Generate Default Gameplay Data")]
    public static void GenerateDefaultGameplayData()
    {
        Debug.LogWarning(
            "GameDataAssetGenerator is disabled. " +
            "The old generation flow was removed because it no longer matches the current combat, job, skill, projectile, and presentation authoring model. " +
            "A new generator should be built later around the new data model.");
    }
}
