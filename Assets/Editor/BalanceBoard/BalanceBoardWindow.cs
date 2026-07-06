using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal sealed class BalanceBoardColumn
{
    public readonly string Label;
    public readonly string PropertyPath;
    public readonly float Width;

    public BalanceBoardColumn(string label, string propertyPath, float width)
    {
        Label = label;
        PropertyPath = propertyPath;
        Width = width;
    }
}

internal sealed class BalanceBoardGroup
{
    public readonly string Name;
    public readonly string TypeName;
    public readonly string[] SearchFolders;
    public readonly BalanceBoardColumn[] Columns;

    public BalanceBoardGroup(string name, string typeName, string[] searchFolders, BalanceBoardColumn[] columns)
    {
        Name = name;
        TypeName = typeName;
        SearchFolders = searchFolders ?? new[] { "Assets" };
        Columns = columns ?? Array.Empty<BalanceBoardColumn>();
    }
}

internal sealed class BalanceBoardRecord
{
    public UnityEngine.Object Asset;
    public string Path;

    public string DisplayName => Asset != null ? Asset.name : "<Missing Asset>";
}

public class BalanceBoardWindow : EditorWindow
{
    private static readonly BalanceBoardGroup[] Groups =
    {
        new BalanceBoardGroup(
            "Enemies",
            "EnemyDefinition",
            new[] { "Assets/Resources/GameData/Enemies", "Assets/Resources/GameData" },
            new[]
            {
                new BalanceBoardColumn("Type", "enemyType", 92f),
                new BalanceBoardColumn("Role", "role", 98f),
                new BalanceBoardColumn("Element", "elementType", 92f),
                new BalanceBoardColumn("HP", "maxHP", 56f),
                new BalanceBoardColumn("DEF", "defense", 56f),
                new BalanceBoardColumn("Avoid", "avoidance", 56f),
                new BalanceBoardColumn("EXP", "expReward", 60f),
                new BalanceBoardColumn("Gauge", "gaugeReward", 62f),
                new BalanceBoardColumn("Touch", "contactDamage", 62f),
                new BalanceBoardColumn("Attack", "animatedAttackDamage", 66f),
                new BalanceBoardColumn("Hit React", "hitReactionDamageThreshold", 76f),
                new BalanceBoardColumn("Lock", "hitReactionMovementLockDuration", 62f),
                new BalanceBoardColumn("Respawn", "respawnDelay", 72f)
            }),

        new BalanceBoardGroup(
            "Skills",
            "PlayerSkillDefinition",
            new[] { "Assets/Resources/GameData/Jobs", "Assets/Resources/GameData/Skills", "Assets/Resources/GameData" },
            new[]
            {
                new BalanceBoardColumn("ID", "skillId", 135f),
                new BalanceBoardColumn("Name", "displayName", 135f),
                new BalanceBoardColumn("Job", "jobType", 92f),
                new BalanceBoardColumn("Type", "skillType", 80f),
                new BalanceBoardColumn("Exec", "executionKind", 88f),
                new BalanceBoardColumn("Target", "combatTargetingKind", 98f),
                new BalanceBoardColumn("Dmg x", "damageMultiplier", 62f),
                new BalanceBoardColumn("Targets", "maxTargets", 62f),
                new BalanceBoardColumn("Proj", "projectileCount", 54f),
                new BalanceBoardColumn("Break", "baseBreakPower", 62f),
                new BalanceBoardColumn("Element", "defaultElement", 92f),
                new BalanceBoardColumn("Slot", "defaultSlotIndex", 52f),
                new BalanceBoardColumn("Duration", "attackDuration", 68f),
                new BalanceBoardColumn("Anim Spd", "animationSpeed", 68f)
            }),

        new BalanceBoardGroup(
            "Basic Attacks",
            "PlayerBasicAttackProfile",
            new[] { "Assets/Resources/GameData/Jobs", "Assets/Resources/GameData" },
            new[]
            {
                new BalanceBoardColumn("ID", "attackId", 135f),
                new BalanceBoardColumn("Name", "displayName", 135f),
                new BalanceBoardColumn("Job", "jobType", 92f),
                new BalanceBoardColumn("Exec", "executionKind", 88f),
                new BalanceBoardColumn("Target", "targetingKind", 98f),
                new BalanceBoardColumn("Coef", "damageCoefficient", 58f),
                new BalanceBoardColumn("Hits", "hitCount", 50f),
                new BalanceBoardColumn("Break", "baseBreakPower", 62f),
                new BalanceBoardColumn("Gauge?", "buildsGauge", 58f),
                new BalanceBoardColumn("Hit Gauge", "gaugeGainOnValidHit", 72f),
                new BalanceBoardColumn("Finish Gauge", "gaugeGainOnChainFinisher", 82f),
                new BalanceBoardColumn("Stacks", "maxFlowStacks", 58f),
                new BalanceBoardColumn("Cooldown", "attackCooldown", 76f)
            }),

        new BalanceBoardGroup(
            "Projectiles",
            "ProjectileProfile",
            new[] { "Assets/Resources/GameData/Jobs", "Assets/Resources/GameData/Combat", "Assets/Resources/GameData" },
            new[]
            {
                new BalanceBoardColumn("ID", "projectileId", 135f),
                new BalanceBoardColumn("Name", "displayName", 135f),
                new BalanceBoardColumn("Prefab", "projectilePrefab", 150f),
                new BalanceBoardColumn("Travel", "travelStyle", 86f),
                new BalanceBoardColumn("Hit", "hitMode", 86f),
                new BalanceBoardColumn("Speed", "speed", 62f),
                new BalanceBoardColumn("Range", "maxRange", 62f),
                new BalanceBoardColumn("Life", "lifetime", 58f),
                new BalanceBoardColumn("Targets", "maxTargets", 62f),
                new BalanceBoardColumn("Spawn F", "spawnForwardOffset", 68f),
                new BalanceBoardColumn("Spawn U", "spawnUpOffset", 68f),
                new BalanceBoardColumn("Scale", "visualScale", 58f),
                new BalanceBoardColumn("Rotation", "visualRotationMode", 150f)
            }),

        new BalanceBoardGroup(
            "Burst Profiles",
            "BurstLinkedSkillProfile",
            new[] { "Assets/Resources/GameData/Jobs", "Assets/Resources/GameData/Skills", "Assets/Resources/GameData" },
            new[]
            {
                new BalanceBoardColumn("Min Gauge", "minimumGaugeToStart", 78f),
                new BalanceBoardColumn("Partial?", "allowPartialGaugeSpend", 64f),
                new BalanceBoardColumn("Gauge Scale", "gaugeScalingProfile", 145f),
                new BalanceBoardColumn("Tap", "tapReleaseThreshold", 62f),
                new BalanceBoardColumn("Hold", "maxHoldDuration", 62f),
                new BalanceBoardColumn("Auto?", "autoReleaseAtMaxHold", 58f),
                new BalanceBoardColumn("Aerial?", "allowAerialBurst", 58f),
                new BalanceBoardColumn("Aerial Hold", "aerialMaxHoldDurationMultiplier", 78f),
                new BalanceBoardColumn("Aerial Gauge", "aerialGaugeSpendMultiplier", 86f),
                new BalanceBoardColumn("Aerial Dmg", "aerialDamageMultiplier", 78f),
                new BalanceBoardColumn("Interrupt?", "interruptedByValidHit", 68f)
            })
    };

    private int groupIndex;
    private string searchText = string.Empty;
    private Vector2 tableScroll;
    private List<BalanceBoardRecord> records = new List<BalanceBoardRecord>();

    [MenuItem("Tools/Combat/Balance Board")]
    public static void Open()
    {
        GetWindow<BalanceBoardWindow>("Balance Board");
    }

    private void OnEnable()
    {
        RefreshRecords();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawHeader();
        DrawTable();
    }

    private BalanceBoardGroup CurrentGroup
    {
        get
        {
            if (groupIndex < 0 || groupIndex >= Groups.Length)
                groupIndex = 0;

            return Groups[groupIndex];
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            string[] groupNames = GetGroupNames();
            int newGroupIndex = GUILayout.Toolbar(groupIndex, groupNames, EditorStyles.toolbarButton);
            if (newGroupIndex != groupIndex)
            {
                groupIndex = newGroupIndex;
                searchText = string.Empty;
                RefreshRecords();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                RefreshRecords();

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                AssetDatabase.SaveAssets();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(CurrentGroup.Name, EditorStyles.boldLabel, GUILayout.Width(180f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{FilteredRecords.Count} / {records.Count}", EditorStyles.miniLabel, GUILayout.Width(80f));
        }

        EditorGUI.BeginChangeCheck();
        searchText = EditorGUILayout.TextField("Search", searchText);
        if (EditorGUI.EndChangeCheck())
            Repaint();

        EditorGUILayout.HelpBox(
            "Fast tuning table. Edits are written to the selected ScriptableObjects through SerializedObject and can be undone by Unity where supported.",
            MessageType.Info);
    }

    private void DrawTable()
    {
        List<BalanceBoardRecord> filteredRecords = FilteredRecords;
        tableScroll = EditorGUILayout.BeginScrollView(tableScroll);

        DrawTableHeader();
        for (int index = 0; index < filteredRecords.Count; index++)
            DrawRecordRow(filteredRecords[index], index);

        EditorGUILayout.EndScrollView();
    }

    private void DrawTableHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Asset", EditorStyles.miniBoldLabel, GUILayout.Width(210f));
            GUILayout.Label("Actions", EditorStyles.miniBoldLabel, GUILayout.Width(124f));

            BalanceBoardColumn[] columns = CurrentGroup.Columns;
            for (int index = 0; index < columns.Length; index++)
                GUILayout.Label(columns[index].Label, EditorStyles.miniBoldLabel, GUILayout.Width(columns[index].Width));
        }
    }

    private void DrawRecordRow(BalanceBoardRecord record, int rowIndex)
    {
        if (record == null || record.Asset == null)
            return;

        GUIStyle rowStyle = rowIndex % 2 == 0 ? EditorStyles.helpBox : GUIStyle.none;
        SerializedObject serializedObject = new SerializedObject(record.Asset);
        serializedObject.Update();

        using (new EditorGUILayout.HorizontalScope(rowStyle))
        {
            DrawAssetCell(record);
            DrawActionCell(record);

            BalanceBoardColumn[] columns = CurrentGroup.Columns;
            for (int index = 0; index < columns.Length; index++)
                DrawPropertyCell(serializedObject, columns[index]);
        }

        if (serializedObject.ApplyModifiedProperties())
            EditorUtility.SetDirty(record.Asset);
    }

    private void DrawAssetCell(BalanceBoardRecord record)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(210f)))
        {
            EditorGUILayout.ObjectField(record.Asset, typeof(UnityEngine.Object), false);
            EditorGUILayout.LabelField(record.Path, EditorStyles.miniLabel);
        }
    }

    private void DrawActionCell(BalanceBoardRecord record)
    {
        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(124f)))
        {
            if (GUILayout.Button("Ping", GUILayout.Width(42f)))
                EditorGUIUtility.PingObject(record.Asset);

            if (GUILayout.Button("Sel", GUILayout.Width(36f)))
                Selection.activeObject = record.Asset;

            if (GUILayout.Button("Open", GUILayout.Width(46f)))
                AssetDatabase.OpenAsset(record.Asset);
        }
    }

    private void DrawPropertyCell(SerializedObject serializedObject, BalanceBoardColumn column)
    {
        SerializedProperty property = serializedObject.FindProperty(column.PropertyPath);
        if (property == null)
        {
            EditorGUILayout.LabelField("N/A", EditorStyles.miniLabel, GUILayout.Width(column.Width));
            return;
        }

        EditorGUILayout.PropertyField(property, GUIContent.none, true, GUILayout.Width(column.Width));
    }

    private void RefreshRecords()
    {
        records = LoadRecords(CurrentGroup);
    }

    private static List<BalanceBoardRecord> LoadRecords(BalanceBoardGroup group)
    {
        List<BalanceBoardRecord> loadedRecords = new List<BalanceBoardRecord>();
        Type type = ResolveScriptableObjectType(group.TypeName);
        if (type == null)
            return loadedRecords;

        string[] folders = ResolveValidFolders(group.SearchFolders);
        if (folders.Length == 0)
            return loadedRecords;

        string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", folders);
        HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            if (string.IsNullOrWhiteSpace(path) || !seenPaths.Add(path))
                continue;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null || !type.IsInstanceOfType(asset))
                continue;

            loadedRecords.Add(new BalanceBoardRecord
            {
                Asset = asset,
                Path = path
            });
        }

        loadedRecords.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
        return loadedRecords;
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

    private static string[] ResolveValidFolders(string[] folders)
    {
        List<string> validFolders = new List<string>();
        if (folders == null || folders.Length == 0)
            folders = new[] { "Assets" };

        for (int index = 0; index < folders.Length; index++)
        {
            string folder = folders[index];
            if (!string.IsNullOrWhiteSpace(folder) && AssetDatabase.IsValidFolder(folder))
                validFolders.Add(folder);
        }

        return validFolders.ToArray();
    }

    private string[] GetGroupNames()
    {
        string[] names = new string[Groups.Length];
        for (int index = 0; index < Groups.Length; index++)
            names[index] = Groups[index].Name;

        return names;
    }

    private List<BalanceBoardRecord> FilteredRecords
    {
        get
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return records;

            string normalizedSearch = searchText.Trim();
            List<BalanceBoardRecord> filtered = new List<BalanceBoardRecord>();
            for (int index = 0; index < records.Count; index++)
            {
                BalanceBoardRecord record = records[index];
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
    }
}
