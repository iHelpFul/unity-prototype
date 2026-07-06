using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameDataWorkbenchWindow : EditorWindow
{
    private const float LeftPaneWidth = 320f;

    private static GUIStyle wrappedMiniLabelStyle;

    private int tabIndex;
    private string searchText = string.Empty;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private Vector2 healthScroll;
    private Vector2 sandboxScroll;
    private List<GameDataWorkbenchAssetRecord> records = new List<GameDataWorkbenchAssetRecord>();
    private List<GameDataWorkbenchAssetRecord> allGameDataRecords = new List<GameDataWorkbenchAssetRecord>();
    private List<GameDataWorkbenchHealthIssue> healthIssues = new List<GameDataWorkbenchHealthIssue>();
    private UnityEngine.Object selectedAsset;
    private Editor selectedEditor;
    private bool showInspector = true;
    private bool showOutgoingReferences = true;
    private bool showIncomingReferences = true;
    private bool showPreview = true;

    private UnityEngine.Object sandboxJob;
    private UnityEngine.Object sandboxSkill;
    private UnityEngine.Object sandboxEnemy;
    private UnityEngine.Object sandboxProjectile;
    private UnityEngine.Object sandboxCueSet;

    [MenuItem("Tools/Game Data/Workbench")]
    public static void Open()
    {
        GetWindow<GameDataWorkbenchWindow>("Game Data Workbench");
    }

    [MenuItem("Assets/Open in Game Data Workbench", false, 2000)]
    public static void OpenSelectedProjectAsset()
    {
        GameDataWorkbenchWindow window = GetWindow<GameDataWorkbenchWindow>("Game Data Workbench");
        window.SelectAssetFromProject(Selection.activeObject);
        window.Focus();
    }

    [MenuItem("Assets/Open in Game Data Workbench", true)]
    public static bool CanOpenSelectedProjectAsset()
    {
        return Selection.activeObject != null;
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    private void OnDisable()
    {
        DestroySelectedEditor();
    }

    private void OnGUI()
    {
        DrawToolbar();

        GameDataWorkbenchCategory category = CurrentCategory;
        if (category == null)
            return;

        if (category.Name == "Overview")
        {
            DrawOverview();
            return;
        }

        if (category.Name == "Health")
        {
            DrawHealthDashboard();
            return;
        }

        if (category.Name == "Sandbox")
        {
            DrawSandbox();
            return;
        }

        DrawAssetBrowser(category);
    }

    private GameDataWorkbenchCategory CurrentCategory
    {
        get
        {
            GameDataWorkbenchCategory[] categories = GameDataWorkbenchUtility.DefaultCategories;
            if (tabIndex < 0 || tabIndex >= categories.Length)
                tabIndex = 0;

            return categories[tabIndex];
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            string[] tabNames = GetTabNames();
            int newTabIndex = GUILayout.Toolbar(tabIndex, tabNames, EditorStyles.toolbarButton);
            if (newTabIndex != tabIndex)
            {
                tabIndex = newTabIndex;
                searchText = string.Empty;
                RefreshCurrentTab();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                RefreshAll();
        }
    }

    private void DrawOverview()
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Game Data Workbench", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "A portable authoring hub for browsing, linking, checking, and testing project data. It does not hide fields or mutate assets unless you press an explicit action button.",
            MessageType.Info);

        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawOverviewCard("Assets", allGameDataRecords.Count.ToString(), "Known game-data assets loaded from Resources/GameData.");
            DrawOverviewCard("Health", healthIssues.Count.ToString(), "Soft warnings and missing references found by the dashboard.");
            DrawOverviewCard("Tools", "Ready", "Browser, links, sandbox setup, library, and health dashboard.");
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open Jobs"))
                SwitchToTab("Jobs");

            if (GUILayout.Button("Open Skills")
            )
                SwitchToTab("Skills");

            if (GUILayout.Button("Open Enemies"))
                SwitchToTab("Enemies");

            if (GUILayout.Button("Open Sandbox"))
                SwitchToTab("Sandbox");

            if (GUILayout.Button("Run Health Check"))
            {
                RefreshHealth();
                SwitchToTab("Health");
            }
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Recommended Flow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Browse Jobs or Skills.\n2. Select an asset and inspect its linked assets.\n3. Use Health for soft project checks.\n4. Use Sandbox to create a quick 3D test setup.",
            MessageType.None);
    }

    private void DrawOverviewCard(string title, string value, string description)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(84f)))
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, WrappedMiniLabelStyle);
        }
    }

    private void DrawAssetBrowser(GameDataWorkbenchCategory category)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawAssetList(category);
            DrawAssetDetails(category);
        }
    }

    private void DrawAssetList(GameDataWorkbenchCategory category)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPaneWidth)))
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(category.Name, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                searchText = EditorGUILayout.TextField(searchText, GetSearchFieldStyle());
                if (GUILayout.Button("x", GetSearchCancelStyle(), GUILayout.Width(22f)))
                {
                    searchText = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{FilteredRecords.Count} / {records.Count}", EditorStyles.miniLabel);

                if (category.TypeNames.Length > 0 && GUILayout.Button("Create", GUILayout.Width(70f)))
                    ShowCreateAssetMenu(category);
            }

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            List<GameDataWorkbenchAssetRecord> filteredRecords = FilteredRecords;
            for (int index = 0; index < filteredRecords.Count; index++)
                DrawAssetListRow(filteredRecords[index]);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawAssetListRow(GameDataWorkbenchAssetRecord record)
    {
        if (record == null || record.Asset == null)
            return;

        bool isSelected = selectedAsset == record.Asset;
        GUIStyle rowStyle = isSelected ? EditorStyles.helpBox : GUIStyle.none;

        using (new EditorGUILayout.HorizontalScope(rowStyle))
        {
            Texture icon = AssetPreview.GetMiniThumbnail(record.Asset);
            GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(20f));

            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button(record.DisplayName, EditorStyles.label))
                    SelectAsset(record.Asset);

                string typeName = record.AssetType != null ? record.AssetType.Name : "Unknown";
                EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel);
            }
        }
    }

    private void DrawAssetDetails(GameDataWorkbenchCategory category)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            if (selectedAsset == null)
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox("Select an asset from the list to inspect it, view linked assets, and find incoming references.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawSelectedAssetHeader();
            DrawSelectedAssetPreview();
            DrawSelectedAssetInspector();
            DrawReferencesPanel(category);

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawSelectedAssetHeader()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(selectedAsset.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedAsset.GetType().Name, EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(selectedAsset), EditorStyles.miniLabel, GUILayout.Height(18f));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select", GUILayout.Width(72f)))
                    Selection.activeObject = selectedAsset;

                if (GUILayout.Button("Ping", GUILayout.Width(72f)))
                    EditorGUIUtility.PingObject(selectedAsset);

                if (GUILayout.Button("Open", GUILayout.Width(72f)))
                    AssetDatabase.OpenAsset(selectedAsset);

                if (GUILayout.Button("Duplicate", GUILayout.Width(92f)))
                {
                    UnityEngine.Object duplicate = GameDataWorkbenchUtility.DuplicateAsset(selectedAsset);
                    if (duplicate != null)
                    {
                        RefreshCurrentTab();
                        SelectAsset(duplicate);
                    }
                }
            }
        }
    }

    private void DrawSelectedAssetPreview()
    {
        showPreview = EditorGUILayout.Foldout(showPreview, "Preview", true);
        if (!showPreview)
            return;

        Texture2D preview = AssetPreview.GetAssetPreview(selectedAsset);
        if (preview == null)
            preview = AssetPreview.GetMiniThumbnail(selectedAsset);

        if (preview == null)
            return;

        Rect rect = GUILayoutUtility.GetRect(120f, 160f, 80f, 140f, GUILayout.ExpandWidth(false));
        GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
    }

    private void DrawSelectedAssetInspector()
    {
        showInspector = EditorGUILayout.Foldout(showInspector, "Inspector", true);
        if (!showInspector)
            return;

        EnsureSelectedEditor();
        if (selectedEditor != null)
            selectedEditor.OnInspectorGUI();
    }

    private void DrawReferencesPanel(GameDataWorkbenchCategory category)
    {
        EditorGUILayout.Space(8f);

        showOutgoingReferences = EditorGUILayout.Foldout(showOutgoingReferences, "Linked Assets", true);
        if (showOutgoingReferences)
        {
            List<GameDataWorkbenchReference> outgoing = GameDataWorkbenchUtility.GetOutgoingReferences(selectedAsset);
            DrawReferenceList(outgoing, useTarget: true);
        }

        showIncomingReferences = EditorGUILayout.Foldout(showIncomingReferences, "Used By", true);
        if (showIncomingReferences)
        {
            List<GameDataWorkbenchReference> incoming =
                GameDataWorkbenchUtility.GetIncomingReferences(selectedAsset, allGameDataRecords);
            DrawReferenceList(incoming, useTarget: false);
        }
    }

    private void DrawReferenceList(List<GameDataWorkbenchReference> references, bool useTarget)
    {
        if (references == null || references.Count == 0)
        {
            EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
            return;
        }

        for (int index = 0; index < references.Count; index++)
        {
            GameDataWorkbenchReference reference = references[index];
            UnityEngine.Object objectToDraw = useTarget ? reference.Target : reference.Source;
            if (objectToDraw == null)
                continue;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.ObjectField(objectToDraw, typeof(UnityEngine.Object), false);

                if (GUILayout.Button("Select", GUILayout.Width(64f)))
                    SelectAsset(objectToDraw);

                if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    EditorGUIUtility.PingObject(objectToDraw);
            }

            EditorGUILayout.LabelField(reference.PropertyPath, EditorStyles.miniLabel);
        }
    }

    private void DrawHealthDashboard()
    {
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Health Dashboard", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
                RefreshHealth();
        }

        EditorGUILayout.HelpBox(
            "Soft checks only. This window reports missing references and duplicate IDs, but does not modify assets.",
            MessageType.Info);

        healthScroll = EditorGUILayout.BeginScrollView(healthScroll);
        if (healthIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues found in the current scan.", MessageType.Info);
        }
        else
        {
            for (int index = 0; index < healthIssues.Count; index++)
                DrawHealthIssue(healthIssues[index]);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawHealthIssue(GameDataWorkbenchHealthIssue issue)
    {
        if (issue == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox($"{issue.Title}\n{issue.Details}", issue.Severity);

            if (issue.Context != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(issue.Context, typeof(UnityEngine.Object), false);

                    if (GUILayout.Button("Select", GUILayout.Width(64f)))
                        SelectAsset(issue.Context);

                    if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                        EditorGUIUtility.PingObject(issue.Context);
                }
            }
        }
    }

    private void DrawSandbox()
    {
        sandboxScroll = EditorGUILayout.BeginScrollView(sandboxScroll);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Combat / 3D Sandbox", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "A lightweight launcher for test setup and quick authoring context. It keeps references here for convenience and can create a basic 3D test scene setup in the currently open scene.",
            MessageType.Info);

        sandboxJob = EditorGUILayout.ObjectField("Job", sandboxJob, typeof(UnityEngine.Object), false);
        sandboxSkill = EditorGUILayout.ObjectField("Skill", sandboxSkill, typeof(UnityEngine.Object), false);
        sandboxEnemy = EditorGUILayout.ObjectField("Enemy", sandboxEnemy, typeof(UnityEngine.Object), false);
        sandboxProjectile = EditorGUILayout.ObjectField("Projectile", sandboxProjectile, typeof(UnityEngine.Object), false);
        sandboxCueSet = EditorGUILayout.ObjectField("Cue Set", sandboxCueSet, typeof(UnityEngine.Object), false);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Basic 3D Test Setup"))
                GameDataWorkbenchUtility.CreateBasic3DTestSetup();

            if (GUILayout.Button("Clear Picks"))
            {
                sandboxJob = null;
                sandboxSkill = null;
                sandboxEnemy = null;
                sandboxProjectile = null;
                sandboxCueSet = null;
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Picked Asset Links", EditorStyles.boldLabel);
        DrawSandboxLinks(sandboxJob);
        DrawSandboxLinks(sandboxSkill);
        DrawSandboxLinks(sandboxEnemy);
        DrawSandboxLinks(sandboxProjectile);
        DrawSandboxLinks(sandboxCueSet);

        EditorGUILayout.EndScrollView();
    }

    private void DrawSandboxLinks(UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(asset, typeof(UnityEngine.Object), false);
                if (GUILayout.Button("Inspect", GUILayout.Width(70f)))
                    SelectAsset(asset);
                if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    EditorGUIUtility.PingObject(asset);
            }

            List<GameDataWorkbenchReference> outgoing = GameDataWorkbenchUtility.GetOutgoingReferences(asset);
            EditorGUILayout.LabelField($"Linked Assets: {outgoing.Count}", EditorStyles.miniLabel);
        }
    }

    private void ShowCreateAssetMenu(GameDataWorkbenchCategory category)
    {
        GenericMenu menu = new GenericMenu();
        for (int index = 0; index < category.TypeNames.Length; index++)
        {
            string typeName = category.TypeNames[index];
            Type type = GameDataWorkbenchUtility.ResolveType(typeName);
            if (type == null)
                continue;

            menu.AddItem(new GUIContent(type.Name), false, () => CreateAssetOfType(type, category));
        }

        menu.ShowAsContext();
    }

    private void CreateAssetOfType(Type type, GameDataWorkbenchCategory category)
    {
        if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
            return;

        string defaultFolder = ResolveDefaultCreateFolder(category);
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Game Data Asset",
            $"New {type.Name}.asset",
            "asset",
            $"Choose where to create {type.Name}.",
            defaultFolder);

        if (string.IsNullOrWhiteSpace(path))
            return;

        ScriptableObject asset = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshCurrentTab();
        SelectAsset(asset);
    }

    private string ResolveDefaultCreateFolder(GameDataWorkbenchCategory category)
    {
        if (category != null && category.SearchFolders != null)
        {
            for (int index = 0; index < category.SearchFolders.Length; index++)
            {
                string folder = category.SearchFolders[index];
                if (AssetDatabase.IsValidFolder(folder))
                    return folder;
            }
        }

        return AssetDatabase.IsValidFolder("Assets/Resources/GameData")
            ? "Assets/Resources/GameData"
            : "Assets";
    }

    private void SelectAsset(UnityEngine.Object asset)
    {
        if (selectedAsset == asset)
        {
            Selection.activeObject = asset;
            Repaint();
            return;
        }

        selectedAsset = asset;
        DestroySelectedEditor();
        detailScroll = Vector2.zero;
        Selection.activeObject = asset;
        Repaint();
    }

    private void SelectAssetFromProject(UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        string selectedPath = AssetDatabase.GetAssetPath(asset);
        GameDataWorkbenchCategory[] categories = GameDataWorkbenchUtility.DefaultCategories;
        for (int index = 0; index < categories.Length; index++)
        {
            GameDataWorkbenchCategory category = categories[index];
            if (category == null || category.Name == "Overview" || category.Name == "Health" || category.Name == "Sandbox")
                continue;

            List<GameDataWorkbenchAssetRecord> categoryRecords = GameDataWorkbenchUtility.LoadCategoryAssets(category);
            for (int recordIndex = 0; recordIndex < categoryRecords.Count; recordIndex++)
            {
                GameDataWorkbenchAssetRecord record = categoryRecords[recordIndex];
                if (record == null)
                    continue;

                if (record.Asset != asset && !string.Equals(record.Path, selectedPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                tabIndex = index;
                records = categoryRecords;
                searchText = string.Empty;
                SelectAsset(asset);
                return;
            }
        }

        SelectAsset(asset);
    }

    private void EnsureSelectedEditor()
    {
        if (selectedAsset == null)
            return;

        if (selectedEditor != null && selectedEditor.target == selectedAsset)
            return;

        DestroySelectedEditor();
        selectedEditor = Editor.CreateEditor(selectedAsset);
    }

    private void DestroySelectedEditor()
    {
        if (selectedEditor == null)
            return;

        DestroyImmediate(selectedEditor);
        selectedEditor = null;
    }

    private void RefreshAll()
    {
        RefreshCurrentTab();
        RefreshAllGameData();
        RefreshHealth();
    }

    private void RefreshCurrentTab()
    {
        GameDataWorkbenchCategory category = CurrentCategory;
        if (category == null || category.Name == "Overview" || category.Name == "Health" || category.Name == "Sandbox")
        {
            records = new List<GameDataWorkbenchAssetRecord>();
            return;
        }

        records = GameDataWorkbenchUtility.LoadCategoryAssets(category);
    }

    private void RefreshAllGameData()
    {
        allGameDataRecords = GameDataWorkbenchUtility.LoadAllGameDataAssets();
    }

    private void RefreshHealth()
    {
        if (allGameDataRecords == null || allGameDataRecords.Count == 0)
            RefreshAllGameData();

        healthIssues = GameDataWorkbenchUtility.AnalyzeHealth(allGameDataRecords);
    }

    private void SwitchToTab(string tabName)
    {
        GameDataWorkbenchCategory[] categories = GameDataWorkbenchUtility.DefaultCategories;
        for (int index = 0; index < categories.Length; index++)
        {
            if (!string.Equals(categories[index].Name, tabName, StringComparison.Ordinal))
                continue;

            tabIndex = index;
            RefreshCurrentTab();
            Repaint();
            return;
        }
    }

    private string[] GetTabNames()
    {
        GameDataWorkbenchCategory[] categories = GameDataWorkbenchUtility.DefaultCategories;
        string[] names = new string[categories.Length];
        for (int index = 0; index < categories.Length; index++)
            names[index] = categories[index].Name;

        return names;
    }

    private static GUIStyle GetSearchFieldStyle()
    {
        GUIStyle style = GUI.skin.FindStyle("ToolbarSearchTextField");
        return style ?? EditorStyles.toolbarTextField;
    }

    private static GUIStyle GetSearchCancelStyle()
    {
        GUIStyle style = GUI.skin.FindStyle("ToolbarSearchCancelButton");
        return style ?? EditorStyles.toolbarButton;
    }

    private static GUIStyle WrappedMiniLabelStyle
    {
        get
        {
            if (wrappedMiniLabelStyle == null)
                wrappedMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

            return wrappedMiniLabelStyle;
        }
    }

    private List<GameDataWorkbenchAssetRecord> FilteredRecords
    {
        get
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return records;

            string normalizedSearch = searchText.Trim();
            List<GameDataWorkbenchAssetRecord> filtered = new List<GameDataWorkbenchAssetRecord>();
            for (int index = 0; index < records.Count; index++)
            {
                GameDataWorkbenchAssetRecord record = records[index];
                if (record == null || record.Asset == null)
                    continue;

                if (record.DisplayName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0
                    || record.Path.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0
                    || (record.AssetType != null && record.AssetType.Name.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    filtered.Add(record);
                }
            }

            return filtered;
        }
    }
}
