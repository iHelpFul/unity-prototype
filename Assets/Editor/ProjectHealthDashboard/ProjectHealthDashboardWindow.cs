using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class ProjectHealthIssue
{
    public MessageType Severity;
    public string Category;
    public string Title;
    public string Details;
    public UnityEngine.Object Context;
    public string AssetPath;
}

public class ProjectHealthDashboardWindow : EditorWindow
{
    private static readonly string[] CommonIdFields =
    {
        "skillId",
        "itemId",
        "questId",
        "npcId",
        "vendorId",
        "enemyId",
        "projectileId",
        "passiveId",
        "attackId",
        "profileId",
        "definitionId"
    };

    private readonly List<ProjectHealthIssue> issues = new List<ProjectHealthIssue>();
    private Vector2 scroll;
    private string searchText = string.Empty;
    private bool showInfo = true;
    private bool showWarnings = true;
    private bool showErrors = true;
    private bool scanScriptableObjects = true;
    private bool scanPrefabs = true;
    private bool scanAnimationClips = true;
    private bool scanActiveScene = true;

    [MenuItem("Tools/Project Health/Dashboard")]
    public static void Open()
    {
        GetWindow<ProjectHealthDashboardWindow>("Project Health");
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawOptions();
        DrawIssues();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                Scan();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                issues.Clear();

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{FilteredIssues.Count} / {issues.Count}", EditorStyles.miniLabel);
        }
    }

    private void DrawOptions()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Project Health Dashboard", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Broad non-destructive project scan. It reports missing references, duplicate common IDs, prefab warnings, animation event issues, and active scene setup issues.",
            MessageType.Info);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Scan Scope", EditorStyles.boldLabel);
            scanScriptableObjects = EditorGUILayout.Toggle("ScriptableObjects", scanScriptableObjects);
            scanPrefabs = EditorGUILayout.Toggle("Prefabs", scanPrefabs);
            scanAnimationClips = EditorGUILayout.Toggle("Animation Clips", scanAnimationClips);
            scanActiveScene = EditorGUILayout.Toggle("Active Scene", scanActiveScene);
        }

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            showErrors = EditorGUILayout.ToggleLeft("Errors", showErrors, GUILayout.Width(80f));
            showWarnings = EditorGUILayout.ToggleLeft("Warnings", showWarnings, GUILayout.Width(100f));
            showInfo = EditorGUILayout.ToggleLeft("Info", showInfo, GUILayout.Width(80f));
            GUILayout.FlexibleSpace();
            searchText = EditorGUILayout.TextField("Search", searchText, GUILayout.Width(300f));
        }
    }

    private void DrawIssues()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        List<ProjectHealthIssue> filtered = FilteredIssues;
        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox(issues.Count == 0 ? "Run a scan to see project health issues." : "No issues match the current filters.", MessageType.Info);
        }
        else
        {
            for (int index = 0; index < filtered.Count; index++)
                DrawIssue(filtered[index]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawIssue(ProjectHealthIssue issue)
    {
        if (issue == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox($"{issue.Category}: {issue.Title}\n{issue.Details}", issue.Severity);

            if (!string.IsNullOrWhiteSpace(issue.AssetPath))
                EditorGUILayout.SelectableLabel(issue.AssetPath, EditorStyles.miniLabel, GUILayout.Height(18f));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (issue.Context != null)
                    EditorGUILayout.ObjectField(issue.Context, typeof(UnityEngine.Object), true);
                else
                    GUILayout.FlexibleSpace();

                if (issue.Context != null && GUILayout.Button("Select", GUILayout.Width(64f)))
                    Selection.activeObject = issue.Context;

                if (issue.Context != null && GUILayout.Button("Ping", GUILayout.Width(52f)))
                    EditorGUIUtility.PingObject(issue.Context);

                if (!string.IsNullOrWhiteSpace(issue.AssetPath) && GUILayout.Button("Open", GUILayout.Width(56f)))
                    AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(issue.AssetPath));
            }
        }
    }

    private void Scan()
    {
        issues.Clear();

        try
        {
            if (scanScriptableObjects)
                ScanScriptableObjects();

            if (scanPrefabs)
                ScanPrefabs();

            if (scanAnimationClips)
                ScanAnimationClips();

            if (scanActiveScene)
                ScanActiveScene();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        issues.Sort((left, right) =>
        {
            int severityCompare = SeverityRank(right.Severity).CompareTo(SeverityRank(left.Severity));
            if (severityCompare != 0)
                return severityCompare;

            int categoryCompare = string.Compare(left.Category, right.Category, StringComparison.OrdinalIgnoreCase);
            if (categoryCompare != 0)
                return categoryCompare;

            return string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void ScanScriptableObjects()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
        Dictionary<string, List<UnityEngine.Object>> idMap = new Dictionary<string, List<UnityEngine.Object>>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                continue;

            EditorUtility.DisplayProgressBar("Project Health", $"Scanning SO {asset.name}", index / Mathf.Max(1f, guids.Length));
            ScanSerializedObject(asset, asset, path, "ScriptableObject");
            CollectCommonIds(asset, idMap);
        }

        foreach (KeyValuePair<string, List<UnityEngine.Object>> pair in idMap)
        {
            if (pair.Value.Count <= 1)
                continue;

            AddIssue(
                MessageType.Warning,
                "IDs",
                "Duplicate common ID",
                $"{pair.Key} appears on {pair.Value.Count} assets.",
                pair.Value[0],
                AssetDatabase.GetAssetPath(pair.Value[0]));
        }
    }

    private void ScanPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            EditorUtility.DisplayProgressBar("Project Health", $"Scanning Prefab {prefab.name}", index / Mathf.Max(1f, guids.Length));
            ScanPrefab(prefab, path);
        }
    }

    private void ScanPrefab(GameObject prefab, string path)
    {
        Component[] components = prefab.GetComponentsInChildren<Component>(true);
        for (int index = 0; index < components.Length; index++)
        {
            Component component = components[index];
            if (component == null)
            {
                AddIssue(MessageType.Error, "Prefab", "Missing script", prefab.name, prefab, path);
                continue;
            }

            ScanSerializedObject(component, prefab, path, "Prefab");
        }

        string lowered = $"{path}/{prefab.name}".ToLowerInvariant();
        if (lowered.Contains("enemy") || lowered.Contains("slime") || lowered.Contains("turtle"))
        {
            if (!HasComponentNamed(prefab, "EnemyHealth"))
                AddIssue(MessageType.Warning, "Prefab", "Enemy-like prefab missing EnemyHealth", prefab.name, prefab, path);
        }
    }

    private void ScanAnimationClips()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
                continue;

            EditorUtility.DisplayProgressBar("Project Health", $"Scanning Clip {clip.name}", index / Mathf.Max(1f, guids.Length));
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
            {
                AnimationEvent animationEvent = events[eventIndex];
                if (!string.IsNullOrWhiteSpace(animationEvent.functionName))
                    continue;

                AddIssue(MessageType.Warning, "Animation", "Animation event without function name", clip.name, clip, path);
            }
        }
    }

    private void ScanActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        Type enemyHealthType = ResolveComponentType("EnemyHealth");
        Type sceneEntityIdType = ResolveComponentType("SceneEntityId");
        Type spawnPointType = ResolveComponentType("SceneSpawnPoint");

        if (enemyHealthType != null)
        {
            List<Component> enemies = FindSceneComponents(enemyHealthType, scene);
            for (int index = 0; index < enemies.Count; index++)
            {
                Component enemy = enemies[index];
                if (sceneEntityIdType != null && enemy.GetComponent(sceneEntityIdType) == null)
                {
                    AddIssue(
                        MessageType.Warning,
                        "Scene",
                        "Enemy missing SceneEntityId",
                        GetHierarchyPath(enemy.transform),
                        enemy,
                        scene.path);
                }
            }
        }

        if (spawnPointType != null)
        {
            List<Component> spawns = FindSceneComponents(spawnPointType, scene);
            if (spawns.Count == 0)
            {
                AddIssue(MessageType.Warning, "Scene", "No SceneSpawnPoint in active scene", scene.name, null, scene.path);
            }
        }
    }

    private void ScanSerializedObject(UnityEngine.Object serializedTarget, UnityEngine.Object context, string path, string category)
    {
        SerializedObject serializedObject;
        try
        {
            serializedObject = new SerializedObject(serializedTarget);
        }
        catch
        {
            return;
        }

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = true;
            if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            if (iterator.objectReferenceValue != null || iterator.objectReferenceInstanceIDValue == 0)
                continue;

            AddIssue(
                MessageType.Error,
                category,
                "Missing object reference",
                $"{serializedTarget.name}: {iterator.propertyPath}",
                context,
                path);
        }
    }

    private void CollectCommonIds(UnityEngine.Object asset, Dictionary<string, List<UnityEngine.Object>> idMap)
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
            if (!idMap.TryGetValue(key, out List<UnityEngine.Object> assets))
            {
                assets = new List<UnityEngine.Object>();
                idMap.Add(key, assets);
            }

            assets.Add(asset);
        }
    }

    private static bool HasComponentNamed(GameObject root, string componentTypeName)
    {
        Component[] components = root.GetComponentsInChildren<Component>(true);
        for (int index = 0; index < components.Length; index++)
        {
            Component component = components[index];
            if (component != null && component.GetType().Name == componentTypeName)
                return true;
        }

        return false;
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

    private void AddIssue(
        MessageType severity,
        string category,
        string title,
        string details,
        UnityEngine.Object context,
        string assetPath)
    {
        issues.Add(new ProjectHealthIssue
        {
            Severity = severity,
            Category = category,
            Title = title,
            Details = details,
            Context = context,
            AssetPath = assetPath
        });
    }

    private List<ProjectHealthIssue> FilteredIssues
    {
        get
        {
            List<ProjectHealthIssue> filtered = new List<ProjectHealthIssue>();
            string normalizedSearch = string.IsNullOrWhiteSpace(searchText) ? string.Empty : searchText.Trim();

            for (int index = 0; index < issues.Count; index++)
            {
                ProjectHealthIssue issue = issues[index];
                if (issue == null)
                    continue;

                if (issue.Severity == MessageType.Error && !showErrors)
                    continue;
                if (issue.Severity == MessageType.Warning && !showWarnings)
                    continue;
                if (issue.Severity == MessageType.Info && !showInfo)
                    continue;

                if (!string.IsNullOrWhiteSpace(normalizedSearch)
                    && issue.Category.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0
                    && issue.Title.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0
                    && issue.Details.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0
                    && (issue.AssetPath == null || issue.AssetPath.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    continue;
                }

                filtered.Add(issue);
            }

            return filtered;
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
