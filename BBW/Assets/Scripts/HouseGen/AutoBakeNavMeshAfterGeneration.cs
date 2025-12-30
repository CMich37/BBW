using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AutoBakeNavMeshAfterGeneration : MonoBehaviour
{
    [Header("What to bake")]
    public Transform generatedRoot;              // parent that contains generated rooms (optional)
    public LayerMask includedLayers = ~0;        // set to Walkable layer(s)
    public bool useColliders = true;             // recommended
    public bool useRenderers = false;            // if you have no colliders

    [Header("Bake volume")]
    public float paddingXZ = 2f;
    public float height = 6f;                    // vertical bake thickness

    [Header("When to bake")]
    public float checkEverySeconds = 0.1f;
    public int stableChecksRequired = 6;
    public float maxWaitSeconds = 15f;
    public int agentTypeId = 0;                  // 0 is usually “Humanoid”

    NavMeshData _navMeshData;
    NavMeshDataInstance _instance;

    IEnumerator Start()
    {
        if (generatedRoot == null) generatedRoot = transform;

        // Wait for generation to “settle”
        yield return WaitForStableHierarchy();

        // Compute bounds
        Bounds bounds = ComputeBounds(generatedRoot);
        if (bounds.size == Vector3.zero)
        {
            Debug.LogWarning("[RuntimeNavMeshBaker] No bounds found. Nothing to bake.");
            yield break;
        }

        bounds.Expand(new Vector3(paddingXZ * 2f, 0f, paddingXZ * 2f));
        bounds.size = new Vector3(bounds.size.x, height, bounds.size.z);

        // Collect build sources
        var sources = new List<NavMeshBuildSource>();
        var markups = new List<NavMeshBuildMarkup>(); // unused, but required by CollectSources signature in some versions

        var defaultArea = 0; // 0 = Walkable
        NavMeshBuilder.CollectSources(
            bounds,
            includedLayers,
            NavMeshCollectGeometry.RenderMeshes, // works for both colliders/renderers; colliders are preferred
            defaultArea,
            markups,
            sources
        );

        if (sources.Count == 0)
        {
            Debug.LogWarning("[RuntimeNavMeshBaker] No sources collected. Check layers/colliders/renderers.");
            yield break;
        }

        // Build settings
        var settings = NavMesh.GetSettingsByID(agentTypeId);
        if (settings.agentTypeID == -1)
        {
            Debug.LogWarning("[RuntimeNavMeshBaker] Invalid agentTypeId. Using default settings.");
            settings = NavMesh.GetSettingsByIndex(0);
        }

        // Create / replace NavMeshData
        if (_instance.valid) _instance.Remove();

        _navMeshData = NavMeshBuilder.BuildNavMeshData(
            settings,
            sources,
            bounds,
            Vector3.zero,
            Quaternion.identity
        );

        if (_navMeshData == null)
        {
            Debug.LogError("[RuntimeNavMeshBaker] BuildNavMeshData returned null.");
            yield break;
        }

        _instance = NavMesh.AddNavMeshData(_navMeshData);
        Debug.Log($"[RuntimeNavMeshBaker] NavMesh baked. Sources: {sources.Count}");
    }

    IEnumerator WaitForStableHierarchy()
    {
        float start = Time.time;

        int stable = 0;
        int lastChildCount = -1;
        Bounds lastBounds = new Bounds(Vector3.zero, Vector3.zero);

        while (Time.time - start < maxWaitSeconds)
        {
            yield return new WaitForSeconds(checkEverySeconds);

            int childCount = generatedRoot.childCount;
            Bounds b = ComputeBounds(generatedRoot);

            bool sameChildCount = (childCount == lastChildCount);
            bool sameBounds = ApproximatelyEqual(b, lastBounds);

            if (sameChildCount && sameBounds) stable++;
            else stable = 0;

            lastChildCount = childCount;
            lastBounds = b;

            if (stable >= stableChecksRequired)
                yield break;
        }

        // If we timeout, we still try to bake with whatever exists.
        Debug.LogWarning("[RuntimeNavMeshBaker] Timed out waiting for stability. Baking anyway.");
    }

    Bounds ComputeBounds(Transform root)
    {
        bool hasAny = false;
        Bounds b = new Bounds();

        if (useColliders)
        {
            var cols = root.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                // optional: respect includedLayers here too
                if (((1 << c.gameObject.layer) & includedLayers.value) == 0) continue;

                if (!hasAny) { b = c.bounds; hasAny = true; }
                else b.Encapsulate(c.bounds);
            }
        }

        if (!hasAny && useRenderers)
        {
            var rends = root.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                if (((1 << r.gameObject.layer) & includedLayers.value) == 0) continue;

                if (!hasAny) { b = r.bounds; hasAny = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        return hasAny ? b : new Bounds(Vector3.zero, Vector3.zero);
    }

    bool ApproximatelyEqual(Bounds a, Bounds b)
    {
        const float eps = 0.01f;
        return (a.center - b.center).sqrMagnitude < eps * eps &&
               (a.size - b.size).sqrMagnitude < eps * eps;
    }

}
