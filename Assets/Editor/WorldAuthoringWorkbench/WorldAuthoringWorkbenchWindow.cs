using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldAuthoringWorkbenchWindow : EditorWindow
{
    private static readonly string[] Tabs = { "Prefabs", "Scene Health", "Selection Tools" };

    private readonly WorldAuthoringPlacementSettings placementSettings = new WorldAuthoringPlacementSettings();

    private int tabIndex;
    private string prefabFolder = "Assets/Prefabs";
    private string prefabSearch = string.Empty;
    private WorldAuthoringPrefabCategory prefabCategory = WorldAuthoringPrefabCategory.All;
    private List<WorldAuthoringPrefabRecord> prefabRecords = new List<WorldAuthoringPrefabRecord>();
    private GameObject selectedPrefab;
    private Editor selectedPrefabEditor;
    private Vector2 prefabListScroll;
    private Vector2 prefabDetailScroll;
    private Vector2 sceneScroll;
    private Vector2 selectionScroll;
    private bool showPlacement = true;
    private bool showPrefabInspector = true;
    private WorldAuthoringSceneSummary sceneSummary;
    private string newSpawnId = "default";
    private bool newSpawnIsDefault = true;

    [MenuItem("Tools/World Authoring/Workbench")]
    public static void Open()
    {
        GetWindow<WorldAuthoringWorkbenchWindow>("World Authoring");
    }

    private void OnEnable()
    {
        RefreshPrefabs();
        RefreshSceneSummary();
    }

    private void OnDisable()
    {
        DestroySelectedPrefabEditor();
    }

    private void OnGUI()
    {
        DrawToolbar();

        switch (tabIndex)
        {
            case 0:
                DrawPrefabsTab();
                break;
            case 1:
                DrawSceneHealthTab();
                break;
            case 2:
                DrawSelectionToolsTab();
                break;
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            tabIndex = GUILayout.Toolbar(tabIndex, Tabs, EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                RefreshPrefabs();
                RefreshSceneSummary();
            }
        }
    }

    private void DrawPrefabsTab()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPrefabList();
            DrawPrefabDetails();
        }
    }

    private void DrawPrefabList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(330f)))
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Prefab Library", EditorStyles.boldLabel);

            DrawFolderPicker();

            EditorGUI.BeginChangeCheck();
            prefabCategory = (WorldAuthoringPrefabCategory)EditorGUILayout.EnumPopup("Category", prefabCategory);
            prefabSearch = EditorGUILayout.TextField("Search", prefabSearch);
            if (EditorGUI.EndChangeCheck())
                RefreshPrefabs();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{prefabRecords.Count} prefabs", EditorStyles.miniLabel);
                if (GUILayout.Button("Reload", GUILayout.Width(70f)))
                    RefreshPrefabs();
            }

            prefabListScroll = EditorGUILayout.BeginScrollView(prefabListScroll);
            for (int index = 0; index < prefabRecords.Count; index++)
                DrawPrefabRow(prefabRecords[index]);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawFolderPicker()
    {
        DefaultAsset currentFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(prefabFolder);
        EditorGUI.BeginChangeCheck();
        DefaultAsset selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", currentFolder, typeof(DefaultAsset), false);
        if (!EditorGUI.EndChangeCheck())
            return;

        if (selectedFolder == null)
            return;

        string path = AssetDatabase.GetAssetPath(selectedFolder);
        if (!AssetDatabase.IsValidFolder(path))
            return;

        prefabFolder = path;
        RefreshPrefabs();
    }

    private void DrawPrefabRow(WorldAuthoringPrefabRecord record)
    {
        if (record == null || record.Prefab == null)
            return;

        bool isSelected = selectedPrefab == record.Prefab;
        GUIStyle style = isSelected ? EditorStyles.helpBox : GUIStyle.none;

        using (new EditorGUILayout.HorizontalScope(style))
        {
            Texture icon = AssetPreview.GetMiniThumbnail(record.Prefab);
            GUILayout.Label(icon, GUILayout.Width(24f), GUILayout.Height(24f));

            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button(record.DisplayName, EditorStyles.label))
                    SelectPrefab(record.Prefab);

                EditorGUILayout.LabelField($"{record.Category}  |  {record.Path}", EditorStyles.miniLabel);
            }
        }
    }

    private void DrawPrefabDetails()
    {
        prefabDetailScroll = EditorGUILayout.BeginScrollView(prefabDetailScroll);

        if (selectedPrefab == null)
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox("Select a prefab to inspect it and place it into the current scene.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(selectedPrefab.name, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(selectedPrefab), EditorStyles.miniLabel, GUILayout.Height(18f));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping", GUILayout.Width(70f)))
                    EditorGUIUtility.PingObject(selectedPrefab);

                if (GUILayout.Button("Open", GUILayout.Width(70f)))
                    AssetDatabase.OpenAsset(selectedPrefab);

                if (GUILayout.Button("Select", GUILayout.Width(70f)))
                    Selection.activeObject = selectedPrefab;
            }
        }

        DrawPrefabPreview();
        DrawPlacementSettings();
        DrawPlacementActions();
        DrawPrefabInspector();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPrefabPreview()
    {
        Texture2D preview = AssetPreview.GetAssetPreview(selectedPrefab);
        if (preview == null)
            preview = AssetPreview.GetMiniThumbnail(selectedPrefab);

        if (preview == null)
            return;

        Rect rect = GUILayoutUtility.GetRect(160f, 220f, 110f, 170f, GUILayout.ExpandWidth(false));
        GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
    }

    private void DrawPlacementSettings()
    {
        showPlacement = EditorGUILayout.Foldout(showPlacement, "Placement Settings", true);
        if (!showPlacement)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            placementSettings.UseParent = EditorGUILayout.Toggle("Use Parent Container", placementSettings.UseParent);
            using (new EditorGUI.DisabledScope(!placementSettings.UseParent))
                placementSettings.ParentName = EditorGUILayout.TextField("Parent Name", placementSettings.ParentName);

            placementSettings.SnapToGrid = EditorGUILayout.Toggle("Snap To Grid", placementSettings.SnapToGrid);
            using (new EditorGUI.DisabledScope(!placementSettings.SnapToGrid))
                placementSettings.GridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", placementSettings.GridSize));

            placementSettings.AlignToGround = EditorGUILayout.Toggle("Align To Ground", placementSettings.AlignToGround);
            using (new EditorGUI.DisabledScope(!placementSettings.AlignToGround))
                placementSettings.GroundMask = DrawLayerMask("Ground Mask", placementSettings.GroundMask);

            placementSettings.PositionOffset = EditorGUILayout.Vector3Field("Position Offset", placementSettings.PositionOffset);
            placementSettings.RotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", placementSettings.RotationOffset);
            placementSettings.Scale = EditorGUILayout.Vector3Field("Scale", placementSettings.Scale);
            placementSettings.RandomizeYaw = EditorGUILayout.Toggle("Randomize Yaw", placementSettings.RandomizeYaw);
            placementSettings.UseFacingYawOverride = EditorGUILayout.Toggle("Use Facing Yaw", placementSettings.UseFacingYawOverride);
            using (new EditorGUI.DisabledScope(!placementSettings.UseFacingYawOverride))
                placementSettings.FacingYaw = EditorGUILayout.FloatField("Facing Yaw", placementSettings.FacingYaw);
        }
    }

    private void DrawPlacementActions()
    {
        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Place At Scene Pivot"))
                WorldAuthoringWorkbenchUtility.PlacePrefab(selectedPrefab, WorldAuthoringWorkbenchUtility.GetSceneViewPivot(), placementSettings);

            if (GUILayout.Button("Place At Selection"))
                WorldAuthoringWorkbenchUtility.PlacePrefab(selectedPrefab, WorldAuthoringWorkbenchUtility.GetSelectedPivotOrScenePivot(), placementSettings);

            if (GUILayout.Button("Place At Origin"))
                WorldAuthoringWorkbenchUtility.PlacePrefab(selectedPrefab, Vector3.zero, placementSettings);
        }
    }

    private void DrawPrefabInspector()
    {
        showPrefabInspector = EditorGUILayout.Foldout(showPrefabInspector, "Prefab Inspector", true);
        if (!showPrefabInspector)
            return;

        EnsureSelectedPrefabEditor();
        selectedPrefabEditor?.OnInspectorGUI();
    }

    private void DrawSceneHealthTab()
    {
        sceneScroll = EditorGUILayout.BeginScrollView(sceneScroll);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Scene Health", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scene checks are non-destructive. Fix buttons are explicit and use Undo where possible.",
            MessageType.Info);

        if (sceneSummary == null)
            RefreshSceneSummary();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSummaryCard("Enemies", sceneSummary.EnemyCount.ToString());
            DrawSummaryCard("Spawn Points", sceneSummary.SpawnPointCount.ToString());
            DrawSummaryCard("Default Spawns", sceneSummary.DefaultSpawnPointCount.ToString());
            DrawSummaryCard("Issues", sceneSummary.Issues.Count.ToString());
        }

        EditorGUILayout.Space(10f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Scene"))
                RefreshSceneSummary();

            if (GUILayout.Button("Assign Missing Enemy IDs"))
            {
                int assigned = WorldAuthoringWorkbenchUtility.AssignMissingEnemySceneIds();
                RefreshSceneSummary();
                ShowNotification(new GUIContent($"Assigned {assigned} enemy IDs"));
            }
        }

        EditorGUILayout.Space(10f);
        DrawSpawnPointCreator();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);
        if (sceneSummary.Issues.Count == 0)
        {
            EditorGUILayout.HelpBox("No scene issues found.", MessageType.Info);
        }
        else
        {
            for (int index = 0; index < sceneSummary.Issues.Count; index++)
                DrawSceneIssue(sceneSummary.Issues[index]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSummaryCard(string title, string value)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(58f)))
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
        }
    }

    private void DrawSpawnPointCreator()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Create Spawn Point", EditorStyles.boldLabel);
            newSpawnId = EditorGUILayout.TextField("Spawn ID", newSpawnId);
            newSpawnIsDefault = EditorGUILayout.Toggle("Is Default", newSpawnIsDefault);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create At Scene Pivot"))
                {
                    WorldAuthoringWorkbenchUtility.CreateSpawnPoint(
                        newSpawnId,
                        newSpawnIsDefault,
                        WorldAuthoringWorkbenchUtility.GetSceneViewPivot());
                    RefreshSceneSummary();
                }

                if (GUILayout.Button("Create At Selection"))
                {
                    WorldAuthoringWorkbenchUtility.CreateSpawnPoint(
                        newSpawnId,
                        newSpawnIsDefault,
                        WorldAuthoringWorkbenchUtility.GetSelectedPivotOrScenePivot());
                    RefreshSceneSummary();
                }
            }
        }
    }

    private void DrawSceneIssue(WorldAuthoringSceneIssue issue)
    {
        if (issue == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox($"{issue.Title}\n{issue.Details}", issue.Severity);
            if (issue.Context == null)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(issue.Context, typeof(UnityEngine.Object), true);
                if (GUILayout.Button("Select", GUILayout.Width(64f)))
                    Selection.activeObject = issue.Context;
                if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    EditorGUIUtility.PingObject(issue.Context);
            }
        }
    }

    private void DrawSelectionToolsTab()
    {
        selectionScroll = EditorGUILayout.BeginScrollView(selectionScroll);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Selection Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Quick cleanup actions for selected scene objects. These are designed for arranging a 3D scene fast without hunting through menus.",
            MessageType.Info);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Transform Cleanup", EditorStyles.boldLabel);

            if (GUILayout.Button("Snap Selection To Grid"))
            {
                int count = WorldAuthoringWorkbenchUtility.SnapSelection(placementSettings.GridSize);
                ShowNotification(new GUIContent($"Snapped {count} objects"));
            }

            if (GUILayout.Button("Align Selection To Ground"))
            {
                int count = WorldAuthoringWorkbenchUtility.AlignSelectionToGround(placementSettings.GroundMask);
                ShowNotification(new GUIContent($"Aligned {count} objects"));
            }

            if (GUILayout.Button("Parent Selection To Container"))
            {
                int count = WorldAuthoringWorkbenchUtility.ParentSelection(placementSettings.ParentName);
                ShowNotification(new GUIContent($"Parented {count} objects"));
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Prefab From Library", EditorStyles.boldLabel);
            GameObject pickedPrefab = (GameObject)EditorGUILayout.ObjectField("Selected Prefab", selectedPrefab, typeof(GameObject), false);
            if (pickedPrefab != selectedPrefab)
                SelectPrefab(pickedPrefab);

            using (new EditorGUI.DisabledScope(selectedPrefab == null))
            {
                if (GUILayout.Button("Place Prefab At Each Selected Object"))
                    PlacePrefabAtEachSelectedObject();
            }
        }

        DrawPlacementSettings();
        EditorGUILayout.EndScrollView();
    }

    private void PlacePrefabAtEachSelectedObject()
    {
        if (selectedPrefab == null)
            return;

        Transform[] targets = Selection.transforms;
        List<Vector3> positions = new List<Vector3>(targets.Length);
        for (int index = 0; index < targets.Length; index++)
        {
            if (targets[index] != null)
                positions.Add(targets[index].position);
        }

        for (int index = 0; index < positions.Count; index++)
            WorldAuthoringWorkbenchUtility.PlacePrefab(selectedPrefab, positions[index], placementSettings);

        ShowNotification(new GUIContent($"Placed {positions.Count} prefabs"));
    }

    private void RefreshPrefabs()
    {
        prefabRecords = WorldAuthoringWorkbenchUtility.LoadPrefabs(prefabFolder, prefabCategory, prefabSearch);
        if (selectedPrefab != null)
        {
            bool stillVisible = false;
            for (int index = 0; index < prefabRecords.Count; index++)
            {
                if (prefabRecords[index].Prefab != selectedPrefab)
                    continue;

                stillVisible = true;
                break;
            }

            if (!stillVisible)
                SelectPrefab(null);
        }
    }

    private void RefreshSceneSummary()
    {
        sceneSummary = WorldAuthoringWorkbenchUtility.AnalyzeCurrentScene();
    }

    private void SelectPrefab(GameObject prefab)
    {
        if (selectedPrefab == prefab)
            return;

        selectedPrefab = prefab;
        DestroySelectedPrefabEditor();
        prefabDetailScroll = Vector2.zero;
        Repaint();
    }

    private void EnsureSelectedPrefabEditor()
    {
        if (selectedPrefab == null)
            return;

        if (selectedPrefabEditor != null && selectedPrefabEditor.target == selectedPrefab)
            return;

        DestroySelectedPrefabEditor();
        selectedPrefabEditor = Editor.CreateEditor(selectedPrefab);
    }

    private void DestroySelectedPrefabEditor()
    {
        if (selectedPrefabEditor == null)
            return;

        DestroyImmediate(selectedPrefabEditor);
        selectedPrefabEditor = null;
    }

    private static LayerMask DrawLayerMask(string label, LayerMask layerMask)
    {
        List<string> layerNames = new List<string>();
        List<int> layerNumbers = new List<int>();
        for (int index = 0; index < 32; index++)
        {
            string layerName = LayerMask.LayerToName(index);
            if (string.IsNullOrWhiteSpace(layerName))
                continue;

            layerNames.Add(layerName);
            layerNumbers.Add(index);
        }

        int maskWithoutEmptyLayers = 0;
        for (int index = 0; index < layerNumbers.Count; index++)
        {
            if ((layerMask.value & (1 << layerNumbers[index])) != 0)
                maskWithoutEmptyLayers |= 1 << index;
        }

        int newMaskWithoutEmptyLayers = EditorGUILayout.MaskField(label, maskWithoutEmptyLayers, layerNames.ToArray());
        int newMask = 0;
        for (int index = 0; index < layerNumbers.Count; index++)
        {
            if ((newMaskWithoutEmptyLayers & (1 << index)) != 0)
                newMask |= 1 << layerNumbers[index];
        }

        return newMask;
    }
}
