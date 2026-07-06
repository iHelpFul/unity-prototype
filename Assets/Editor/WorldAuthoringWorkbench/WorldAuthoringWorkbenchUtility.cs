using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal enum WorldAuthoringPrefabCategory
{
    All,
    Enemies,
    Npc,
    Loot,
    Items,
    Projectiles,
    Vfx,
    Environment
}

internal sealed class WorldAuthoringPrefabRecord
{
    public GameObject Prefab;
    public string Path;
    public WorldAuthoringPrefabCategory Category;

    public string DisplayName => Prefab != null ? Prefab.name : "<Missing Prefab>";
}

internal sealed class WorldAuthoringPlacementSettings
{
    public string ParentName = "World Authoring";
    public bool UseParent = true;
    public bool SnapToGrid = true;
    public float GridSize = 1f;
    public bool AlignToGround = true;
    public LayerMask GroundMask = Physics.DefaultRaycastLayers;
    public bool RandomizeYaw;
    public bool UseFacingYawOverride;
    public float FacingYaw;
    public Vector3 RotationOffset;
    public Vector3 PositionOffset;
    public Vector3 Scale = Vector3.one;
}

internal sealed class WorldAuthoringSceneIssue
{
    public MessageType Severity;
    public string Title;
    public string Details;
    public UnityEngine.Object Context;
}

internal sealed class WorldAuthoringSceneSummary
{
    public int EnemyCount;
    public int SpawnPointCount;
    public int DefaultSpawnPointCount;
    public int MissingEnemySceneIds;
    public int InvalidEnemySceneIds;
    public int DuplicateEnemySceneIds;
    public int DuplicateSpawnIds;
    public readonly List<WorldAuthoringSceneIssue> Issues = new List<WorldAuthoringSceneIssue>();
}

internal static class WorldAuthoringWorkbenchUtility
{
    private const string EnemyHealthTypeName = "EnemyHealth";
    private const string SceneEntityIdTypeName = "SceneEntityId";
    private const string SceneSpawnPointTypeName = "SceneSpawnPoint";
    private const string EnemySceneIdPrefix = "map01_enemy_";
    private const int EnemySceneIdHexLength = 8;

    public static List<WorldAuthoringPrefabRecord> LoadPrefabs(
        string searchFolder,
        WorldAuthoringPrefabCategory category,
        string searchText)
    {
        string folder = string.IsNullOrWhiteSpace(searchFolder) ? "Assets/Prefabs" : searchFolder.Trim();
        string[] folders = AssetDatabase.IsValidFolder(folder) ? new[] { folder } : new[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", folders);
        List<WorldAuthoringPrefabRecord> records = new List<WorldAuthoringPrefabRecord>();
        string normalizedSearch = string.IsNullOrWhiteSpace(searchText) ? string.Empty : searchText.Trim();

        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            WorldAuthoringPrefabCategory resolvedCategory = ResolvePrefabCategory(path, prefab.name);
            if (category != WorldAuthoringPrefabCategory.All && resolvedCategory != category)
                continue;

            if (!string.IsNullOrWhiteSpace(normalizedSearch)
                && prefab.name.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0
                && path.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            records.Add(new WorldAuthoringPrefabRecord
            {
                Prefab = prefab,
                Path = path,
                Category = resolvedCategory
            });
        }

        records.Sort((left, right) =>
        {
            int categoryCompare = left.Category.CompareTo(right.Category);
            if (categoryCompare != 0)
                return categoryCompare;

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        return records;
    }

    public static GameObject PlacePrefab(
        GameObject prefab,
        Vector3 position,
        WorldAuthoringPlacementSettings settings)
    {
        if (prefab == null)
            return null;

        if (settings == null)
            settings = new WorldAuthoringPlacementSettings();
        Vector3 resolvedPosition = ResolvePlacementPosition(position, settings);
        Quaternion rotation = ResolvePlacementRotation(settings);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");
        instance.transform.SetPositionAndRotation(resolvedPosition, rotation);
        instance.transform.localScale = ResolveScale(settings.Scale);

        if (settings.UseParent)
        {
            Transform parent = FindOrCreateContainer(settings.ParentName);
            if (parent != null)
                Undo.SetTransformParent(instance.transform, parent, "Parent Placed Prefab");
        }

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return instance;
    }

    public static int SnapSelection(float gridSize)
    {
        gridSize = Mathf.Max(0.01f, gridSize);
        Transform[] transforms = Selection.transforms;
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform transform = transforms[index];
            if (transform == null)
                continue;

            Undo.RecordObject(transform, "Snap Selection");
            transform.position = SnapVector(transform.position, gridSize);
        }

        if (transforms.Length > 0)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return transforms.Length;
    }

    public static int AlignSelectionToGround(LayerMask groundMask)
    {
        Transform[] transforms = Selection.transforms;
        int aligned = 0;
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform transform = transforms[index];
            if (transform == null)
                continue;

            Vector3 position = transform.position;
            if (!TryProjectToGround(position, groundMask, out Vector3 groundPosition))
                continue;

            Undo.RecordObject(transform, "Align Selection To Ground");
            transform.position = groundPosition;
            aligned++;
        }

        if (aligned > 0)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return aligned;
    }

    public static int ParentSelection(string parentName)
    {
        Transform parent = FindOrCreateContainer(parentName);
        if (parent == null)
            return 0;

        Transform[] transforms = Selection.transforms;
        int parented = 0;
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform transform = transforms[index];
            if (transform == null || transform == parent || transform.IsChildOf(parent))
                continue;

            Undo.SetTransformParent(transform, parent, "Parent Selection");
            parented++;
        }

        if (parented > 0)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return parented;
    }

    public static GameObject CreateSpawnPoint(string spawnId, bool isDefault, Vector3 position)
    {
        Type spawnPointType = ResolveComponentType(SceneSpawnPointTypeName);
        GameObject spawnPointObject = new GameObject(string.IsNullOrWhiteSpace(spawnId) ? "SpawnPoint" : $"SpawnPoint_{spawnId.Trim()}");
        Undo.RegisterCreatedObjectUndo(spawnPointObject, "Create Spawn Point");
        spawnPointObject.transform.position = position;

        if (spawnPointType != null)
        {
            Component component = Undo.AddComponent(spawnPointObject, spawnPointType);
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty spawnIdProperty = serializedObject.FindProperty("spawnId");
            if (spawnIdProperty != null)
                spawnIdProperty.stringValue = string.IsNullOrWhiteSpace(spawnId) ? "default" : spawnId.Trim();

            SerializedProperty defaultProperty = serializedObject.FindProperty("isDefaultSpawn");
            if (defaultProperty != null)
                defaultProperty.boolValue = isDefault;

            serializedObject.ApplyModifiedProperties();
        }

        Selection.activeGameObject = spawnPointObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return spawnPointObject;
    }

    public static WorldAuthoringSceneSummary AnalyzeCurrentScene()
    {
        WorldAuthoringSceneSummary summary = new WorldAuthoringSceneSummary();
        Scene activeScene = SceneManager.GetActiveScene();

        AnalyzeEnemies(activeScene, summary);
        AnalyzeSpawnPoints(activeScene, summary);
        summary.Issues.Sort((left, right) => SeverityRank(right.Severity).CompareTo(SeverityRank(left.Severity)));
        return summary;
    }

    public static int AssignMissingEnemySceneIds()
    {
        Type enemyHealthType = ResolveComponentType(EnemyHealthTypeName);
        Type sceneEntityIdType = ResolveComponentType(SceneEntityIdTypeName);
        if (enemyHealthType == null || sceneEntityIdType == null)
            return 0;

        List<Component> enemies = FindSceneComponents(enemyHealthType, SceneManager.GetActiveScene());
        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < enemies.Count; index++)
        {
            Component existingEntityId = enemies[index].GetComponent(sceneEntityIdType);
            string existingId = GetSceneEntityId(existingEntityId);
            if (IsValidEnemySceneId(existingId))
                usedIds.Add(existingId);
        }

        int assigned = 0;
        for (int index = 0; index < enemies.Count; index++)
        {
            GameObject enemyObject = enemies[index].gameObject;
            Component entityId = enemyObject.GetComponent(sceneEntityIdType);
            string currentId = GetSceneEntityId(entityId);
            if (IsValidEnemySceneId(currentId))
                continue;

            if (entityId == null)
                entityId = Undo.AddComponent(enemyObject, sceneEntityIdType);

            string generatedId = GenerateUniqueEnemySceneId(usedIds);
            SetSceneEntityId(entityId, generatedId);
            assigned++;
        }

        if (assigned > 0)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return assigned;
    }

    public static Vector3 GetSceneViewPivot()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        return sceneView != null ? sceneView.pivot : Vector3.zero;
    }

    public static Vector3 GetSelectedPivotOrScenePivot()
    {
        return Selection.activeTransform != null
            ? Selection.activeTransform.position
            : GetSceneViewPivot();
    }

    private static WorldAuthoringPrefabCategory ResolvePrefabCategory(string path, string name)
    {
        string combined = $"{path}/{name}".ToLowerInvariant();
        if (combined.Contains("/enemies/") || combined.Contains("enemy") || combined.Contains("slime") || combined.Contains("turtle"))
            return WorldAuthoringPrefabCategory.Enemies;
        if (combined.Contains("npc"))
            return WorldAuthoringPrefabCategory.Npc;
        if (combined.Contains("loot") || combined.Contains("drop") || combined.Contains("potion") || combined.Contains("meso") || combined.Contains("money"))
            return WorldAuthoringPrefabCategory.Loot;
        if (combined.Contains("/items/") || combined.Contains("weapon") || combined.Contains("hat") || combined.Contains("body"))
            return WorldAuthoringPrefabCategory.Items;
        if (combined.Contains("projectile") || combined.Contains("shurikan") || combined.Contains("shuriken"))
            return WorldAuthoringPrefabCategory.Projectiles;
        if (combined.Contains("vfx") || combined.Contains("effect"))
            return WorldAuthoringPrefabCategory.Vfx;

        return WorldAuthoringPrefabCategory.Environment;
    }

    private static Vector3 ResolvePlacementPosition(Vector3 position, WorldAuthoringPlacementSettings settings)
    {
        Vector3 resolved = position + settings.PositionOffset;
        if (settings.AlignToGround && TryProjectToGround(resolved, settings.GroundMask, out Vector3 groundPosition))
            resolved = groundPosition + new Vector3(0f, settings.PositionOffset.y, 0f);

        if (settings.SnapToGrid)
            resolved = SnapVector(resolved, Mathf.Max(0.01f, settings.GridSize));

        return resolved;
    }

    private static Quaternion ResolvePlacementRotation(WorldAuthoringPlacementSettings settings)
    {
        float yaw = settings.UseFacingYawOverride ? settings.FacingYaw : 0f;
        if (settings.RandomizeYaw)
            yaw += UnityEngine.Random.Range(0f, 360f);

        return Quaternion.Euler(settings.RotationOffset.x, yaw + settings.RotationOffset.y, settings.RotationOffset.z);
    }

    private static Vector3 ResolveScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Approximately(scale.x, 0f) ? 1f : scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : scale.y,
            Mathf.Approximately(scale.z, 0f) ? 1f : scale.z);
    }

    private static Vector3 SnapVector(Vector3 value, float gridSize)
    {
        return new Vector3(
            Mathf.Round(value.x / gridSize) * gridSize,
            Mathf.Round(value.y / gridSize) * gridSize,
            Mathf.Round(value.z / gridSize) * gridSize);
    }

    private static bool TryProjectToGround(Vector3 position, LayerMask groundMask, out Vector3 groundPosition)
    {
        Vector3 rayOrigin = position + Vector3.up * 150f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 400f, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;
            return true;
        }

        groundPosition = position;
        return false;
    }

    private static Transform FindOrCreateContainer(string parentName)
    {
        string resolvedName = string.IsNullOrWhiteSpace(parentName) ? "World Authoring" : parentName.Trim();
        GameObject existing = GameObject.Find(resolvedName);
        if (existing != null)
            return existing.transform;

        GameObject container = new GameObject(resolvedName);
        Undo.RegisterCreatedObjectUndo(container, "Create Authoring Container");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return container.transform;
    }

    private static void AnalyzeEnemies(Scene scene, WorldAuthoringSceneSummary summary)
    {
        Type enemyHealthType = ResolveComponentType(EnemyHealthTypeName);
        Type sceneEntityIdType = ResolveComponentType(SceneEntityIdTypeName);
        if (enemyHealthType == null)
        {
            summary.Issues.Add(new WorldAuthoringSceneIssue
            {
                Severity = MessageType.Info,
                Title = "Enemy scan skipped",
                Details = "No EnemyHealth component type was found in this project."
            });
            return;
        }

        List<Component> enemies = FindSceneComponents(enemyHealthType, scene);
        summary.EnemyCount = enemies.Count;
        Dictionary<string, Component> ids = new Dictionary<string, Component>(StringComparer.Ordinal);

        for (int index = 0; index < enemies.Count; index++)
        {
            Component enemy = enemies[index];
            Component entityId = sceneEntityIdType != null ? enemy.GetComponent(sceneEntityIdType) : null;
            if (entityId == null)
            {
                summary.MissingEnemySceneIds++;
                summary.Issues.Add(new WorldAuthoringSceneIssue
                {
                    Severity = MessageType.Warning,
                    Title = "Enemy missing SceneEntityId",
                    Details = GetHierarchyPath(enemy.transform),
                    Context = enemy
                });
                continue;
            }

            string sceneId = GetSceneEntityId(entityId);
            if (!IsValidEnemySceneId(sceneId))
            {
                summary.InvalidEnemySceneIds++;
                summary.Issues.Add(new WorldAuthoringSceneIssue
                {
                    Severity = MessageType.Warning,
                    Title = "Enemy has invalid SceneEntityId",
                    Details = $"{GetHierarchyPath(enemy.transform)} -> '{sceneId}'",
                    Context = enemy
                });
                continue;
            }

            if (ids.TryGetValue(sceneId, out Component duplicate))
            {
                summary.DuplicateEnemySceneIds++;
                summary.Issues.Add(new WorldAuthoringSceneIssue
                {
                    Severity = MessageType.Error,
                    Title = "Duplicate enemy SceneEntityId",
                    Details = $"{sceneId}\n{GetHierarchyPath(duplicate.transform)}\n{GetHierarchyPath(enemy.transform)}",
                    Context = enemy
                });
                continue;
            }

            ids.Add(sceneId, enemy);
        }
    }

    private static void AnalyzeSpawnPoints(Scene scene, WorldAuthoringSceneSummary summary)
    {
        Type spawnPointType = ResolveComponentType(SceneSpawnPointTypeName);
        if (spawnPointType == null)
        {
            summary.Issues.Add(new WorldAuthoringSceneIssue
            {
                Severity = MessageType.Info,
                Title = "Spawn point scan skipped",
                Details = "No SceneSpawnPoint component type was found in this project."
            });
            return;
        }

        List<Component> spawnPoints = FindSceneComponents(spawnPointType, scene);
        summary.SpawnPointCount = spawnPoints.Count;
        Dictionary<string, Component> spawnIds = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < spawnPoints.Count; index++)
        {
            Component spawnPoint = spawnPoints[index];
            string spawnId = GetStringMemberValue(spawnPoint, "SpawnId", "spawnId", "default");
            bool isDefault = GetBoolMemberValue(spawnPoint, "IsDefaultSpawn", "isDefaultSpawn", false);

            if (isDefault)
                summary.DefaultSpawnPointCount++;

            if (spawnIds.TryGetValue(spawnId, out Component duplicate))
            {
                summary.DuplicateSpawnIds++;
                summary.Issues.Add(new WorldAuthoringSceneIssue
                {
                    Severity = MessageType.Warning,
                    Title = "Duplicate spawn ID",
                    Details = $"{spawnId}\n{GetHierarchyPath(duplicate.transform)}\n{GetHierarchyPath(spawnPoint.transform)}",
                    Context = spawnPoint
                });
                continue;
            }

            spawnIds.Add(spawnId, spawnPoint);
        }

        if (spawnPoints.Count == 0)
        {
            summary.Issues.Add(new WorldAuthoringSceneIssue
            {
                Severity = MessageType.Warning,
                Title = "No spawn points",
                Details = "The active scene has no SceneSpawnPoint components."
            });
        }
        else if (summary.DefaultSpawnPointCount == 0)
        {
            summary.Issues.Add(new WorldAuthoringSceneIssue
            {
                Severity = MessageType.Warning,
                Title = "No default spawn point",
                Details = "At least one SceneSpawnPoint should be marked as default."
            });
        }
    }

    private static List<Component> FindSceneComponents(Type componentType, Scene scene)
    {
        List<Component> components = new List<Component>();
        if (componentType == null)
            return components;

        UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(componentType);
        for (int index = 0; index < objects.Length; index++)
        {
            Component component = objects[index] as Component;
            if (component == null || component.gameObject == null)
                continue;

            if (!component.gameObject.scene.IsValid() || component.gameObject.scene.handle != scene.handle)
                continue;

            if (EditorUtility.IsPersistent(component))
                continue;

            components.Add(component);
        }

        return components;
    }

    private static Type ResolveComponentType(string typeName)
    {
        foreach (Type type in TypeCache.GetTypesDerivedFrom<Component>())
        {
            if (string.Equals(type.Name, typeName, StringComparison.Ordinal)
                || string.Equals(type.FullName, typeName, StringComparison.Ordinal))
            {
                return type;
            }
        }

        return null;
    }

    private static string GetSceneEntityId(Component entityId)
    {
        return GetStringMemberValue(entityId, "SceneId", "sceneId", string.Empty);
    }

    private static void SetSceneEntityId(Component entityId, string value)
    {
        if (entityId == null)
            return;

        MethodInfo editorSetMethod = entityId.GetType().GetMethod(
            "EditorSetSceneId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (editorSetMethod != null)
        {
            Undo.RecordObject(entityId, "Assign Scene Entity ID");
            editorSetMethod.Invoke(entityId, new object[] { value });
            EditorUtility.SetDirty(entityId);
            return;
        }

        SerializedObject serializedObject = new SerializedObject(entityId);
        SerializedProperty sceneIdProperty = serializedObject.FindProperty("sceneId");
        if (sceneIdProperty != null)
        {
            sceneIdProperty.stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static string GetStringMemberValue(Component component, string propertyName, string serializedFieldName, string fallback)
    {
        if (component == null)
            return fallback;

        PropertyInfo property = component.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.PropertyType == typeof(string))
        {
            string value = property.GetValue(component) as string;
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty serializedProperty = serializedObject.FindProperty(serializedFieldName);
        if (serializedProperty != null && serializedProperty.propertyType == SerializedPropertyType.String)
        {
            string value = serializedProperty.stringValue;
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        return fallback;
    }

    private static bool GetBoolMemberValue(Component component, string propertyName, string serializedFieldName, bool fallback)
    {
        if (component == null)
            return fallback;

        PropertyInfo property = component.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.PropertyType == typeof(bool))
            return (bool)property.GetValue(component);

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty serializedProperty = serializedObject.FindProperty(serializedFieldName);
        if (serializedProperty != null && serializedProperty.propertyType == SerializedPropertyType.Boolean)
            return serializedProperty.boolValue;

        return fallback;
    }

    private static bool IsValidEnemySceneId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith(EnemySceneIdPrefix, StringComparison.Ordinal)
            || value.Length != EnemySceneIdPrefix.Length + EnemySceneIdHexLength)
        {
            return false;
        }

        for (int index = EnemySceneIdPrefix.Length; index < value.Length; index++)
        {
            char character = value[index];
            bool isUpperHex = character >= '0' && character <= '9' || character >= 'A' && character <= 'F';
            if (!isUpperHex)
                return false;
        }

        return !value.EndsWith("00000000", StringComparison.Ordinal);
    }

    private static string GenerateUniqueEnemySceneId(HashSet<string> usedIds)
    {
        while (true)
        {
            string candidate = EnemySceneIdPrefix + Guid.NewGuid().ToString("N").Substring(0, EnemySceneIdHexLength).ToUpperInvariant();
            if (IsValidEnemySceneId(candidate) && usedIds.Add(candidate))
                return candidate;
        }
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return "<null>";

        List<string> segments = new List<string>();
        Transform current = target;
        while (current != null)
        {
            segments.Add(current.name);
            current = current.parent;
        }

        segments.Reverse();
        return string.Join("/", segments);
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
}
