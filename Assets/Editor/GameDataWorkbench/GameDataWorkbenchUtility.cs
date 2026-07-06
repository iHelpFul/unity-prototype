using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class GameDataWorkbenchCategory
{
    public readonly string Name;
    public readonly string[] TypeNames;
    public readonly string[] SearchFolders;
    public readonly string Filter;
    public readonly bool IncludeSceneAssets;
    public readonly bool IncludeAssetLibrary;

    public GameDataWorkbenchCategory(
        string name,
        string[] typeNames,
        string[] searchFolders = null,
        string filter = null,
        bool includeSceneAssets = false,
        bool includeAssetLibrary = false)
    {
        Name = name;
        TypeNames = typeNames ?? Array.Empty<string>();
        SearchFolders = searchFolders ?? new[] { "Assets" };
        Filter = filter;
        IncludeSceneAssets = includeSceneAssets;
        IncludeAssetLibrary = includeAssetLibrary;
    }
}

internal sealed class GameDataWorkbenchAssetRecord
{
    public UnityEngine.Object Asset;
    public string Path;
    public Type AssetType;

    public string DisplayName
    {
        get
        {
            if (Asset == null)
                return "<Missing Asset>";

            return Asset.name;
        }
    }
}

internal sealed class GameDataWorkbenchReference
{
    public UnityEngine.Object Source;
    public UnityEngine.Object Target;
    public string SourcePath;
    public string PropertyPath;
}

internal sealed class GameDataWorkbenchHealthIssue
{
    public MessageType Severity;
    public string Title;
    public string Details;
    public UnityEngine.Object Context;
}

internal static class GameDataWorkbenchUtility
{
    private static readonly string[] CommonIdFields =
    {
        "skillId",
        "projectileId",
        "passiveId",
        "itemId",
        "questId",
        "npcId",
        "vendorId",
        "enemyId",
        "definitionId"
    };

    public static readonly GameDataWorkbenchCategory[] DefaultCategories =
    {
        new GameDataWorkbenchCategory(
            "Overview",
            Array.Empty<string>()),

        new GameDataWorkbenchCategory(
            "Jobs",
            new[] { "PlayerJobDefinition", "PlayerJobDatabaseAsset", "PlayerBasicAttackProfile", "AnimationProfile", "CombatFormulaProfile" },
            new[] { "Assets/Resources/GameData/Jobs", "Assets/Resources/GameData" }),

        new GameDataWorkbenchCategory(
            "Skills",
            new[] { "PlayerSkillDefinition", "PlayerSkillDatabaseAsset", "BurstLinkedSkillProfile", "UtilitySkillProfile", "PassiveDefinition", "GaugeScalingProfile" },
            new[] { "Assets/Resources/GameData/Jobs", "Assets/Resources/GameData/Skills", "Assets/Resources/GameData" }),

        new GameDataWorkbenchCategory(
            "Combat",
            new[] { "ProjectileProfile", "PresentationCueSet", "CombatElementRuleProfile", "VfxLibrary", "SfxLibrary" },
            new[] { "Assets/Resources/GameData" }),

        new GameDataWorkbenchCategory(
            "Enemies",
            new[] { "EnemyDefinition", "EnemyDefinitionDatabaseAsset", "CombatElementIconCatalog" },
            new[] { "Assets/Resources/GameData/Enemies", "Assets/Resources/GameData" }),

        new GameDataWorkbenchCategory(
            "Items",
            new[] { "ItemDefinition", "ItemDatabaseAsset" },
            new[] { "Assets/Resources/GameData/Items", "Assets/Resources/GameData" }),

        new GameDataWorkbenchCategory(
            "Quests / NPC",
            new[] { "NpcDefinition", "NpcQuestDefinition", "NpcVendorDefinition", "QuestUiIconCatalog", "RuntimeSpawnPrefabCatalog" },
            new[] { "Assets/Resources/GameData/NPC", "Assets/Resources/GameData/Vendors", "Assets/Resources/GameData/Runtime" }),

        new GameDataWorkbenchCategory(
            "Characters",
            new[] { "CharacterAppearanceCatalogAsset", "ProgressionProfile" },
            new[] { "Assets/Resources/GameData/Characters", "Assets/Resources/GameData" }),

        new GameDataWorkbenchCategory(
            "Asset Library",
            Array.Empty<string>(),
            new[] { "Assets" },
            includeAssetLibrary: true),

        new GameDataWorkbenchCategory(
            "Scenes",
            Array.Empty<string>(),
            new[] { "Assets/Scenes" },
            includeSceneAssets: true),

        new GameDataWorkbenchCategory(
            "Sandbox",
            Array.Empty<string>()),

        new GameDataWorkbenchCategory(
            "Health",
            Array.Empty<string>())
    };

    public static List<GameDataWorkbenchAssetRecord> LoadCategoryAssets(GameDataWorkbenchCategory category)
    {
        List<GameDataWorkbenchAssetRecord> records = new List<GameDataWorkbenchAssetRecord>();
        HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (category == null)
            return records;

        if (category.IncludeSceneAssets)
        {
            AddAssetsByFilter(records, seenPaths, "t:SceneAsset", category.SearchFolders);
        }

        if (category.IncludeAssetLibrary)
        {
            AddAssetsByFilter(records, seenPaths, "t:Prefab", new[] { "Assets/Prefabs", "Assets/VFX" });
            AddAssetsByFilter(records, seenPaths, "t:AudioClip", new[] { "Assets/SFX", "Assets/Audio" });
            AddAssetsByFilter(records, seenPaths, "t:Texture2D", new[] { "Assets/UI_Sprites", "Assets/Textures" });
            AddAssetsByFilter(records, seenPaths, "t:Material", new[] { "Assets/Materials", "Assets/VFX" });
        }

        if (!string.IsNullOrWhiteSpace(category.Filter))
            AddAssetsByFilter(records, seenPaths, category.Filter, category.SearchFolders);

        for (int index = 0; index < category.TypeNames.Length; index++)
        {
            Type type = ResolveType(category.TypeNames[index]);
            if (type == null)
                continue;

            AddAssetsByType(records, seenPaths, type, category.SearchFolders);
        }

        records.Sort((left, right) =>
        {
            int typeCompare = string.Compare(
                left.AssetType != null ? left.AssetType.Name : string.Empty,
                right.AssetType != null ? right.AssetType.Name : string.Empty,
                StringComparison.OrdinalIgnoreCase);
            if (typeCompare != 0)
                return typeCompare;

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        return records;
    }

    public static List<GameDataWorkbenchAssetRecord> LoadAllGameDataAssets()
    {
        GameDataWorkbenchCategory allGameData = new GameDataWorkbenchCategory(
            "All Game Data",
            GetAllKnownTypeNames(),
            new[] { "Assets/Resources/GameData" });

        return LoadCategoryAssets(allGameData);
    }

    public static Type ResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        Type directType = Type.GetType(typeName);
        if (directType != null)
            return directType;

        foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
        {
            if (string.Equals(type.Name, typeName, StringComparison.Ordinal)
                || string.Equals(type.FullName, typeName, StringComparison.Ordinal))
            {
                return type;
            }
        }

        return null;
    }

    public static List<GameDataWorkbenchReference> GetOutgoingReferences(UnityEngine.Object source)
    {
        List<GameDataWorkbenchReference> references = new List<GameDataWorkbenchReference>();
        if (source == null)
            return references;

        string sourcePath = AssetDatabase.GetAssetPath(source);
        foreach (SerializedProperty property in EnumerateProperties(source))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            UnityEngine.Object target = property.objectReferenceValue;
            if (target == null)
                continue;

            references.Add(new GameDataWorkbenchReference
            {
                Source = source,
                Target = target,
                SourcePath = sourcePath,
                PropertyPath = property.propertyPath
            });
        }

        return references;
    }

    public static List<GameDataWorkbenchReference> GetIncomingReferences(
        UnityEngine.Object target,
        IReadOnlyList<GameDataWorkbenchAssetRecord> searchScope)
    {
        List<GameDataWorkbenchReference> references = new List<GameDataWorkbenchReference>();
        if (target == null || searchScope == null)
            return references;

        for (int index = 0; index < searchScope.Count; index++)
        {
            UnityEngine.Object source = searchScope[index] != null ? searchScope[index].Asset : null;
            if (source == null || source == target)
                continue;

            foreach (SerializedProperty property in EnumerateProperties(source))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (property.objectReferenceValue != target)
                    continue;

                references.Add(new GameDataWorkbenchReference
                {
                    Source = source,
                    Target = target,
                    SourcePath = AssetDatabase.GetAssetPath(source),
                    PropertyPath = property.propertyPath
                });
            }
        }

        return references;
    }

    public static List<GameDataWorkbenchHealthIssue> AnalyzeHealth(
        IReadOnlyList<GameDataWorkbenchAssetRecord> assets)
    {
        List<GameDataWorkbenchHealthIssue> issues = new List<GameDataWorkbenchHealthIssue>();
        Dictionary<string, List<UnityEngine.Object>> ids = new Dictionary<string, List<UnityEngine.Object>>();

        if (assets == null)
            return issues;

        for (int index = 0; index < assets.Count; index++)
        {
            UnityEngine.Object asset = assets[index] != null ? assets[index].Asset : null;
            if (asset == null)
                continue;

            AnalyzeMissingReferences(asset, issues);
            CollectIds(asset, ids);
        }

        foreach (KeyValuePair<string, List<UnityEngine.Object>> pair in ids)
        {
            if (pair.Value.Count <= 1)
                continue;

            issues.Add(new GameDataWorkbenchHealthIssue
            {
                Severity = MessageType.Warning,
                Title = "Duplicate ID",
                Details = $"{pair.Key} appears on {pair.Value.Count} assets.",
                Context = pair.Value[0]
            });
        }

        issues.Sort((left, right) => SeverityRank(right.Severity).CompareTo(SeverityRank(left.Severity)));
        return issues;
    }

    public static UnityEngine.Object DuplicateAsset(UnityEngine.Object source)
    {
        if (source == null)
            return null;

        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return null;

        string directory = Path.GetDirectoryName(sourcePath);
        string fileName = Path.GetFileNameWithoutExtension(sourcePath);
        string extension = Path.GetExtension(sourcePath);
        string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{fileName}_Copy{extension}");

        if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            return null;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadMainAssetAtPath(targetPath);
    }

    public static void CreateBasic3DTestSetup()
    {
        GameObject root = new GameObject("[3D Test Setup]");
        Undo.RegisterCreatedObjectUndo(root, "Create 3D Test Setup");

        EnsureGround(root.transform);
        EnsureDirectionalLight(root.transform);
        EnsurePrototype3DActivator(root.transform);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void AddAssetsByType(
        List<GameDataWorkbenchAssetRecord> records,
        HashSet<string> seenPaths,
        Type type,
        string[] searchFolders)
    {
        AddAssetsByFilter(records, seenPaths, $"t:{type.Name}", searchFolders, type);
    }

    private static void AddAssetsByFilter(
        List<GameDataWorkbenchAssetRecord> records,
        HashSet<string> seenPaths,
        string filter,
        string[] searchFolders,
        Type requiredType = null)
    {
        string[] validFolders = ResolveValidSearchFolders(searchFolders);
        if (validFolders.Length == 0)
            return;

        string[] guids = AssetDatabase.FindAssets(filter, validFolders);
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            if (string.IsNullOrWhiteSpace(path) || !seenPaths.Add(path))
                continue;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                continue;

            if (requiredType != null && !requiredType.IsInstanceOfType(asset))
                continue;

            records.Add(new GameDataWorkbenchAssetRecord
            {
                Asset = asset,
                Path = path,
                AssetType = asset.GetType()
            });
        }
    }

    private static string[] ResolveValidSearchFolders(string[] searchFolders)
    {
        List<string> validFolders = new List<string>();
        string[] folders = searchFolders == null || searchFolders.Length == 0
            ? new[] { "Assets" }
            : searchFolders;

        for (int index = 0; index < folders.Length; index++)
        {
            string folder = folders[index];
            if (string.IsNullOrWhiteSpace(folder))
                continue;

            if (AssetDatabase.IsValidFolder(folder))
                validFolders.Add(folder);
        }

        return validFolders.ToArray();
    }

    private static IEnumerable<SerializedProperty> EnumerateProperties(UnityEngine.Object asset)
    {
        if (asset == null)
            yield break;

        SerializedObject serializedObject;
        try
        {
            serializedObject = new SerializedObject(asset);
        }
        catch
        {
            yield break;
        }

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = true;
            yield return iterator.Copy();
        }
    }

    private static void AnalyzeMissingReferences(
        UnityEngine.Object asset,
        List<GameDataWorkbenchHealthIssue> issues)
    {
        foreach (SerializedProperty property in EnumerateProperties(asset))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            if (property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0)
                continue;

            issues.Add(new GameDataWorkbenchHealthIssue
            {
                Severity = MessageType.Error,
                Title = "Missing Reference",
                Details = $"{asset.name} has a missing reference at {property.propertyPath}.",
                Context = asset
            });
        }
    }

    private static void CollectIds(
        UnityEngine.Object asset,
        Dictionary<string, List<UnityEngine.Object>> ids)
    {
        SerializedObject serializedObject;
        try
        {
            serializedObject = new SerializedObject(asset);
        }
        catch
        {
            return;
        }

        for (int index = 0; index < CommonIdFields.Length; index++)
        {
            SerializedProperty property = serializedObject.FindProperty(CommonIdFields[index]);
            if (property == null || property.propertyType != SerializedPropertyType.String)
                continue;

            string value = property.stringValue;
            if (string.IsNullOrWhiteSpace(value))
                continue;

            string key = $"{asset.GetType().Name}.{CommonIdFields[index]}:{value.Trim()}";
            if (!ids.TryGetValue(key, out List<UnityEngine.Object> assets))
            {
                assets = new List<UnityEngine.Object>();
                ids.Add(key, assets);
            }

            assets.Add(asset);
        }
    }

    private static int SeverityRank(MessageType severity)
    {
        switch (severity)
        {
            case MessageType.Error:
                return 3;
            case MessageType.Warning:
                return 2;
            case MessageType.Info:
                return 1;
            default:
                return 0;
        }
    }

    private static string[] GetAllKnownTypeNames()
    {
        HashSet<string> typeNames = new HashSet<string>(StringComparer.Ordinal);
        for (int categoryIndex = 0; categoryIndex < DefaultCategories.Length; categoryIndex++)
        {
            string[] names = DefaultCategories[categoryIndex].TypeNames;
            for (int index = 0; index < names.Length; index++)
                typeNames.Add(names[index]);
        }

        string[] result = new string[typeNames.Count];
        typeNames.CopyTo(result);
        return result;
    }

    private static void EnsureGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Test Ground";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        Undo.RegisterCreatedObjectUndo(ground, "Create Test Ground");
    }

    private static void EnsureDirectionalLight(Transform parent)
    {
        if (UnityEngine.Object.FindFirstObjectByType<Light>() != null)
            return;

        GameObject lightObject = new GameObject("Test Directional Light");
        lightObject.transform.SetParent(parent);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        Undo.RegisterCreatedObjectUndo(lightObject, "Create Test Directional Light");
    }

    private static void EnsurePrototype3DActivator(Transform parent)
    {
        Type activatorType = Type.GetType("Prototype3DSceneModeActivator");
        if (activatorType == null)
            activatorType = ResolveMonoBehaviourType("Prototype3DSceneModeActivator");

        if (activatorType == null)
            return;

        if (FindSceneComponentOfType(activatorType) != null)
            return;

        GameObject activatorObject = new GameObject("Prototype3DSceneMode");
        activatorObject.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(activatorObject, "Create Prototype3DSceneMode");
        Undo.AddComponent(activatorObject, activatorType);
    }

    private static Type ResolveMonoBehaviourType(string typeName)
    {
        foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
        {
            if (string.Equals(type.Name, typeName, StringComparison.Ordinal)
                || string.Equals(type.FullName, typeName, StringComparison.Ordinal))
            {
                return type;
            }
        }

        return null;
    }

    private static Component FindSceneComponentOfType(Type type)
    {
        if (type == null)
            return null;

        UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(type);
        for (int index = 0; index < objects.Length; index++)
        {
            Component component = objects[index] as Component;
            if (component == null || component.gameObject == null)
                continue;

            if (!component.gameObject.scene.IsValid())
                continue;

            return component;
        }

        return null;
    }
}
