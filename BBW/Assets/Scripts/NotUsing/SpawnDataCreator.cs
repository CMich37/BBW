using UnityEditor;
using UnityEngine;

public class SpawnDataCreator
{
    [MenuItem("Assets/Create/Spawn System/Create Sample SpawnData")]
    public static void CreateSampleSpawnData()
    {
        SpawnData data = ScriptableObject.CreateInstance<SpawnData>();

        GameObject apple = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Apple.prefab");
        GameObject bacon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bacon.prefab");
        GameObject banana = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Banana.prefab");

        data.itemPrefabs.Add(apple);
        data.weights.Add(2f);

        data.itemPrefabs.Add(bacon);
        data.weights.Add(1f);

        data.itemPrefabs.Add(banana);
        data.weights.Add(3f);

        data.localSpawnOffsets.Add(new Vector3(0, 0.5f, 0));
        data.localSpawnOffsets.Add(new Vector3(-0.3f, 0.5f, 0.2f));
        data.localSpawnOffsets.Add(new Vector3(0.3f, 0.5f, -0.2f));

        AssetDatabase.CreateAsset(data, "Assets/SpawnData/SO_SampleSpawnData.asset");
        EditorUtility.SetDirty(data); // ← This ensures Unity saves the field values
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = data;
    }
}

