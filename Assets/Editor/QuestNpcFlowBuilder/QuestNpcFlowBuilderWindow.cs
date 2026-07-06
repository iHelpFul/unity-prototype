using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal sealed class QuestNpcAssetRecord
{
    public UnityEngine.Object Asset;
    public string Path;

    public string DisplayName => Asset != null ? Asset.name : "<Missing Asset>";
}

internal sealed class QuestNpcFlowIssue
{
    public MessageType Severity;
    public string Title;
    public string Details;
    public UnityEngine.Object Context;
}

public class QuestNpcFlowBuilderWindow : EditorWindow
{
    private static readonly string[] Tabs = { "Quests", "NPCs", "Vendors", "Health" };

    private int tabIndex;
    private string searchText = string.Empty;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private Vector2 healthScroll;
    private List<QuestNpcAssetRecord> quests = new List<QuestNpcAssetRecord>();
    private List<QuestNpcAssetRecord> npcs = new List<QuestNpcAssetRecord>();
    private List<QuestNpcAssetRecord> vendors = new List<QuestNpcAssetRecord>();
    private List<QuestNpcFlowIssue> issues = new List<QuestNpcFlowIssue>();
    private UnityEngine.Object selectedQuest;
    private UnityEngine.Object selectedNpc;
    private UnityEngine.Object selectedVendor;
    private Editor cachedEditor;
    private UnityEngine.Object cachedEditorTarget;
    private bool showInspector = true;
    private bool showLinks = true;

    [MenuItem("Tools/Quest NPC/Flow Builder")]
    public static void Open()
    {
        GetWindow<QuestNpcFlowBuilderWindow>("Quest NPC Flow");
    }

    private void OnEnable()
    {
        RefreshAll();
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
                DrawQuestTab();
                break;
            case 1:
                DrawNpcTab();
                break;
            case 2:
                DrawVendorTab();
                break;
            case 3:
                DrawHealthTab();
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
                RefreshAll();
        }
    }

    private void DrawQuestTab()
    {
        DrawAssetBrowser("Quests", quests, ref selectedQuest, DrawQuestDetails);
    }

    private void DrawNpcTab()
    {
        DrawAssetBrowser("NPCs", npcs, ref selectedNpc, DrawNpcDetails);
    }

    private void DrawVendorTab()
    {
        DrawAssetBrowser("Vendors", vendors, ref selectedVendor, DrawVendorDetails);
    }

    private void DrawAssetBrowser(
        string title,
        List<QuestNpcAssetRecord> records,
        ref UnityEngine.Object selectedAsset,
        Action<UnityEngine.Object> drawDetails)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(330f)))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                searchText = EditorGUILayout.TextField("Search", searchText);

                List<QuestNpcAssetRecord> filtered = FilterRecords(records);
                EditorGUILayout.LabelField($"{filtered.Count} / {records.Count}", EditorStyles.miniLabel);

                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                for (int index = 0; index < filtered.Count; index++)
                    DrawAssetRow(filtered[index], ref selectedAsset);
                EditorGUILayout.EndScrollView();
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            if (selectedAsset == null)
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox($"Select one of the {title} assets to inspect flow links.", MessageType.Info);
            }
            else
            {
                drawDetails(selectedAsset);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawAssetRow(QuestNpcAssetRecord record, ref UnityEngine.Object selectedAsset)
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
                    DestroyCachedEditor();
                    Selection.activeObject = record.Asset;
                }

                EditorGUILayout.LabelField(record.Path, EditorStyles.miniLabel);
            }
        }
    }

    private void DrawQuestDetails(UnityEngine.Object quest)
    {
        DrawAssetHeader(quest);
        DrawQuestSummary(quest);
        DrawQuestLinks(quest);
        DrawInspector(quest);
    }

    private void DrawNpcDetails(UnityEngine.Object npc)
    {
        DrawAssetHeader(npc);
        DrawNpcSummary(npc);
        DrawNpcLinks(npc);
        DrawInspector(npc);
    }

    private void DrawVendorDetails(UnityEngine.Object vendor)
    {
        DrawAssetHeader(vendor);
        DrawVendorSummary(vendor);
        DrawInspector(vendor);
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

    private void DrawQuestSummary(UnityEngine.Object quest)
    {
        SerializedObject serializedObject = new SerializedObject(quest);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Quest Flow", EditorStyles.boldLabel);
            DrawReadOnlyProperty(serializedObject, "questId", "ID");
            DrawReadOnlyProperty(serializedObject, "questTitle", "Title");
            DrawReadOnlyProperty(serializedObject, "minimumPlayerLevel", "Min Level");
            DrawReadOnlyProperty(serializedObject, "repeatable", "Repeatable");
            DrawReadOnlyProperty(serializedObject, "expReward", "EXP");
            DrawReadOnlyProperty(serializedObject, "mesosReward", "Mesos");
            DrawArrayCount(serializedObject, "starterNpcs", "Starter NPCs");
            DrawArrayCount(serializedObject, "completionNpcs", "Completion NPCs");
            DrawArrayCount(serializedObject, "objectives", "Objectives");
            DrawArrayCount(serializedObject, "rewardItems", "Reward Items");
            DrawArrayCount(serializedObject, "introPages", "Intro Pages");
            DrawArrayCount(serializedObject, "inProgressPages", "Progress Pages");
            DrawArrayCount(serializedObject, "completionPages", "Completion Pages");
        }
    }

    private void DrawQuestLinks(UnityEngine.Object quest)
    {
        showLinks = EditorGUILayout.Foldout(showLinks, "NPC Links", true);
        if (!showLinks)
            return;

        List<UnityEngine.Object> starterNpcs = GetObjectArrayReferences(quest, "starterNpcs");
        List<UnityEngine.Object> completionNpcs = GetObjectArrayReferences(quest, "completionNpcs");
        List<UnityEngine.Object> npcQuestLists = FindNpcQuestListsContaining(quest);

        DrawObjectList("Starter NPCs", starterNpcs);
        DrawObjectList("Completion NPCs", completionNpcs);
        DrawObjectList("NPC quest lists containing this quest", npcQuestLists);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Link Selected NPC", EditorStyles.boldLabel);
            selectedNpc = EditorGUILayout.ObjectField("NPC", selectedNpc, ResolveObjectFieldType("NpcDefinition"), false);

            using (new EditorGUI.DisabledScope(selectedNpc == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add As Starter"))
                        AddObjectToArray(quest, "starterNpcs", selectedNpc);

                    if (GUILayout.Button("Add As Completion"))
                        AddObjectToArray(quest, "completionNpcs", selectedNpc);

                    if (GUILayout.Button("Add Quest To NPC"))
                        AddObjectToArray(selectedNpc, "quests", quest);
                }
            }
        }
    }

    private void DrawNpcSummary(UnityEngine.Object npc)
    {
        SerializedObject serializedObject = new SerializedObject(npc);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("NPC", EditorStyles.boldLabel);
            DrawReadOnlyProperty(serializedObject, "npcId", "ID");
            DrawReadOnlyProperty(serializedObject, "displayName", "Name");
            DrawArrayCount(serializedObject, "quests", "Quest List");
        }
    }

    private void DrawNpcLinks(UnityEngine.Object npc)
    {
        List<UnityEngine.Object> directQuests = GetObjectArrayReferences(npc, "quests");
        List<UnityEngine.Object> starterFor = FindQuestsReferencingNpc(npc, "starterNpcs");
        List<UnityEngine.Object> completionFor = FindQuestsReferencingNpc(npc, "completionNpcs");

        DrawObjectList("Direct Quest List", directQuests);
        DrawObjectList("Starter For", starterFor);
        DrawObjectList("Completion For", completionFor);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Link Selected Quest", EditorStyles.boldLabel);
            selectedQuest = EditorGUILayout.ObjectField("Quest", selectedQuest, ResolveObjectFieldType("NpcQuestDefinition"), false);

            using (new EditorGUI.DisabledScope(selectedQuest == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Quest To NPC"))
                        AddObjectToArray(npc, "quests", selectedQuest);

                    if (GUILayout.Button("Mark Starter"))
                        AddObjectToArray(selectedQuest, "starterNpcs", npc);

                    if (GUILayout.Button("Mark Completion"))
                        AddObjectToArray(selectedQuest, "completionNpcs", npc);
                }
            }
        }
    }

    private void DrawVendorSummary(UnityEngine.Object vendor)
    {
        SerializedObject serializedObject = new SerializedObject(vendor);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Vendor", EditorStyles.boldLabel);
            DrawReadOnlyProperty(serializedObject, "buysPlayerItems", "Buys Player Items");
            DrawArrayCount(serializedObject, "stockedItems", "Stocked Items");
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

    private void DrawHealthTab()
    {
        healthScroll = EditorGUILayout.BeginScrollView(healthScroll);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Quest / NPC Health", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Health", GUILayout.Width(140f)))
            RefreshHealth();

        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("No Quest/NPC flow issues found.", MessageType.Info);
        }
        else
        {
            for (int index = 0; index < issues.Count; index++)
                DrawIssue(issues[index]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawIssue(QuestNpcFlowIssue issue)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox($"{issue.Title}\n{issue.Details}", issue.Severity);
            if (issue.Context == null)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(issue.Context, typeof(UnityEngine.Object), false);
                if (GUILayout.Button("Select", GUILayout.Width(64f)))
                    Selection.activeObject = issue.Context;
                if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    EditorGUIUtility.PingObject(issue.Context);
            }
        }
    }

    private void DrawReadOnlyProperty(SerializedObject serializedObject, string propertyPath, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
            return;

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    private void DrawArrayCount(SerializedObject serializedObject, string propertyPath, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null || !property.isArray)
            return;

        EditorGUILayout.LabelField(label, property.arraySize.ToString());
    }

    private void DrawObjectList(string title, List<UnityEngine.Object> objects)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"{title} ({objects.Count})", EditorStyles.boldLabel);
            if (objects.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                return;
            }

            for (int index = 0; index < objects.Count; index++)
            {
                UnityEngine.Object item = objects[index];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(item, typeof(UnityEngine.Object), false);
                    if (GUILayout.Button("Open", GUILayout.Width(56f)))
                    {
                        if (item != null)
                            AssetDatabase.OpenAsset(item);
                    }
                }
            }
        }
    }

    private void RefreshAll()
    {
        quests = LoadAssets("NpcQuestDefinition", new[] { "Assets/Resources/GameData/NPC", "Assets/Resources/GameData" });
        npcs = LoadAssets("NpcDefinition", new[] { "Assets/Resources/GameData/NPC", "Assets/Resources/GameData" });
        vendors = LoadAssets("NpcVendorDefinition", new[] { "Assets/Resources/GameData/Vendors", "Assets/Resources/GameData" });
        RefreshHealth();
    }

    private void RefreshHealth()
    {
        issues = AnalyzeHealth();
    }

    private List<QuestNpcFlowIssue> AnalyzeHealth()
    {
        List<QuestNpcFlowIssue> result = new List<QuestNpcFlowIssue>();
        HashSet<string> questIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> npcIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < quests.Count; index++)
        {
            UnityEngine.Object quest = quests[index].Asset;
            SerializedObject serializedObject = new SerializedObject(quest);
            string questId = GetString(serializedObject, "questId");
            string title = GetString(serializedObject, "questTitle");

            if (string.IsNullOrWhiteSpace(questId))
                AddIssue(result, MessageType.Warning, "Quest missing ID", quest.name, quest);
            else if (!questIds.Add(questId.Trim()))
                AddIssue(result, MessageType.Warning, "Duplicate quest ID", questId, quest);

            if (string.IsNullOrWhiteSpace(title))
                AddIssue(result, MessageType.Info, "Quest missing title", quest.name, quest);

            if (GetArraySize(serializedObject, "objectives") == 0 && !GetBool(serializedObject, "isNarrativeOnly"))
                AddIssue(result, MessageType.Info, "Quest has no objectives", quest.name, quest);

            if (GetArraySize(serializedObject, "introPages") == 0)
                AddIssue(result, MessageType.Info, "Quest has no intro dialogue", quest.name, quest);
        }

        for (int index = 0; index < npcs.Count; index++)
        {
            UnityEngine.Object npc = npcs[index].Asset;
            SerializedObject serializedObject = new SerializedObject(npc);
            string npcId = GetString(serializedObject, "npcId");
            if (string.IsNullOrWhiteSpace(npcId))
                AddIssue(result, MessageType.Warning, "NPC missing ID", npc.name, npc);
            else if (!npcIds.Add(npcId.Trim()))
                AddIssue(result, MessageType.Warning, "Duplicate NPC ID", npcId, npc);
        }

        result.Sort((left, right) => SeverityRank(right.Severity).CompareTo(SeverityRank(left.Severity)));
        return result;
    }

    private List<UnityEngine.Object> FindNpcQuestListsContaining(UnityEngine.Object quest)
    {
        List<UnityEngine.Object> result = new List<UnityEngine.Object>();
        for (int index = 0; index < npcs.Count; index++)
        {
            UnityEngine.Object npc = npcs[index].Asset;
            if (ObjectArrayContains(npc, "quests", quest))
                result.Add(npc);
        }

        return result;
    }

    private List<UnityEngine.Object> FindQuestsReferencingNpc(UnityEngine.Object npc, string propertyPath)
    {
        List<UnityEngine.Object> result = new List<UnityEngine.Object>();
        for (int index = 0; index < quests.Count; index++)
        {
            UnityEngine.Object quest = quests[index].Asset;
            if (ObjectArrayContains(quest, propertyPath, npc))
                result.Add(quest);
        }

        return result;
    }

    private static List<UnityEngine.Object> GetObjectArrayReferences(UnityEngine.Object asset, string propertyPath)
    {
        List<UnityEngine.Object> result = new List<UnityEngine.Object>();
        if (asset == null)
            return result;

        SerializedObject serializedObject = new SerializedObject(asset);
        SerializedProperty array = serializedObject.FindProperty(propertyPath);
        if (array == null || !array.isArray)
            return result;

        for (int index = 0; index < array.arraySize; index++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
                result.Add(element.objectReferenceValue);
        }

        return result;
    }

    private static void AddObjectToArray(UnityEngine.Object asset, string propertyPath, UnityEngine.Object value)
    {
        if (asset == null || value == null)
            return;

        if (ObjectArrayContains(asset, propertyPath, value))
            return;

        SerializedObject serializedObject = new SerializedObject(asset);
        SerializedProperty array = serializedObject.FindProperty(propertyPath);
        if (array == null || !array.isArray)
            return;

        Undo.RecordObject(asset, "Link Quest NPC Flow");
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty element = array.GetArrayElementAtIndex(index);
        element.objectReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
    }

    private static bool ObjectArrayContains(UnityEngine.Object asset, string propertyPath, UnityEngine.Object value)
    {
        if (asset == null || value == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(asset);
        SerializedProperty array = serializedObject.FindProperty(propertyPath);
        if (array == null || !array.isArray)
            return false;

        for (int index = 0; index < array.arraySize; index++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue == value)
                return true;
        }

        return false;
    }

    private List<QuestNpcAssetRecord> FilterRecords(List<QuestNpcAssetRecord> source)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return source;

        string normalizedSearch = searchText.Trim();
        List<QuestNpcAssetRecord> filtered = new List<QuestNpcAssetRecord>();
        for (int index = 0; index < source.Count; index++)
        {
            QuestNpcAssetRecord record = source[index];
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

    private static List<QuestNpcAssetRecord> LoadAssets(string typeName, string[] folders)
    {
        List<QuestNpcAssetRecord> records = new List<QuestNpcAssetRecord>();
        Type type = ResolveScriptableObjectType(typeName);
        if (type == null)
            return records;

        string[] validFolders = ResolveValidFolders(folders);
        if (validFolders.Length == 0)
            return records;

        string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", validFolders);
        HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            if (string.IsNullOrWhiteSpace(path) || !seenPaths.Add(path))
                continue;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null || !type.IsInstanceOfType(asset))
                continue;

            records.Add(new QuestNpcAssetRecord { Asset = asset, Path = path });
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

    private static string[] ResolveValidFolders(string[] folders)
    {
        List<string> validFolders = new List<string>();
        for (int index = 0; index < folders.Length; index++)
        {
            if (AssetDatabase.IsValidFolder(folders[index]))
                validFolders.Add(folders[index]);
        }

        return validFolders.ToArray();
    }

    private static string GetString(SerializedObject serializedObject, string propertyPath)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        return property != null && property.propertyType == SerializedPropertyType.String
            ? property.stringValue
            : string.Empty;
    }

    private static bool GetBool(SerializedObject serializedObject, string propertyPath)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        return property != null && property.propertyType == SerializedPropertyType.Boolean && property.boolValue;
    }

    private static int GetArraySize(SerializedObject serializedObject, string propertyPath)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        return property != null && property.isArray ? property.arraySize : 0;
    }

    private static void AddIssue(
        List<QuestNpcFlowIssue> issues,
        MessageType severity,
        string title,
        string details,
        UnityEngine.Object context)
    {
        issues.Add(new QuestNpcFlowIssue
        {
            Severity = severity,
            Title = title,
            Details = details,
            Context = context
        });
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

    private void DestroyCachedEditor()
    {
        if (cachedEditor == null)
            return;

        DestroyImmediate(cachedEditor);
        cachedEditor = null;
        cachedEditorTarget = null;
    }
}
