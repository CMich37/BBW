using UnityEngine;
using UnityEditor;
using System.Linq; // ← add this


public class CenterPrefabChildren : EditorWindow
{
    private GameObject selectedPrefab;

    [MenuItem("Tools/Center Prefab Children")]
    public static void ShowWindow()
    {
        GetWindow<CenterPrefabChildren>("Center Prefab Children");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-Center Children Around Pivot", EditorStyles.boldLabel);

        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", selectedPrefab, typeof(GameObject), true);

        if (selectedPrefab == null || !PrefabUtility.IsPartOfAnyPrefab(selectedPrefab))
        {
            EditorGUILayout.HelpBox("Select a prefab root GameObject.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Center Children"))
        {
            CenterChildren(selectedPrefab);
        }
    }

    private void CenterChildren(GameObject prefabRoot)
    {
        // Get all renderers to find bounds
        Renderer[] renderers = prefabRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found. Skipping.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(prefabRoot, "Center Children");

        // Compute center of bounds
        Bounds bounds = renderers[0].bounds;
        foreach (var rend in renderers.Skip(1))
            bounds.Encapsulate(rend.bounds);

        Vector3 centerOffset = bounds.center - prefabRoot.transform.position;

        // Move all children to offset them to new center
        foreach (Transform child in prefabRoot.transform)
        {
            child.position -= centerOffset;
        }

        Debug.Log($"[CenterPrefabChildren] Recentered {prefabRoot.name} around its bounds center.");
    }
}
