using UnityEngine;
[ExecuteInEditMode]
public class RoomSizeDebugger : MonoBehaviour
{
    public GameObject[] roomPrefabs;

    void Start()
    {
        foreach (var prefab in roomPrefabs)
        {
            var bounds = GetBounds(prefab);
            Debug.Log($"{prefab.name} => Size: {bounds.size}");
        }
    }

    Bounds GetBounds(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(prefab.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
