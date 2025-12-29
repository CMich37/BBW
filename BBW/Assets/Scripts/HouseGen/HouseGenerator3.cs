using System;
using System.Collections.Generic;
using UnityEngine;

public class HouseGenerator3 : MonoBehaviour
{
    [Header("Room Library")]
    public List<RoomLayout> roomPool = new List<RoomLayout>();

    [Header("Generation")]
    [Min(1)] public int roomCount = 12;
    [Min(0f)] public float spacing = 0f; // 0 = flush
    public int seed = 0;
    public bool useRandomSeed = true;

    [Header("Compactness vs Random Branching")]
    [Range(0f, 3f)] public float touchWeight = 1.5f;
    [Range(0f, 3f)] public float centerWeight = 0.35f;
    [Range(0f, 1f)] public float branchChance = 0.25f;
    [Range(1, 30)] public int attemptsPerRoom = 20;

    [Header("Auto Measurement")]
    public bool measureUsingColliders = true;          // use colliders for footprint
    public bool fallbackToDimensions = true;           // if no colliders found, use RoomLayout.dimensions

    [Header("Debug")]
    public bool drawGizmos = true;

    // ---------------- Internals ----------------

    private System.Random rng;
    private readonly List<PlacedRoom> placed = new List<PlacedRoom>();

    private static readonly Vector3[] Dir = new Vector3[]
    {
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    private readonly Dictionary<RoomLayout, MeasuredFootprint> footprintCache = new Dictionary<RoomLayout, MeasuredFootprint>();

    [Serializable]
    private class PlacedRoom
    {
        public RoomLayout layout;
        public Vector3 pos;               // we treat this as bounds-center in world XZ
        public Vector2 size;              // footprint (x,z)
        public Vector3 centerOffsetLocal; // local offset from root pivot -> bounds center
        public GameObject instance;

        public Bounds2D Bounds => Bounds2D.FromCenterSize(pos, size);
    }

    private struct Bounds2D
    {
        public float minX, maxX, minZ, maxZ;

        public static Bounds2D FromCenterSize(Vector3 center, Vector2 size)
        {
            float hx = size.x * 0.5f;
            float hz = size.y * 0.5f;
            return new Bounds2D
            {
                minX = center.x - hx,
                maxX = center.x + hx,
                minZ = center.z - hz,
                maxZ = center.z + hz
            };
        }

        public bool Overlaps(Bounds2D other, float epsilon = 0.0001f)
        {
            // Strict overlap; touching edges is allowed.
            if (maxX <= other.minX + epsilon) return false;
            if (minX >= other.maxX - epsilon) return false;
            if (maxZ <= other.minZ + epsilon) return false;
            if (minZ >= other.maxZ - epsilon) return false;
            return true;
        }
    }

    private struct Candidate
    {
        public RoomLayout layout;
        public Vector3 pos; // desired bounds-center
        public Vector2 size;
        public Vector3 centerOffsetLocal;
        public float score;
    }

    private struct MeasuredFootprint
    {
        public Vector2 sizeXZ;            // (x,z)
        public Vector3 centerOffsetLocal; // local offset from prefab root to bounds center
        public bool usedFallback;
    }

    // ---------------- Public API ----------------

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();

        if (roomPool == null || roomPool.Count == 0)
        {
            Debug.LogError("HouseGenerator: roomPool is empty.");
            return;
        }

        rng = useRandomSeed ? new System.Random(Environment.TickCount) : new System.Random(seed);

        // First room at origin (origin = bounds-center)
        var first = PickWeighted(roomPool);
        var firstFp = GetFootprint(first);
        PlaceRoom(first, Vector3.zero, firstFp);

        for (int i = 1; i < roomCount; i++)
        {
            if (!TryPlaceNextRoom())
            {
                Debug.LogWarning($"HouseGenerator: Could not place room #{i + 1}. Stopping early. Placed: {placed.Count}");
                break;
            }
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if (placed[i].instance != null)
                DestroyImmediate(placed[i].instance);
        }
        placed.Clear();
        footprintCache.Clear();
    }

    // ---------------- Placement Logic ----------------

    private bool TryPlaceNextRoom()
    {
        RoomLayout layout = PickWeighted(roomPool);
        var fp = GetFootprint(layout);

        PlacedRoom anchor = PickAnchor();

        Candidate best = default;
        bool found = false;

        for (int attempt = 0; attempt < attemptsPerRoom; attempt++)
        {
            if (attempt > 0 && attempt % 5 == 0)
                anchor = PickAnchor();

            Vector3 d = Dir[rng.Next(Dir.Length)];
            Vector3 candidatePos = ComputeAdjacentPosition(anchor, fp.sizeXZ, d, spacing);

            var candBounds = Bounds2D.FromCenterSize(candidatePos, fp.sizeXZ);
            if (HasOverlap(candBounds)) continue;

            int touches = CountTouches(candBounds, spacing);
            float centerDist = Vector3.Distance(candidatePos, ComputeCentroid());
            float jitter = (float)rng.NextDouble() * 0.15f;

            float score = (touches * touchWeight) - (centerDist * centerWeight) + jitter;

            if (!found || score > best.score)
            {
                best = new Candidate
                {
                    layout = layout,
                    pos = candidatePos,
                    size = fp.sizeXZ,
                    centerOffsetLocal = fp.centerOffsetLocal,
                    score = score
                };
                found = true;
            }
        }

        if (!found) return false;

        PlaceRoom(best.layout, best.pos, new MeasuredFootprint
        {
            sizeXZ = best.size,
            centerOffsetLocal = best.centerOffsetLocal,
            usedFallback = false
        });

        return true;
    }

    private PlacedRoom PickAnchor()
    {
        if (placed.Count == 1) return placed[0];

        bool branch = ((float)rng.NextDouble() < branchChance);

        if (!branch)
        {
            Vector3 c = ComputeCentroid();
            int bestIndex = 0;
            float best = float.PositiveInfinity;

            int samples = Mathf.Min(6, placed.Count);
            for (int i = 0; i < samples; i++)
            {
                int idx = rng.Next(placed.Count);
                float d = Vector3.Distance(placed[idx].pos, c);
                if (d < best) { best = d; bestIndex = idx; }
            }
            return placed[bestIndex];
        }
        else
        {
            int bestIndex = 0;
            int bestTouches = int.MaxValue;

            int samples = Mathf.Min(8, placed.Count);
            for (int i = 0; i < samples; i++)
            {
                int idx = rng.Next(placed.Count);
                int t = CountTouches(placed[idx].Bounds, spacing);
                if (t < bestTouches) { bestTouches = t; bestIndex = idx; }
            }
            return placed[bestIndex];
        }
    }

    private Vector3 ComputeAdjacentPosition(PlacedRoom anchor, Vector2 newSize, Vector3 dir, float gap)
    {
        float ax = anchor.size.x * 0.5f;
        float az = anchor.size.y * 0.5f;
        float nx = newSize.x * 0.5f;
        float nz = newSize.y * 0.5f;

        Vector3 p = anchor.pos;

        if (dir == Vector3.right)   p.x += ax + nx + gap;
        if (dir == Vector3.left)    p.x -= ax + nx + gap;
        if (dir == Vector3.forward) p.z += az + nz + gap;
        if (dir == Vector3.back)    p.z -= az + nz + gap;

        return p;
    }

    private bool HasOverlap(Bounds2D candidate)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if (candidate.Overlaps(placed[i].Bounds))
                return true;
        }
        return false;
    }

    private int CountTouches(Bounds2D candidate, float gap)
    {
        const float tol = 0.01f;
        int touches = 0;

        for (int i = 0; i < placed.Count; i++)
        {
            var b = placed[i].Bounds;

            bool overlapZ = !(candidate.maxZ <= b.minZ || candidate.minZ >= b.maxZ);
            bool overlapX = !(candidate.maxX <= b.minX || candidate.minX >= b.maxX);

            if (overlapZ && Mathf.Abs(candidate.minX - (b.maxX + gap)) <= tol) touches++;
            if (overlapZ && Mathf.Abs(candidate.maxX - (b.minX - gap)) <= tol) touches++;
            if (overlapX && Mathf.Abs(candidate.minZ - (b.maxZ + gap)) <= tol) touches++;
            if (overlapX && Mathf.Abs(candidate.maxZ - (b.minZ - gap)) <= tol) touches++;
        }

        return touches;
    }

    private Vector3 ComputeCentroid()
    {
        if (placed.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < placed.Count; i++) sum += placed[i].pos;
        return sum / placed.Count;
    }

    // ---------------- Measurement (Colliders) ----------------

    private MeasuredFootprint GetFootprint(RoomLayout layout)
    {
        if (layout == null || layout.prefab == null)
        {
            return new MeasuredFootprint
            {
                sizeXZ = Vector2.one,
                centerOffsetLocal = Vector3.zero,
                usedFallback = true
            };
        }

        if (footprintCache.TryGetValue(layout, out var cached))
            return cached;

        // Temporary instantiate to measure nested colliders correctly
        GameObject temp = Instantiate(layout.prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        temp.transform.localScale = Vector3.one;

        bool ok = TryGetHierarchyBoundsWorld(temp, out var b, measureUsingColliders);

        Vector2 sizeXZ;
        Vector3 centerOffsetLocal;
        bool usedFallback = false;

        if (ok)
        {
            sizeXZ = new Vector2(b.size.x, b.size.z);
            centerOffsetLocal = temp.transform.InverseTransformPoint(b.center);
        }
        else if (fallbackToDimensions && layout.dimensions.x > 0 && layout.dimensions.y > 0)
        {
            // fallback to SO dimensions if no colliders found
            sizeXZ = new Vector2(layout.dimensions.x, layout.dimensions.y);
            centerOffsetLocal = Vector3.zero;
            usedFallback = true;
        }
        else
        {
            sizeXZ = Vector2.one;
            centerOffsetLocal = Vector3.zero;
            usedFallback = true;
            Debug.LogWarning($"HouseGenerator: Could not measure bounds for '{layout.name}'. Add colliders or set dimensions.");
        }

        DestroyImmediate(temp);

        var result = new MeasuredFootprint
        {
            sizeXZ = sizeXZ,
            centerOffsetLocal = centerOffsetLocal,
            usedFallback = usedFallback
        };

        footprintCache[layout] = result;
        return result;
    }

    private bool TryGetHierarchyBoundsWorld(GameObject root, out Bounds worldBounds, bool useColliders)
    {
        worldBounds = default;
        bool hasAny = false;

        if (useColliders)
        {
            var cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null || !c.enabled) continue;

                if (!hasAny) { worldBounds = c.bounds; hasAny = true; }
                else worldBounds.Encapsulate(c.bounds);
            }
        }
        else
        {
            var rends = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                if (r == null || !r.enabled) continue;

                if (!hasAny) { worldBounds = r.bounds; hasAny = true; }
                else worldBounds.Encapsulate(r.bounds);
            }
        }

        return hasAny;
    }

    // ---------------- Instantiate & Align to Bounds Center ----------------

    private void PlaceRoom(RoomLayout layout, Vector3 boundsCenterPos, MeasuredFootprint fp)
    {
        GameObject go = Instantiate(layout.prefab, boundsCenterPos, Quaternion.identity, transform);
        go.name = layout.name;

        // Shift so the prefab's measured bounds center lands at boundsCenterPos
        go.transform.position = boundsCenterPos - fp.centerOffsetLocal;

        placed.Add(new PlacedRoom
        {
            layout = layout,
            pos = boundsCenterPos,
            size = fp.sizeXZ,
            centerOffsetLocal = fp.centerOffsetLocal,
            instance = go
        });
    }

    // ---------------- Weighted picking ----------------

    private RoomLayout PickWeighted(List<RoomLayout> list)
    {
        double total = 0;
        for (int i = 0; i < list.Count; i++) total += Math.Max(0.0, list[i].weight);

        if (total <= 0.0)
            return list[rng.Next(list.Count)];

        double r = rng.NextDouble() * total;
        double acc = 0;

        for (int i = 0; i < list.Count; i++)
        {
            acc += Math.Max(0.0, list[i].weight);
            if (r <= acc) return list[i];
        }

        return list[list.Count - 1];
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = Color.green;
        for (int i = 0; i < placed.Count; i++)
        {
            var p = placed[i];
            Vector3 center = p.pos;
            Vector3 size = new Vector3(p.size.x, 0.1f, p.size.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
