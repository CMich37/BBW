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
            var colBounds = GetColBounds(prefab);
            Debug.Log($"{prefab.name} => Size: {colBounds.size}");
        }
    }

    Bounds GetBounds(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.Log("No colliders");
            return new Bounds(prefab.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    Bounds GetColBounds(GameObject prefab)
    {
        Collider[] colliders = prefab.GetComponentsInChildren<Collider>();

        if (colliders.Length == 0)
        {
            Debug.Log("No colliders");
            return new Bounds(prefab.transform.position, Vector3.zero);
        }

        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            bounds.Encapsulate(colliders[i].bounds);
        }
        return bounds;
    }

    
}
