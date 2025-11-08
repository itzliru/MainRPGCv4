using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using VaultSystems.Data;

[CustomEditor(typeof(MapUIManager))]
public class MapUIManagerEditor : Editor
{
    private MapUIManager mapUI;
    private bool showMapSettings = true;
    private bool showXORSettings = true;
    private bool showMarkerManagement = true;
    private bool showDebugTools = false;
    private Vector2 scrollPos;

    private void OnEnable()
    {
        mapUI = (MapUIManager)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Map UI Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Map Settings Section
        showMapSettings = EditorGUILayout.Foldout(showMapSettings, "Map Settings", true);
        if (showMapSettings)
        {
            EditorGUI.indentLevel++;
            DrawMapSettings();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // XOR Settings Section
        showXORSettings = EditorGUILayout.Foldout(showXORSettings, "XOR Positioning", true);
        if (showXORSettings)
        {
            EditorGUI.indentLevel++;
            DrawXORSettings();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Marker Management Section
        showMarkerManagement = EditorGUILayout.Foldout(showMarkerManagement, "Marker Management", true);
        if (showMarkerManagement)
        {
            EditorGUI.indentLevel++;
            DrawMarkerManagement();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Debug Tools Section
        showDebugTools = EditorGUILayout.Foldout(showDebugTools, "Debug Tools", true);
        if (showDebugTools)
        {
            EditorGUI.indentLevel++;
            DrawDebugTools();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMapSettings()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapContainer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("markerPrefab"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scaling & Positioning", EditorStyles.miniBoldLabel);

        SerializedProperty scaleProp = serializedObject.FindProperty("mapScale");
        EditorGUILayout.PropertyField(scaleProp);

        SerializedProperty offsetProp = serializedObject.FindProperty("mapCenterOffset");
        EditorGUILayout.PropertyField(offsetProp);

        // Scale preview
        if (scaleProp.floatValue > 0)
        {
            EditorGUILayout.HelpBox($"1 world unit = {1f / scaleProp.floatValue:F2} UI units", MessageType.Info);
        }
    }

    private void DrawXORSettings()
    {
        SerializedProperty useXORProp = serializedObject.FindProperty("useXORPositioning");
        EditorGUILayout.PropertyField(useXORProp);

        if (useXORProp.boolValue)
        {
            SerializedProperty xorKeyProp = serializedObject.FindProperty("xorKey");
            EditorGUILayout.PropertyField(xorKeyProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("XOR Key Tools", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Random Key"))
            {
                GenerateRandomXORKey();
            }
            if (GUILayout.Button("Reset Key"))
            {
                xorKeyProp.vector3Value = Vector3.zero;
            }
            EditorGUILayout.EndHorizontal();

            // XOR preview
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("XOR Preview", EditorStyles.miniBoldLabel);
            Vector3 testPos = EditorGUILayout.Vector3Field("Test Position", Vector3.zero);
            Vector3 encrypted = ApplyXORPreview(testPos, xorKeyProp.vector3Value);
            EditorGUILayout.Vector3Field("Encrypted", encrypted);
            Vector3 decrypted = ApplyXORPreview(encrypted, xorKeyProp.vector3Value);
            EditorGUILayout.Vector3Field("Decrypted", decrypted);
        }
    }

    private void DrawMarkerManagement()
    {
        // Marker count
        int markerCount = MarkerSystem.GetAllMarkers().Count();
        EditorGUILayout.LabelField($"Active Markers: {markerCount}", EditorStyles.miniBoldLabel);

        // Refresh button
        if (GUILayout.Button("Refresh Map Markers"))
        {
            mapUI.RefreshMapMarkers();
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.Space();

        // Marker list
        EditorGUILayout.LabelField("Registered Markers", EditorStyles.miniBoldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));

        foreach (var marker in MarkerSystem.GetAllMarkers())
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(marker.displayName, GUILayout.Width(100));
            EditorGUILayout.LabelField(marker.cellId ?? "No Cell", GUILayout.Width(80));

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeGameObject = marker.gameObject;
            }

            if (GUILayout.Button(marker.isVisible ? "Hide" : "Show", GUILayout.Width(40)))
            {
                marker.ToggleVisibility();
                EditorUtility.SetDirty(marker);
            }

            if (GUILayout.Button("XOR", GUILayout.Width(35)))
            {
                marker.UpdatePositionXOR(Vector3.one);
                EditorUtility.SetDirty(marker);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Bulk operations
        EditorGUILayout.LabelField("Bulk Operations", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Show All"))
        {
            foreach (var marker in MarkerSystem.GetAllMarkers())
            {
                marker.SetVisible(true);
                EditorUtility.SetDirty(marker);
            }
        }

        if (GUILayout.Button("Hide All"))
        {
            foreach (var marker in MarkerSystem.GetAllMarkers())
            {
                marker.SetVisible(false);
                EditorUtility.SetDirty(marker);
            }
        }

        if (GUILayout.Button("Toggle All"))
        {
            foreach (var marker in MarkerSystem.GetAllMarkers())
            {
                marker.ToggleVisibility();
                EditorUtility.SetDirty(marker);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawDebugTools()
    {
        EditorGUILayout.LabelField("Testing & Validation", EditorStyles.miniBoldLabel);

        if (GUILayout.Button("Validate Setup"))
        {
            ValidateMapUISetup();
        }

        if (GUILayout.Button("Test Cell Toggle"))
        {
            string testCell = EditorGUILayout.TextField("Cell ID", "Cell_01");
            mapUI.ToggleCellMarkers(testCell);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Performance Info", EditorStyles.miniBoldLabel);
        int markerCount = MarkerSystem.GetAllMarkers().Count();
        EditorGUILayout.LabelField($"Markers: {markerCount}");
        EditorGUILayout.LabelField($"UI Elements: {mapUI.GetMarkerUIElementsCount()}");

        if (markerCount > 50)
        {
            EditorGUILayout.HelpBox("High marker count may impact performance. Consider marker pooling.", MessageType.Warning);
        }
    }

    private void GenerateRandomXORKey()
    {
        SerializedProperty xorKeyProp = serializedObject.FindProperty("xorKey");
        xorKeyProp.vector3Value = new Vector3(
            Random.Range(10000f, 99999f),
            Random.Range(10000f, 99999f),
            Random.Range(10000f, 99999f)
        );
    }

    private Vector3 ApplyXORPreview(Vector3 position, Vector3 key)
{
    return new Vector3(
        Mathf.RoundToInt(position.x) ^ Mathf.RoundToInt(key.x),
        Mathf.RoundToInt(position.y) ^ Mathf.RoundToInt(key.y),
        Mathf.RoundToInt(position.z) ^ Mathf.RoundToInt(key.z)
    );
}

    private void ValidateMapUISetup()
    {
        string errors = "";
        string warnings = "";

        if (mapUI.mapContainer == null)
            errors += "• Map Container not assigned\n";

        if (mapUI.markerPrefab == null)
            errors += "• Marker Prefab not assigned\n";

        if (mapUI.mapScale <= 0)
            errors += "• Map Scale must be positive\n";

        if (MarkerSystem.GetAllMarkers().Count() == 0)
            warnings += "• No markers registered with MarkerSystem\n";

        if (!string.IsNullOrEmpty(errors))
        {
            EditorUtility.DisplayDialog("Validation Errors", errors, "OK");
        }
        else if (!string.IsNullOrEmpty(warnings))
        {
            EditorUtility.DisplayDialog("Validation Warnings", warnings, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Validation", "Map UI setup looks good!", "OK");
        }
    }
}

// Extension method to get UI elements count
public static class MapUIManagerExtensions
{
    public static int GetMarkerUIElementsCount(this MapUIManager mapUI)
    {
        return mapUI.markerUIElements.Count;
    }
}
