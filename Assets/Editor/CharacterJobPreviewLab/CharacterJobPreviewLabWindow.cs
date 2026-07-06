using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class CharacterJobAssetRecord
{
    public UnityEngine.Object Asset;
    public string Path;

    public string DisplayName => Asset != null ? Asset.name : "<Missing Asset>";
}

public class CharacterJobPreviewLabWindow : EditorWindow
{
    private static readonly string[] Tabs = { "Jobs", "Appearance", "Preview Scene" };

    private int tabIndex;
    private string searchText = string.Empty;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private List<CharacterJobAssetRecord> jobs = new List<CharacterJobAssetRecord>();
    private List<CharacterJobAssetRecord> appearanceCatalogs = new List<CharacterJobAssetRecord>();
    private UnityEngine.Object selectedJob;
    private UnityEngine.Object selectedAppearanceCatalog;
    private GameObject previewPrefab;
    private Transform previewInstance;
    private Vector3 previewPosition;
    private Vector3 previewRotation;
    private Vector3 previewScale = Vector3.one;
    private Editor cachedEditor;
    private UnityEngine.Object cachedEditorTarget;
    private bool showInspector = true;

    [MenuItem("Tools/Characters/Job Preview Lab")]
    public static void Open()
    {
        GetWindow<CharacterJobPreviewLabWindow>("Job Preview Lab");
    }

    private void OnEnable()
    {
        RefreshAssets();
    }

    private void OnDisable()
    {
        DestroyCachedEditor();
    }

    private void OnGUI()
    {
        DrawToolbar();

        switch (tabIndex)
        {
            case 0:
                DrawJobsTab();
                break;
            case 1:
                DrawAppearanceTab();
                break;
            case 2:
                DrawPreviewSceneTab();
                break;
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            int newTabIndex = GUILayout.Toolbar(tabIndex, Tabs, EditorStyles.toolbarButton);
            if (newTabIndex != tabIndex)
            {
                tabIndex = newTabIndex;
                searchText = string.Empty;
                DestroyCachedEditor();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                RefreshAssets();
        }
    }

    private void DrawJobsTab()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawAssetList("Jobs", jobs, ref selectedJob);
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            if (selectedJob == null)
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox("Select a PlayerJobDefinition to inspect its combat, progression, and skill connections.", MessageType.Info);
            }
            else
            {
                DrawJobDetails(selectedJob);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawAppearanceTab()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawAssetList("Appearance Catalogs", appearanceCatalogs, ref selectedAppearanceCatalog);
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            if (selectedAppearanceCatalog == null)
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox("Select a CharacterAppearanceCatalogAsset to inspect option counts and palette coverage.", MessageType.Info);
            }
            else
            {
                DrawAppearanceCatalogDetails(selectedAppearanceCatalog);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawPreviewSceneTab()
    {
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Preview Scene", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use this as a lightweight scene staging helper. It places a prefab into the open scene without touching runtime character-selection UI.",
            MessageType.Info);

        selectedJob = EditorGUILayout.ObjectField("Job Context", selectedJob, ResolveObjectFieldType("PlayerJobDefinition"), false);
        selectedAppearanceCatalog = EditorGUILayout.ObjectField("Appearance Context", selectedAppearanceCatalog, ResolveObjectFieldType("CharacterAppearanceCatalogAsset"), false);
        previewPrefab = (GameObject)EditorGUILayout.ObjectField("Preview Prefab", previewPrefab, typeof(GameObject), false);
        previewPosition = EditorGUILayout.Vector3Field("Position", previewPosition);
        previewRotation = EditorGUILayout.Vector3Field("Rotation", previewRotation);
        previewScale = EditorGUILayout.Vector3Field("Scale", previewScale);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(previewPrefab == null))
            {
                if (GUILayout.Button("Create / Replace Preview"))
                    CreateOrReplacePreview();
            }

            if (GUILayout.Button("Remove Preview"))
                RemovePreview();

            if (GUILayout.Button("Frame Preview"))
                FramePreview();
        }

        EditorGUILayout.Space(8f);
        if (selectedJob != null)
            DrawJobQuickSummary(selectedJob);
        if (selectedAppearanceCatalog != null)
            DrawAppearanceQuickSummary(selectedAppearanceCatalog);

        EditorGUILayout.EndScrollView();
    }

    private void DrawAssetList(string title, List<CharacterJobAssetRecord> records, ref UnityEngine.Object selectedAsset)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(330f)))
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            searchText = EditorGUILayout.TextField("Search", searchText);

            List<CharacterJobAssetRecord> filtered = FilterRecords(records);
            EditorGUILayout.LabelField($"{filtered.Count} / {records.Count}", EditorStyles.miniLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            for (int index = 0; index < filtered.Count; index++)
                DrawAssetRow(filtered[index], ref selectedAsset);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawAssetRow(CharacterJobAssetRecord record, ref UnityEngine.Object selectedAsset)
    {
        if (record == null || record.Asset == null)
            return;

        bool selected = selectedAsset == record.Asset;
        using (new EditorGUILayout.HorizontalScope(selected ? EditorStyles.helpBox : GUIStyle.none))
        {
            Texture icon = AssetPreview.GetMiniThumbnail(record.Asset);
            GUILayout.Label(icon, GUILayout.Width(22f), GUILayout.Height(22f));

            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button(record.DisplayName, EditorStyles.label))
                {
                    selectedAsset = record.Asset;
                    Selection.activeObject = record.Asset;
                    DestroyCachedEditor();
                }

                EditorGUILayout.LabelField(record.Path, EditorStyles.miniLabel);
            }
        }
    }

    private void DrawJobDetails(UnityEngine.Object job)
    {
        DrawAssetHeader(job);
        DrawJobQuickSummary(job);
        DrawJobConnections(job);
        DrawInspector(job);
    }

    private void DrawJobQuickSummary(UnityEngine.Object job)
    {
        SerializedObject serializedObject = new SerializedObject(job);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Job Summary", EditorStyles.boldLabel);
            DrawReadOnlyProperty(serializedObject, "jobType", "Job Type");
            DrawReadOnlyProperty(serializedObject, "displayName", "Name");
            DrawReadOnlyProperty(serializedObject, "coreStat", "Core Stat");
            DrawReadOnlyProperty(serializedObject, "secondaryStat", "Secondary Stat");
            DrawReadOnlyProperty(serializedObject, "advancementLevelRequirement", "Advancement Level");
            DrawReadOnlyProperty(serializedObject, "baseMaxGauge", "Base Max Gauge");
            DrawArrayCount(serializedObject, "allowedWeaponTypes", "Allowed Weapons");
            DrawArrayCount(serializedObject, "defaultSkills", "Default Skills");
            DrawArrayCount(serializedObject, "defaultPassives", "Default Passives");
        }
    }

    private void DrawJobConnections(UnityEngine.Object job)
    {
        SerializedObject serializedObject = new SerializedObject(job);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Connected Profiles", EditorStyles.boldLabel);
            DrawObjectProperty(serializedObject, "basicAttackProfile", "Basic Attack");
            DrawObjectProperty(serializedObject, "animationProfile", "Animation Profile");
            DrawObjectProperty(serializedObject, "combatFormulaProfile", "Combat Formula");
        }

        DrawObjectArray("Default Skills", serializedObject.FindProperty("defaultSkills"));
        DrawObjectArray("Default Passives", serializedObject.FindProperty("defaultPassives"));
        DrawMissingJobConnections(serializedObject, job);
    }

    private void DrawMissingJobConnections(SerializedObject serializedObject, UnityEngine.Object context)
    {
        List<string> missing = new List<string>();
        if (serializedObject.FindProperty("basicAttackProfile")?.objectReferenceValue == null)
            missing.Add("Basic Attack Profile");
        if (serializedObject.FindProperty("animationProfile")?.objectReferenceValue == null)
            missing.Add("Animation Profile");
        if (serializedObject.FindProperty("combatFormulaProfile")?.objectReferenceValue == null)
            missing.Add("Combat Formula Profile");

        if (missing.Count == 0)
            return;

        EditorGUILayout.HelpBox($"Missing connections: {string.Join(", ", missing)}", MessageType.Warning);
    }

    private void DrawAppearanceCatalogDetails(UnityEngine.Object catalog)
    {
        DrawAssetHeader(catalog);
        DrawAppearanceQuickSummary(catalog);
        DrawInspector(catalog);
    }

    private void DrawAppearanceQuickSummary(UnityEngine.Object catalog)
    {
        SerializedObject serializedObject = new SerializedObject(catalog);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Appearance Catalog Coverage", EditorStyles.boldLabel);
            DrawArrayCount(serializedObject, "headOptions", "Heads");
            DrawArrayCount(serializedObject, "hairOptions", "Hair");
            DrawArrayCount(serializedObject, "eyeOptions", "Eyes");
            DrawArrayCount(serializedObject, "mouthOptions", "Mouths");
            DrawArrayCount(serializedObject, "outfitOptions", "Outfits");
            DrawArrayCount(serializedObject, "weaponRightOptions", "Right Weapons");
            DrawArrayCount(serializedObject, "hatOptions", "Hats");
            DrawArrayCount(serializedObject, "capeOptions", "Capes");
            DrawArrayCount(serializedObject, "hornsOptions", "Horns");
            DrawArrayCount(serializedObject, "accessoryOptions", "Accessories");
            DrawArrayCount(serializedObject, "ninjaMaskOptions", "Ninja Masks");
            DrawArrayCount(serializedObject, "mustacheOptions", "Mustaches");
            DrawArrayCount(serializedObject, "paletteOptions", "Palette Colors");
        }

        DrawPalettePreview(serializedObject.FindProperty("paletteOptions"));
    }

    private void DrawPalettePreview(SerializedProperty paletteProperty)
    {
        if (paletteProperty == null || !paletteProperty.isArray || paletteProperty.arraySize == 0)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Palette Preview", EditorStyles.boldLabel);
            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 360f) / 34f));
            int currentColumn = 0;
            EditorGUILayout.BeginHorizontal();
            for (int index = 0; index < paletteProperty.arraySize; index++)
            {
                SerializedProperty colorProperty = paletteProperty.GetArrayElementAtIndex(index);
                Rect rect = GUILayoutUtility.GetRect(28f, 22f, GUILayout.Width(28f), GUILayout.Height(22f));
                EditorGUI.DrawRect(rect, colorProperty.colorValue);
                currentColumn++;
                if (currentColumn < columns)
                    continue;

                currentColumn = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawAssetHeader(UnityEngine.Object asset)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(asset), EditorStyles.miniLabel, GUILayout.Height(18f));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping", GUILayout.Width(64f)))
                    EditorGUIUtility.PingObject(asset);
                if (GUILayout.Button("Open", GUILayout.Width(64f)))
                    AssetDatabase.OpenAsset(asset);
                if (GUILayout.Button("Select", GUILayout.Width(64f)))
                    Selection.activeObject = asset;
            }
        }
    }

    private void DrawInspector(UnityEngine.Object asset)
    {
        showInspector = EditorGUILayout.Foldout(showInspector, "Inspector", true);
        if (!showInspector)
            return;

        if (cachedEditor == null || cachedEditorTarget != asset)
        {
            DestroyCachedEditor();
            cachedEditor = Editor.CreateEditor(asset);
            cachedEditorTarget = asset;
        }

        cachedEditor?.OnInspectorGUI();
    }

    private void DrawReadOnlyProperty(SerializedObject serializedObject, string propertyPath, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
            return;

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    private void DrawObjectProperty(SerializedObject serializedObject, string propertyPath, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);

            if (property.objectReferenceValue != null && GUILayout.Button("Open", GUILayout.Width(56f)))
                AssetDatabase.OpenAsset(property.objectReferenceValue);
        }
    }

    private void DrawArrayCount(SerializedObject serializedObject, string propertyPath, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null || !property.isArray)
            return;

        EditorGUILayout.LabelField(label, property.arraySize.ToString());
    }

    private void DrawObjectArray(string title, SerializedProperty property)
    {
        if (property == null || !property.isArray)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"{title} ({property.arraySize})", EditorStyles.boldLabel);
            for (int index = 0; index < property.arraySize; index++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(element, GUIContent.none, true);

                    if (element.objectReferenceValue != null && GUILayout.Button("Open", GUILayout.Width(56f)))
                        AssetDatabase.OpenAsset(element.objectReferenceValue);
                }
            }
        }
    }

    private void CreateOrReplacePreview()
    {
        RemovePreview();

        GameObject instance = PrefabUtility.InstantiatePrefab(previewPrefab) as GameObject;
        if (instance == null)
            return;

        Undo.RegisterCreatedObjectUndo(instance, "Create Character Preview");
        instance.name = $"[Preview] {previewPrefab.name}";
        instance.transform.position = previewPosition;
        instance.transform.rotation = Quaternion.Euler(previewRotation);
        instance.transform.localScale = ResolveScale(previewScale);
        previewInstance = instance.transform;
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void RemovePreview()
    {
        if (previewInstance == null)
            previewInstance = FindPreviewInstance();

        if (previewInstance == null)
            return;

        Undo.DestroyObjectImmediate(previewInstance.gameObject);
        previewInstance = null;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void FramePreview()
    {
        if (previewInstance == null)
            previewInstance = FindPreviewInstance();

        if (previewInstance == null)
            return;

        Selection.activeTransform = previewInstance;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private Transform FindPreviewInstance()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int index = 0; index < roots.Length; index++)
        {
            if (roots[index] != null && roots[index].name.StartsWith("[Preview]", StringComparison.Ordinal))
                return roots[index].transform;
        }

        return null;
    }

    private static Vector3 ResolveScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Approximately(scale.x, 0f) ? 1f : scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : scale.y,
            Mathf.Approximately(scale.z, 0f) ? 1f : scale.z);
    }

    private void RefreshAssets()
    {
        jobs = LoadAssets("PlayerJobDefinition", new[] { "Assets/Resources/GameData/Jobs", "Assets/Resources/GameData" });
        appearanceCatalogs = LoadAssets("CharacterAppearanceCatalogAsset", new[] { "Assets/Resources/GameData/Characters", "Assets/Resources/GameData" });

        if (selectedJob == null && jobs.Count > 0)
            selectedJob = jobs[0].Asset;
        if (selectedAppearanceCatalog == null && appearanceCatalogs.Count > 0)
            selectedAppearanceCatalog = appearanceCatalogs[0].Asset;
    }

    private List<CharacterJobAssetRecord> FilterRecords(List<CharacterJobAssetRecord> source)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return source;

        string normalizedSearch = searchText.Trim();
        List<CharacterJobAssetRecord> filtered = new List<CharacterJobAssetRecord>();
        for (int index = 0; index < source.Count; index++)
        {
            CharacterJobAssetRecord record = source[index];
            if (record == null || record.Asset == null)
                continue;

            if (record.DisplayName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0
                || record.Path.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                filtered.Add(record);
            }
        }

        return filtered;
    }

    private static List<CharacterJobAssetRecord> LoadAssets(string typeName, string[] folders)
    {
        List<CharacterJobAssetRecord> records = new List<CharacterJobAssetRecord>();
        Type type = ResolveScriptableObjectType(typeName);
        if (type == null)
            return records;

        List<string> validFolders = new List<string>();
        for (int index = 0; index < folders.Length; index++)
        {
            if (AssetDatabase.IsValidFolder(folders[index]))
                validFolders.Add(folders[index]);
        }

        if (validFolders.Count == 0)
            return records;

        string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", validFolders.ToArray());
        HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            if (string.IsNullOrWhiteSpace(path) || !seenPaths.Add(path))
                continue;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null || !type.IsInstanceOfType(asset))
                continue;

            records.Add(new CharacterJobAssetRecord { Asset = asset, Path = path });
        }

        records.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
        return records;
    }

    private static Type ResolveScriptableObjectType(string typeName)
    {
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

    private static Type ResolveObjectFieldType(string typeName)
    {
        Type type = ResolveScriptableObjectType(typeName);
        return type ?? typeof(UnityEngine.Object);
    }

    private void DestroyCachedEditor()
    {
        if (cachedEditor == null)
            return;

        DestroyImmediate(cachedEditor);
        cachedEditor = null;
        cachedEditorTarget = null;
    }
}
