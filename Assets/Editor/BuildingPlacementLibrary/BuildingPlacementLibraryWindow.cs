using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class BuildingPieceRecord
{
    public GameObject Prefab;
    public string Path;
    public string DisplayName => Prefab != null ? Prefab.name : "<Missing Prefab>";
}

public class BuildingPlacementLibraryWindow : EditorWindow
{
    private string prefabFolder = "Assets/Prefabs";
    private string searchText = string.Empty;
    private List<BuildingPieceRecord> pieces = new List<BuildingPieceRecord>();
    private GameObject selectedPrefab;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private string parentName = "Base Layout";
    private bool useParent = true;
    private float gridSize = 1f;
    private float spacing = 1f;
    private int lineCount = 5;
    private int gridWidth = 3;
    private int gridDepth = 3;
    private int circleCount = 8;
    private float circleRadius = 4f;
    private float yaw;
    private bool snapToGrid = true;
    private bool alignToGround = true;
    private LayerMask groundMask = Physics.DefaultRaycastLayers;
    private Vector3 positionOffset;
    private Vector3 rotationOffset;
    private Vector3 scale = Vector3.one;

    [MenuItem("Tools/Building/Base Placement Library")]
    public static void Open()
    {
        GetWindow<BuildingPlacementLibraryWindow>("Base Placement");
    }

    private void OnEnable()
    {
        RefreshPieces();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPieceList();
            DrawDetails();
        }
    }

    private void DrawPieceList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(330f)))
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Build Pieces", EditorStyles.boldLabel);
            DrawFolderPicker();

            EditorGUI.BeginChangeCheck();
            searchText = EditorGUILayout.TextField("Search", searchText);
            if (EditorGUI.EndChangeCheck())
                RefreshPieces();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{pieces.Count} prefabs", EditorStyles.miniLabel);
                if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                    RefreshPieces();
            }

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            for (int index = 0; index < pieces.Count; index++)
                DrawPieceRow(pieces[index]);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawFolderPicker()
    {
        DefaultAsset currentFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(prefabFolder);
        EditorGUI.BeginChangeCheck();
        DefaultAsset selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", currentFolder, typeof(DefaultAsset), false);
        if (!EditorGUI.EndChangeCheck() || selectedFolder == null)
            return;

        string path = AssetDatabase.GetAssetPath(selectedFolder);
        if (!AssetDatabase.IsValidFolder(path))
            return;

        prefabFolder = path;
        RefreshPieces();
    }

    private void DrawPieceRow(BuildingPieceRecord record)
    {
        if (record == null || record.Prefab == null)
            return;

        bool selected = selectedPrefab == record.Prefab;
        using (new EditorGUILayout.HorizontalScope(selected ? EditorStyles.helpBox : GUIStyle.none))
        {
            Texture icon = AssetPreview.GetMiniThumbnail(record.Prefab);
            GUILayout.Label(icon, GUILayout.Width(24f), GUILayout.Height(24f));

            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button(record.DisplayName, EditorStyles.label))
                    selectedPrefab = record.Prefab;

                EditorGUILayout.LabelField(record.Path, EditorStyles.miniLabel);
            }
        }
    }

    private void DrawDetails()
    {
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);

        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Selected Piece", selectedPrefab, typeof(GameObject), false);
        DrawSelectedPreview();
        DrawPlacementSettings();
        DrawPlacementActions();
        DrawPrefabDiagnostics();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectedPreview()
    {
        if (selectedPrefab == null)
        {
            EditorGUILayout.HelpBox("Select a prefab from the library to place it.", MessageType.Info);
            return;
        }

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
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);
            useParent = EditorGUILayout.Toggle("Use Parent Container", useParent);
            using (new EditorGUI.DisabledScope(!useParent))
                parentName = EditorGUILayout.TextField("Parent Name", parentName);

            snapToGrid = EditorGUILayout.Toggle("Snap To Grid", snapToGrid);
            using (new EditorGUI.DisabledScope(!snapToGrid))
                gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", gridSize));

            alignToGround = EditorGUILayout.Toggle("Align To Ground", alignToGround);
            using (new EditorGUI.DisabledScope(!alignToGround))
                groundMask = DrawLayerMask("Ground Mask", groundMask);

            spacing = Mathf.Max(0.01f, EditorGUILayout.FloatField("Spacing", spacing));
            yaw = EditorGUILayout.FloatField("Yaw", yaw);
            positionOffset = EditorGUILayout.Vector3Field("Position Offset", positionOffset);
            rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);
            scale = EditorGUILayout.Vector3Field("Scale", scale);
        }
    }

    private void DrawPlacementActions()
    {
        using (new EditorGUI.DisabledScope(selectedPrefab == null))
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Place Single", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("At Scene Pivot"))
                        PlaceAt(GetScenePivot());

                    if (GUILayout.Button("At Selection"))
                        PlaceAt(GetSelectedPivot());

                    if (GUILayout.Button("At Origin"))
                        PlaceAt(Vector3.zero);
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Pattern Placement", EditorStyles.boldLabel);
                lineCount = Mathf.Max(1, EditorGUILayout.IntField("Line Count", lineCount));
                if (GUILayout.Button("Place Line X"))
                    PlaceLine(Vector3.right);
                if (GUILayout.Button("Place Line Z"))
                    PlaceLine(Vector3.forward);

                gridWidth = Mathf.Max(1, EditorGUILayout.IntField("Grid Width", gridWidth));
                gridDepth = Mathf.Max(1, EditorGUILayout.IntField("Grid Depth", gridDepth));
                if (GUILayout.Button("Place Grid"))
                    PlaceGrid();

                circleCount = Mathf.Max(1, EditorGUILayout.IntField("Circle Count", circleCount));
                circleRadius = Mathf.Max(0.01f, EditorGUILayout.FloatField("Circle Radius", circleRadius));
                if (GUILayout.Button("Place Circle"))
                    PlaceCircle();
            }
        }
    }

    private void DrawPrefabDiagnostics()
    {
        if (selectedPrefab == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Prefab Diagnostics", EditorStyles.boldLabel);
            int rendererCount = selectedPrefab.GetComponentsInChildren<Renderer>(true).Length;
            int colliderCount = selectedPrefab.GetComponentsInChildren<Collider>(true).Length;
            int meshFilterCount = selectedPrefab.GetComponentsInChildren<MeshFilter>(true).Length;
            DrawMetric("Renderers", rendererCount.ToString());
            DrawMetric("Colliders", colliderCount.ToString());
            DrawMetric("Mesh Filters", meshFilterCount.ToString());

            if (rendererCount == 0)
                EditorGUILayout.HelpBox("This prefab has no renderers.", MessageType.Warning);
            if (colliderCount == 0)
                EditorGUILayout.HelpBox("This prefab has no colliders. That can be fine for decoration, but not for blocking/building pieces.", MessageType.Info);
        }
    }

    private void PlaceLine(Vector3 axis)
    {
        Vector3 origin = GetSelectedPivot();
        for (int index = 0; index < lineCount; index++)
            PlaceAt(origin + axis * spacing * index);
    }

    private void PlaceGrid()
    {
        Vector3 origin = GetSelectedPivot();
        Vector3 centerOffset = new Vector3((gridWidth - 1) * spacing * 0.5f, 0f, (gridDepth - 1) * spacing * 0.5f);
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 offset = new Vector3(x * spacing, 0f, z * spacing) - centerOffset;
                PlaceAt(origin + offset);
            }
        }
    }

    private void PlaceCircle()
    {
        Vector3 origin = GetSelectedPivot();
        for (int index = 0; index < circleCount; index++)
        {
            float angle = index / (float)circleCount * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * circleRadius, 0f, Mathf.Sin(angle) * circleRadius);
            PlaceAt(origin + offset, Mathf.Rad2Deg * -angle + 90f);
        }
    }

    private GameObject PlaceAt(Vector3 position, float extraYaw = 0f)
    {
        if (selectedPrefab == null)
            return null;

        Vector3 resolvedPosition = ResolvePosition(position);
        Quaternion rotation = Quaternion.Euler(rotationOffset.x, yaw + extraYaw + rotationOffset.y, rotationOffset.z);
        GameObject instance = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Place Building Piece");
        instance.transform.SetPositionAndRotation(resolvedPosition, rotation);
        instance.transform.localScale = ResolveScale(scale);

        if (useParent)
        {
            Transform parent = FindOrCreateParent();
            if (parent != null)
                Undo.SetTransformParent(instance.transform, parent, "Parent Building Piece");
        }

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return instance;
    }

    private Vector3 ResolvePosition(Vector3 position)
    {
        Vector3 resolved = position + positionOffset;
        if (alignToGround && TryProjectToGround(resolved, out Vector3 groundPosition))
            resolved = groundPosition + new Vector3(0f, positionOffset.y, 0f);

        if (snapToGrid)
            resolved = Snap(resolved, Mathf.Max(0.01f, gridSize));

        return resolved;
    }

    private bool TryProjectToGround(Vector3 position, out Vector3 groundPosition)
    {
        Vector3 origin = position + Vector3.up * 150f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 400f, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;
            return true;
        }

        groundPosition = position;
        return false;
    }

    private Transform FindOrCreateParent()
    {
        string resolvedName = string.IsNullOrWhiteSpace(parentName) ? "Base Layout" : parentName.Trim();
        GameObject existing = GameObject.Find(resolvedName);
        if (existing != null)
            return existing.transform;

        GameObject parent = new GameObject(resolvedName);
        Undo.RegisterCreatedObjectUndo(parent, "Create Base Layout Parent");
        return parent.transform;
    }

    private Vector3 GetScenePivot()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        return sceneView != null ? sceneView.pivot : Vector3.zero;
    }

    private Vector3 GetSelectedPivot()
    {
        return Selection.activeTransform != null ? Selection.activeTransform.position : GetScenePivot();
    }

    private void RefreshPieces()
    {
        pieces = new List<BuildingPieceRecord>();
        string[] folders = AssetDatabase.IsValidFolder(prefabFolder) ? new[] { prefabFolder } : new[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", folders);
        string normalizedSearch = string.IsNullOrWhiteSpace(searchText) ? string.Empty : searchText.Trim();

        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            if (!string.IsNullOrWhiteSpace(normalizedSearch)
                && prefab.name.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0
                && path.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            pieces.Add(new BuildingPieceRecord { Prefab = prefab, Path = path });
        }

        pieces.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
    }

    private static Vector3 Snap(Vector3 value, float size)
    {
        return new Vector3(
            Mathf.Round(value.x / size) * size,
            Mathf.Round(value.y / size) * size,
            Mathf.Round(value.z / size) * size);
    }

    private static Vector3 ResolveScale(Vector3 value)
    {
        return new Vector3(
            Mathf.Approximately(value.x, 0f) ? 1f : value.x,
            Mathf.Approximately(value.y, 0f) ? 1f : value.y,
            Mathf.Approximately(value.z, 0f) ? 1f : value.z);
    }

    private static void DrawMetric(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, GUILayout.Width(120f));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
        }
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
