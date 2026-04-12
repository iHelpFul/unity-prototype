using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneEntityIdTools
{
    private const string MapScenePath = "Assets/Scenes/Map_01.unity";
    private const string AssignMissingMenuPath = "Tools/Multiplayer Prototype/Scene Entity IDs/Assign Missing Enemy IDs";
    private const string ValidateMenuPath = "Tools/Multiplayer Prototype/Scene Entity IDs/Validate Enemy IDs";

    [MenuItem(AssignMissingMenuPath)]
    public static void AssignMissingEnemyIds()
    {
        bool wasAlreadyOpen;
        Scene mapScene = GetOrOpenScene(MapScenePath, out wasAlreadyOpen);

        try
        {
            EnemyHealth[] enemies = FindSceneEnemies(mapScene);
            HashSet<string> usedSceneIds = new HashSet<string>(StringComparer.Ordinal);

            int preservedValidIds = 0;
            int addedComponents = 0;
            int assignedIds = 0;
            int skippedInvalidIds = 0;

            for (int index = 0; index < enemies.Length; index++)
            {
                SceneEntityId existingEntityId = enemies[index] != null
                    ? enemies[index].GetComponent<SceneEntityId>()
                    : null;

                if (existingEntityId == null || !SceneEntityId.IsValidEnemySceneId(existingEntityId.SceneId))
                    continue;

                if (usedSceneIds.Add(existingEntityId.SceneId))
                    preservedValidIds++;
            }

            for (int index = 0; index < enemies.Length; index++)
            {
                EnemyHealth enemy = enemies[index];
                if (enemy == null)
                    continue;

                SceneEntityId entityId = enemy.GetComponent<SceneEntityId>();
                if (entityId == null)
                {
                    entityId = Undo.AddComponent<SceneEntityId>(enemy.gameObject);
                    addedComponents++;
                }

                if (SceneEntityId.IsValidEnemySceneId(entityId.SceneId))
                    continue;

                if (!string.IsNullOrWhiteSpace(entityId.SceneId))
                {
                    skippedInvalidIds++;
                    Debug.LogWarning(
                        $"[SceneEntityIdTools] Kept invalid SceneEntityId '{entityId.SceneId}' on '{SceneEntityIdValidationUtility.GetHierarchyPath(enemy.transform)}'. Run validation to fix it manually.",
                        enemy);
                    continue;
                }

                string generatedId = GenerateUniqueEnemySceneId(usedSceneIds);
                SetSceneId(entityId, generatedId);
                assignedIds++;
            }

            if (addedComponents > 0 || assignedIds > 0)
            {
                EditorSceneManager.MarkSceneDirty(mapScene);
                EditorSceneManager.SaveScene(mapScene);
            }

            ValidateSceneInternal(mapScene);

            Debug.Log(
                $"[SceneEntityIdTools] Assign Missing Enemy IDs completed for '{mapScene.name}'. Total enemies: {enemies.Length}, preserved valid IDs: {preservedValidIds}, added components: {addedComponents}, assigned IDs: {assignedIds}, skipped invalid existing IDs: {skippedInvalidIds}.");
        }
        finally
        {
            if (!wasAlreadyOpen && mapScene.IsValid())
                EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [MenuItem(ValidateMenuPath)]
    public static void ValidateEnemyIds()
    {
        bool wasAlreadyOpen;
        Scene mapScene = GetOrOpenScene(MapScenePath, out wasAlreadyOpen);

        try
        {
            ValidateSceneInternal(mapScene);
        }
        finally
        {
            if (!wasAlreadyOpen && mapScene.IsValid())
                EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static void ValidateSceneInternal(Scene scene)
    {
        List<SceneEntityEnemyRegistration> registrations = new List<SceneEntityEnemyRegistration>();
        List<string> errors = new List<string>();
        int totalEnemies = SceneEntityIdValidationUtility.CollectEnemyRegistrations(scene, registrations, errors);

        for (int index = 0; index < errors.Count; index++)
            Debug.LogError(errors[index]);

        if (errors.Count == 0)
        {
            Debug.Log(
                $"[SceneEntityIdTools] SceneEntityId validation passed for '{scene.name}'. Registered {registrations.Count}/{totalEnemies} enemies.");
            return;
        }

        Debug.LogError(
            $"[SceneEntityIdTools] SceneEntityId validation failed for '{scene.name}'. Registered {registrations.Count}/{totalEnemies} enemies. Errors: {errors.Count}.");
    }

    private static EnemyHealth[] FindSceneEnemies(Scene scene)
    {
        List<EnemyHealth> sceneEnemies = new List<EnemyHealth>();
        EnemyHealth[] allEnemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int index = 0; index < allEnemies.Length; index++)
        {
            EnemyHealth enemy = allEnemies[index];
            if (enemy == null || enemy.gameObject.scene.handle != scene.handle)
                continue;

            sceneEnemies.Add(enemy);
        }

        return sceneEnemies.ToArray();
    }

    private static Scene GetOrOpenScene(string scenePath, out bool wasAlreadyOpen)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        wasAlreadyOpen = scene.IsValid() && scene.isLoaded;
        if (wasAlreadyOpen)
            return scene;

        return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
    }

    private static string GenerateUniqueEnemySceneId(HashSet<string> usedSceneIds)
    {
        while (true)
        {
            string hex = Guid.NewGuid().ToString("N").Substring(0, SceneEntityId.EnemyHexLength).ToUpperInvariant();
            string candidate = $"{SceneEntityId.EnemyPrefix}{hex}";
            if (!SceneEntityId.IsValidEnemySceneId(candidate))
                continue;

            if (usedSceneIds.Add(candidate))
                return candidate;
        }
    }

    private static void SetSceneId(SceneEntityId entityId, string value)
    {
        SerializedObject serializedObject = new SerializedObject(entityId);
        SerializedProperty property = serializedObject.FindProperty("sceneId");
        property.stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(entityId);
    }
}
