using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MultiplayerPrototypeSetup
{
    private const string MapScenePath = "Assets/Scenes/Map_01.unity";
    private const string BootScenePath = "Assets/Scenes/Boot_MultiplayerPrototype.unity";
    private const string PrefabsFolderPath = "Assets/Prefabs";
    private const string PlayerPrefabPath = "Assets/Prefabs/Players/PlayerNetworkPrototype.prefab";

    [MenuItem("Tools/Multiplayer Prototype/Generate Prototype Assets")]
    public static void GeneratePrototypeAssets()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder(PrefabsFolderPath, "Players");

        bool wasMapSceneAlreadyOpen;
        Scene mapScene = GetOrOpenScene(MapScenePath, out wasMapSceneAlreadyOpen);

        try
        {
            ConfigureMapScene(mapScene);
            CreatePlayerPrefab(mapScene);
        }
        finally
        {
            if (!wasMapSceneAlreadyOpen && mapScene.IsValid())
                EditorSceneManager.CloseScene(mapScene, true);
        }

        CreateBootScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void ExecuteFromBatchmode()
    {
        GeneratePrototypeAssets();
    }

    private static void ConfigureMapScene(Scene mapScene)
    {
        PlayerCharacter scenePlayer = FindScenePlayer(mapScene);
        if (scenePlayer == null)
            throw new InvalidOperationException("Could not find the scene player in Map_01.");

        if (scenePlayer.GetComponent<MultiplayerPrototypeSceneAvatarSuppressor>() == null)
            scenePlayer.gameObject.AddComponent<MultiplayerPrototypeSceneAvatarSuppressor>();

        EditorSceneManager.MarkSceneDirty(mapScene);
        EditorSceneManager.SaveScene(mapScene);
    }

    private static void CreatePlayerPrefab(Scene mapScene)
    {
        PlayerCharacter scenePlayer = FindScenePlayer(mapScene);
        if (scenePlayer == null)
            throw new InvalidOperationException("Could not clone the scene player because it was not found.");

        GameObject clone = UnityEngine.Object.Instantiate(scenePlayer.gameObject);
        clone.name = "PlayerNetworkPrototype";

        MultiplayerPrototypeSceneAvatarSuppressor suppressor =
            clone.GetComponent<MultiplayerPrototypeSceneAvatarSuppressor>();
        if (suppressor != null)
            UnityEngine.Object.DestroyImmediate(suppressor);

        if (clone.GetComponent<NetworkObject>() == null)
            clone.AddComponent<NetworkObject>();

        NetworkTransform networkTransform = clone.GetComponent<NetworkTransform>();
        if (networkTransform == null)
            networkTransform = clone.AddComponent<NetworkTransform>();

        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        networkTransform.AutoOwnerAuthorityTickOffset = false;
        networkTransform.PositionInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
        networkTransform.RotationInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
        networkTransform.PositionLerpSmoothing = true;
        networkTransform.RotationLerpSmoothing = true;
        networkTransform.PositionMaxInterpolationTime = 0.035f;
        networkTransform.RotationMaxInterpolationTime = 0.02f;

        if (clone.GetComponent<NetworkPlayerPrototypeAvatar>() == null)
            clone.AddComponent<NetworkPlayerPrototypeAvatar>();

        ClearObjectReference(clone.GetComponent<PlayerCharacter>(), "bootstrap");
        ClearObjectReference(clone.GetComponent<PlayerFacade>(), "bootstrap");
        ClearObjectReference(clone.GetComponent<PlayerFacade>(), "cameraTransform");
        ClearObjectReference(clone.GetComponent<PlayerAppearanceController>(), "bootstrap");

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(clone, PlayerPrefabPath);
        UnityEngine.Object.DestroyImmediate(clone);

        if (savedPrefab == null)
            throw new InvalidOperationException("Unity could not save PlayerNetworkPrototype.prefab.");
    }

    private static void CreateBootScene()
    {
        Scene bootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(bootScene);

        GameObject bootstrapObject = new GameObject("[MultiplayerPrototypeBootstrap]");
        MultiplayerPrototypeBootstrap bootstrap = bootstrapObject.AddComponent<MultiplayerPrototypeBootstrap>();
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
            throw new InvalidOperationException("PlayerNetworkPrototype.prefab could not be loaded after creation.");

        SetObjectReference(bootstrap, "playerPrefab", playerPrefab);
        SetStringValue(bootstrap, "gameplaySceneName", "Map_01");
        SetStringValue(bootstrap, "hostAddress", MultiplayerPrototypeRuntime.DefaultAddress);
        SetIntValue(bootstrap, "hostPort", MultiplayerPrototypeRuntime.DefaultPort);

        CreateBootCamera();
        EditorSceneManager.MarkSceneDirty(bootScene);
        EditorSceneManager.SaveScene(bootScene, BootScenePath);
        EditorSceneManager.CloseScene(bootScene, true);
    }

    private static void CreateBootCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        EnsureBuildScene(scenes, BootScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureBuildScene(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        for (int index = 0; index < scenes.Count; index++)
        {
            if (string.Equals(scenes[index].path, scenePath, StringComparison.OrdinalIgnoreCase))
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    }

    private static Scene GetOrOpenScene(string scenePath, out bool wasAlreadyOpen)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        wasAlreadyOpen = scene.IsValid() && scene.isLoaded;
        if (wasAlreadyOpen)
            return scene;

        return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
    }

    private static PlayerCharacter FindScenePlayer(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] rootObjects = scene.GetRootGameObjects();
        for (int index = 0; index < rootObjects.Length; index++)
        {
            GameObject rootObject = rootObjects[index];
            if (rootObject == null)
                continue;

            PlayerCharacter player = rootObject.GetComponentInChildren<PlayerCharacter>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    private static void EnsureFolder(string parentFolderPath, string childFolderName)
    {
        string combinedPath = $"{parentFolderPath}/{childFolderName}";
        if (!AssetDatabase.IsValidFolder(combinedPath))
            AssetDatabase.CreateFolder(parentFolderPath, childFolderName);
    }

    private static void ClearObjectReference(UnityEngine.Object target, string propertyName)
    {
        if (target == null)
            return;

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        property.objectReferenceValue = null;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStringValue(UnityEngine.Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        property.stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetIntValue(UnityEngine.Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}

[InitializeOnLoad]
public static class MultiplayerPrototypeSetupAutoRunner
{
    private const string PendingRunKey = "MultiplayerPrototypeSetup.PendingRun";

    static MultiplayerPrototypeSetupAutoRunner()
    {
        if (Application.isBatchMode)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Players/PlayerNetworkPrototype.prefab") != null
            && AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot_MultiplayerPrototype.unity") != null)
        {
            SessionState.SetBool(PendingRunKey, false);
            return;
        }

        if (!SessionState.GetBool(PendingRunKey, true))
            return;

        EditorApplication.delayCall += TryGenerateWhenEditorIsReady;
    }

    private static void TryGenerateWhenEditorIsReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryGenerateWhenEditorIsReady;
            return;
        }

        if (!SessionState.GetBool(PendingRunKey, true))
            return;

        try
        {
            Debug.Log("Attempting to auto-generate multiplayer prototype assets.");
            MultiplayerPrototypeSetup.GeneratePrototypeAssets();
            Debug.Log("Multiplayer prototype assets generated successfully.");
            SessionState.SetBool(PendingRunKey, false);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to auto-generate multiplayer prototype assets: {exception}");
            SessionState.SetBool(PendingRunKey, false);
        }
    }
}
