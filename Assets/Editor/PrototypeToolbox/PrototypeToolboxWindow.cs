using UnityEditor;
using UnityEngine;

public class PrototypeToolboxWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("Tools/Prototype Toolbox")]
    public static void Open()
    {
        GetWindow<PrototypeToolboxWindow>("Prototype Toolbox");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prototype Toolbox", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "A single launcher for the portable editor tools in this project. Each tool lives in its own folder under Assets/Editor so it can be copied into future projects.",
            MessageType.Info);

        DrawToolCard(
            "Game Data Workbench",
            "Browse, create, duplicate, inspect, and health-check ScriptableObject game data.",
            "Tools/Game Data/Workbench",
            "Assets/Editor/GameDataWorkbench");

        DrawToolCard(
            "World Authoring Workbench",
            "Browse prefabs, place scene objects, create spawn points, and scan scene health.",
            "Tools/World Authoring/Workbench",
            "Assets/Editor/WorldAuthoringWorkbench");

        DrawToolCard(
            "Balance Board",
            "Tune enemies, skills, basic attacks, projectiles, and burst profiles in fast tables.",
            "Tools/Combat/Balance Board",
            "Assets/Editor/BalanceBoard");

        DrawToolCard(
            "Quest NPC Flow Builder",
            "Inspect and link quests, NPCs, vendors, objectives, rewards, and dialogue flow.",
            "Tools/Quest NPC/Flow Builder",
            "Assets/Editor/QuestNpcFlowBuilder");

        DrawToolCard(
            "Character Job Preview Lab",
            "Inspect job definitions, connected combat profiles, appearance catalogs, and preview prefabs.",
            "Tools/Characters/Job Preview Lab",
            "Assets/Editor/CharacterJobPreviewLab");

        DrawToolCard(
            "Combat Test Runner",
            "Dry-preview skill/basic damage against enemy definitions before Play Mode testing.",
            "Tools/Combat/Test Runner",
            "Assets/Editor/CombatTestRunner");

        DrawToolCard(
            "Building Placement Library",
            "Place base/building prefabs as singles, lines, grids, or circles with snapping and ground alignment.",
            "Tools/Building/Base Placement Library",
            "Assets/Editor/BuildingPlacementLibrary");

        DrawToolCard(
            "Project Health Dashboard",
            "Scan ScriptableObjects, prefabs, animation clips, and the active scene for common issues.",
            "Tools/Project Health/Dashboard",
            "Assets/Editor/ProjectHealthDashboard");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Existing Project Utilities", EditorStyles.boldLabel);
        DrawMenuButton("Assign Missing Enemy IDs", "Tools/Multiplayer Prototype/Scene Entity IDs/Assign Missing Enemy IDs");
        DrawMenuButton("Validate Enemy IDs", "Tools/Multiplayer Prototype/Scene Entity IDs/Validate Enemy IDs");
        DrawMenuButton("Multiplayer Prototype Setup", "Tools/Multiplayer Prototype/Setup Project");

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolCard(string title, string description, string menuPath, string folderPath)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, WrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Tool"))
                    ExecuteMenuItem(menuPath);

                if (GUILayout.Button("Ping Folder", GUILayout.Width(96f)))
                    PingFolder(folderPath);
            }
        }
    }

    private void DrawMenuButton(string label, string menuPath)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Run", GUILayout.Width(72f)))
                ExecuteMenuItem(menuPath);
        }
    }

    private static void ExecuteMenuItem(string menuPath)
    {
        if (!EditorApplication.ExecuteMenuItem(menuPath))
            Debug.LogWarning($"[PrototypeToolbox] Menu item not found or could not execute: {menuPath}");
    }

    private static void PingFolder(string folderPath)
    {
        UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
        if (folder == null)
        {
            Debug.LogWarning($"[PrototypeToolbox] Folder not found: {folderPath}");
            return;
        }

        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
    }

    private static GUIStyle wrappedMiniLabel;

    private static GUIStyle WrappedMiniLabel
    {
        get
        {
            if (wrappedMiniLabel == null)
                wrappedMiniLabel = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

            return wrappedMiniLabel;
        }
    }
}
