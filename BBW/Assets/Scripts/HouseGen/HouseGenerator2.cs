using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HouseGenerator2 : MonoBehaviour
{
    [Header("Generation")]
    [Tooltip("If true, generates basement, two main floors and an attic. If false, only one main floor + attic.")]
    public bool useFourFloors = true;

    [Header("Heights")]
    public float basementHeight = 3f;
    public float floorHeight = 3f;
    public float atticHeight = 2.5f;

    [Header("Grid Settings")]
    [Tooltip("Size of one grid tile in world units.")]
    public float tileSize = 2.5f;

    [Tooltip("Gap between rooms, in tiles.")]
    public int tileGapBetweenRooms = 0;

    [Header("Prefabs")]
    public GameObject basementPrefab;
    public GameObject atticPrefab;

    [Header("Floor Tiles")]
    public GameObject floorTilePrefab;
    [Tooltip("Small negative offset to avoid z-fighting with room floors.")]
    public float floorYOffset = -0.02f;

    [Header("Room Types (First Floor Mandatory)")]
    public RoomTypeSO foyer;
    public RoomTypeSO kitchen;
    public RoomTypeSO livingRoom;

    [Header("Other Rooms (Optional)")]
    public RoomTypeSO[] otherRooms;

    [Header("Special Rooms")]
    public RoomTypeSO bedroom;
    public RoomTypeSO bathroom;


    // ─────────────────────────────────────────────────────────────
    // Internal types / state
    // ─────────────────────────────────────────────────────────────

    private class RoomInstance
    {
        public GameObject gameObject;
        public RoomTypeSO type;
        public int floorLevel;           // 0 = first floor, 1 = second floor
        public Vector2Int gridPos;       // bottom-left tile on that floor
        public Vector2Int gridSize;      // width x depth in tiles
    }

    /// <summary>Tiles occupied on each floor level.</summary>
    private readonly Dictionary<int, HashSet<Vector2Int>> occupiedTilesPerFloor =
        new Dictionary<int, HashSet<Vector2Int>>();

    private readonly List<RoomInstance> placedRooms = new List<RoomInstance>();

    private Transform basementParent;
    private Transform firstFloorParent;
    private Transform secondFloorParent;
    private Transform atticParent;

    private Vector2Int foyerGridPos = Vector2Int.zero;

    // ─────────────────────────────────────────────────────────────
    // Unity entrypoints
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        GenerateHouse();
    }

    [ContextMenu("Generate House")]
    public void GenerateHouse()
    {
        ClearOldHouse();
        InitFloorParents();

        CreateBasement();

        List<RoomTypeSO> mustFirst = new List<RoomTypeSO>();
        List<RoomTypeSO> flexible = new List<RoomTypeSO>();

        if (otherRooms != null)
        {
            foreach (var r in otherRooms)
            {
                // NEW: skip the explicitly handled types
                if (r == null || r == bedroom || r == bathroom) 
                    continue;

                if (r.mustBeOnFirstFloor) mustFirst.Add(r);
                else flexible.Add(r);
            }
        }


        // Shuffle flexible rooms for variety
        flexible = flexible.OrderBy(_ => Random.value).ToList();

        List<RoomTypeSO> firstFloorOptionals = new List<RoomTypeSO>();
        List<RoomTypeSO> secondFloorOptionals = new List<RoomTypeSO>();

        // Always start by putting every must-first room on floor 1
        firstFloorOptionals.AddRange(mustFirst);

        if (useFourFloors && flexible.Count > 1)
        {
            // Only split if we have at least 2 flexible rooms
            int half = flexible.Count / 2;

            // First half → first floor, second half → second floor
            firstFloorOptionals.AddRange(flexible.Take(half));
            secondFloorOptionals.AddRange(flexible.Skip(half));
        }
        else
        {
            // Either no second floor, or only 0–1 flexible rooms:
            // keep them all on the first floor.
            firstFloorOptionals.AddRange(flexible);
        }

        CreateFirstFloor(firstFloorOptionals);

        if (useFourFloors)
        {
            CreateSecondFloor(secondFloorOptionals);
        }

        // Fill floor tiles between rooms on each floor
        GenerateFloorsBetweenRooms();

        CreateAttic();
    }

    // ─────────────────────────────────────────────────────────────
    // Setup / cleanup
    // ─────────────────────────────────────────────────────────────

    private void ClearOldHouse()    
    {
        placedRooms.Clear();
        occupiedTilesPerFloor.Clear();

        // Destroy children under this generator
        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in transform)
            toDestroy.Add(child);

        foreach (Transform t in toDestroy)
        {
            if (Application.isPlaying)
                Destroy(t.gameObject);
            else
                DestroyImmediate(t.gameObject);
        }
    }

    private void InitFloorParents()
    {
        basementParent = new GameObject("Basement").transform;
        basementParent.SetParent(transform);
        basementParent.localPosition = Vector3.zero;

        firstFloorParent = new GameObject("First Floor").transform;
        firstFloorParent.SetParent(transform);
        firstFloorParent.localPosition = Vector3.zero;

        if (useFourFloors)
        {
            secondFloorParent = new GameObject("Second Floor").transform;
            secondFloorParent.SetParent(transform);
            secondFloorParent.localPosition = Vector3.up * floorHeight;
        }

        atticParent = new GameObject("Attic Parent").transform;
        atticParent.SetParent(transform);
        atticParent.localPosition = Vector3.zero;
    }

    private HashSet<Vector2Int> GetFloorTileSet(int floorLevel)
    {
        if (!occupiedTilesPerFloor.ContainsKey(floorLevel))
            occupiedTilesPerFloor[floorLevel] = new HashSet<Vector2Int>();
        return occupiedTilesPerFloor[floorLevel];
    }

    private Transform GetFloorParentForLevel(int floorLevel)
    {
        if (floorLevel == 0) return firstFloorParent;
        if (floorLevel == 1) return secondFloorParent;
        return null;
    }

    // ─────────────────────────────────────────────────────────────
    // Basement / Attic
    // ─────────────────────────────────────────────────────────────

    private void CreateBasement()
    {
        if (basementPrefab == null) return;

        Vector3 pos = Vector3.down * basementHeight;
        Instantiate(basementPrefab, pos, Quaternion.identity, basementParent);
    }

    private void CreateAttic()
    {
        if (atticPrefab == null) return;

        int topFloorLevel = useFourFloors ? 1 : 0;

        Bounds? combined = null;

        foreach (RoomInstance room in placedRooms.Where(r => r.floorLevel == topFloorLevel))
        {
            Bounds b = CalculateRoomBounds(room.gameObject);
            if (combined == null) combined = b;
            else
            {
                Bounds tmp = combined.Value;
                tmp.Encapsulate(b);
                combined = tmp;
            }
        }

        if (combined == null)
        {
            Debug.LogWarning("No rooms found on top floor for attic placement.");
            return;
        }

        Bounds bounds = combined.Value;
        float atticY = (topFloorLevel + 1) * floorHeight + atticHeight / 2f;

        GameObject attic = Instantiate(
            atticPrefab,
            new Vector3(bounds.center.x, atticY, bounds.center.z),
            Quaternion.identity,
            atticParent
        );

        attic.transform.localScale = new Vector3(bounds.size.x, atticHeight, bounds.size.z);
    }

    // ─────────────────────────────────────────────────────────────
    // Main floors
    // ─────────────────────────────────────────────────────────────

    private void CreateFirstFloor(List<RoomTypeSO> optionalRooms)
    {
        int floorLevel = 0;

        // Mandatory rooms
        PlaceRoomOnFloor(firstFloorParent, foyer, floorLevel, needsStairs: useFourFloors);
        PlaceRoomOnFloor(firstFloorParent, kitchen, floorLevel);
        PlaceRoomOnFloor(firstFloorParent, livingRoom, floorLevel);

        // Cache foyer position (as before) ...
        RoomInstance foyerRoom = placedRooms.FirstOrDefault(
            r => r.floorLevel == floorLevel && r.type == foyer
        );
        if (foyerRoom != null)
            foyerGridPos = foyerRoom.gridPos;

        // NEW: place bedroom (normal placement)
        if (bedroom != null)
        {
            PlaceRoomOnFloor(firstFloorParent, bedroom, floorLevel);
        }

        // NEW: place bathroom (special adjacency placement)
        if (bathroom != null)
        {
            PlaceRoomOnFloor(firstFloorParent, bathroom, floorLevel);
        }

        // Remaining optional rooms
        if (optionalRooms != null)
        {
            foreach (RoomTypeSO roomType in optionalRooms)
            {
                if (roomType == null) continue;
                PlaceRoomOnFloor(firstFloorParent, roomType, floorLevel);
            }
        }
    }


    private void CreateSecondFloor(List<RoomTypeSO> optionalRooms)
    {
        if (!useFourFloors || secondFloorParent == null) return;

        int floorLevel = 1;

        if (optionalRooms != null)
        {
            foreach (RoomTypeSO roomType in optionalRooms)
            {
                if (roomType == null) continue;
                PlaceRoomOnFloor(secondFloorParent, roomType, floorLevel);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Room placement
    // ─────────────────────────────────────────────────────────────

    private void PlaceRoomOnFloor(Transform floorParent, RoomTypeSO roomType, int floorLevel, bool needsStairs = false)
    {
        if (roomType == null)
        {
            Debug.LogWarning("Tried to place a null RoomTypeSO.");
            return;
        }

        RoomLayout layout = GetRandomLayout(roomType, needsStairs);
        if (layout == null || layout.prefab == null)
        {
            Debug.LogWarning($"RoomType {roomType.roomName} has no valid layout or prefab.");
            return;
        }

        // 1) Get the VISUAL bounds of the prefab in its prefab space
        Bounds prefabBounds = CalculateRoomBounds(layout.prefab);
        Vector3 boundsSize = prefabBounds.size;
        Vector3 centerOffset = prefabBounds.center - layout.prefab.transform.position;

        // 2) Derive footprint (width/depth) in tiles from those bounds
        int widthInTiles = Mathf.Max(1, Mathf.CeilToInt(boundsSize.x / tileSize));
        int depthInTiles = Mathf.Max(1, Mathf.CeilToInt(boundsSize.z / tileSize));
        Vector2Int gridSize = new Vector2Int(widthInTiles, depthInTiles);

        HashSet<Vector2Int> occupied = GetFloorTileSet(floorLevel);

        // OLD:
        // Vector2Int gridPos = FindPlacementForRoom(floorLevel, gridSize, occupied);

        // NEW: bathroom uses a special placement function
        Vector2Int gridPos;
        if (roomType == bathroom)
        {
            gridPos = FindPlacementForBathroom(floorLevel, gridSize, occupied);
        }
        else
        {
            gridPos = FindPlacementForRoom(floorLevel, gridSize, occupied);
        }

        // 4) Convert grid bottom-left → world bottom-left
        Vector3 worldMin = new Vector3(
            gridPos.x * tileSize,
            floorLevel * floorHeight,
            gridPos.y * tileSize
        );

        // We want the bounds MIN (bottom-left in XZ) to sit on worldMin.
        // Given: newCenter = worldPos + centerOffset
        //        newMin = newCenter - 0.5 * boundsSize
        // Set newMin = worldMin ⇒
        // worldPos = worldMin + 0.5 * boundsSize - centerOffset
        Vector3 worldPos = worldMin
                        + new Vector3(boundsSize.x * 0.5f, 0f, boundsSize.z * 0.5f)
                        - new Vector3(centerOffset.x, 0f, centerOffset.z);

        GameObject roomObj = Instantiate(
            layout.prefab,
            worldPos,
            Quaternion.identity,
            floorParent
        );

        // 5) Mark all tiles covered by this room as occupied
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int z = 0; z < gridSize.y; z++)
            {
                occupied.Add(new Vector2Int(gridPos.x + x, gridPos.y + z));
            }
        }

        // 6) Register the room instance
        RoomInstance instance = new RoomInstance
        {
            gameObject = roomObj,
            type = roomType,
            floorLevel = floorLevel,
            gridPos = gridPos,
            gridSize = gridSize
        };
        placedRooms.Add(instance);

        if (roomType == foyer)
            foyerGridPos = gridPos;

        Debug.Log($"Placed {roomType.roomName} on floor {floorLevel} at grid {gridPos} (size {gridSize})");
    }


    /// <summary>
    /// Choose a tile position for a new room on a given floor.
    /// First room on that floor → (0,0).
    /// Others: try adjacencies around existing rooms; if none fits, search near the cluster but keep it attached.
    /// </summary>
    private Vector2Int FindPlacementForRoom(int floorLevel, Vector2Int roomSize, HashSet<Vector2Int> occupied)
    {
        // Any rooms on this floor yet?
        bool anyOnFloor = placedRooms.Any(r => r.floorLevel == floorLevel);
        if (!anyOnFloor)
            return Vector2Int.zero;

        List<Vector2Int> candidates = new List<Vector2Int>();

        int gap = Mathf.Max(0, tileGapBetweenRooms);

        // 1) Try simple adjacency around existing rooms
        foreach (RoomInstance existing in placedRooms.Where(r => r.floorLevel == floorLevel))
        {
            Vector2Int ePos = existing.gridPos;
            Vector2Int eSize = existing.gridSize;

            // Right of existing room
            Vector2Int right = new Vector2Int(ePos.x + eSize.x + gap, ePos.y);
            if (!DoesRoomOverlap(right, roomSize, occupied))
                candidates.Add(right);

            // Left of existing room
            Vector2Int left = new Vector2Int(ePos.x - roomSize.x - gap, ePos.y);
            if (!DoesRoomOverlap(left, roomSize, occupied))
                candidates.Add(left);

            // Above existing room (positive Z)
            Vector2Int up = new Vector2Int(ePos.x, ePos.y + eSize.y + gap);
            if (!DoesRoomOverlap(up, roomSize, occupied))
                candidates.Add(up);

            // Below existing room (negative Z)
            Vector2Int down = new Vector2Int(ePos.x, ePos.y - roomSize.y - gap);
            if (!DoesRoomOverlap(down, roomSize, occupied))
                candidates.Add(down);
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        // 2) If no simple adjacency works, search near the existing cluster
        //    but enforce adjacency to at least one occupied tile.

        // Get bounding box of occupied tiles on this floor
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var tile in occupied)
        {
            if (tile.x < minX) minX = tile.x;
            if (tile.x > maxX) maxX = tile.x;
            if (tile.y < minZ) minZ = tile.y;
            if (tile.y > maxZ) maxZ = tile.y;
        }

        int padding = 1; // how far around the cluster we search
        int searchMinX = minX - roomSize.x - gap - padding;
        int searchMaxX = maxX + gap + padding;
        int searchMinZ = minZ - roomSize.y - gap - padding;
        int searchMaxZ = maxZ + gap + padding;

        for (int x = searchMinX; x <= searchMaxX; x++)
        {
            for (int z = searchMinZ; z <= searchMaxZ; z++)
            {
                Vector2Int candidate = new Vector2Int(x, z);
                if (DoesRoomOverlap(candidate, roomSize, occupied))
                    continue;

                if (IsAdjacentToAnyOccupied(candidate, roomSize, occupied))
                {
                    return candidate;
                }
            }
        }

        // 3) Fallback: put it at the cluster center in grid-space
        int centerX = (minX + maxX) / 2;
        int centerZ = (minZ + maxZ) / 2;
        Debug.LogWarning($"Could not find perfect adjacent position for room on floor {floorLevel}, using cluster center.");
        return new Vector2Int(centerX, centerZ);
    }

    /// <summary>
    /// Place a bathroom next to a bedroom or kitchen (if they exist on this floor).
    /// Falls back to normal placement if no suitable spot is found.
    /// </summary>
    private Vector2Int FindPlacementForBathroom(int floorLevel, Vector2Int roomSize, HashSet<Vector2Int> occupied)
    {
        // "Anchor" rooms we want to stick to: bedrooms and kitchens on this floor
        var anchors = placedRooms
            .Where(r => r.floorLevel == floorLevel && (r.type == bedroom || r.type == kitchen))
            .ToList();

        // If we don't have any anchors yet (e.g. no bedroom or kitchen on that floor),
        // just use the regular placement.
        if (anchors.Count == 0)
        {
            return FindPlacementForRoom(floorLevel, roomSize, occupied);
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        int gap = Mathf.Max(0, tileGapBetweenRooms);

        // Try to place the bathroom directly adjacent (left/right/up/down) to any anchor room
        foreach (RoomInstance existing in anchors)
        {
            Vector2Int ePos = existing.gridPos;
            Vector2Int eSize = existing.gridSize;

            // Right of anchor
            Vector2Int right = new Vector2Int(ePos.x + eSize.x + gap, ePos.y);
            if (!DoesRoomOverlap(right, roomSize, occupied))
                candidates.Add(right);

            // Left of anchor
            Vector2Int left = new Vector2Int(ePos.x - roomSize.x - gap, ePos.y);
            if (!DoesRoomOverlap(left, roomSize, occupied))
                candidates.Add(left);

            // Above anchor (positive Z)
            Vector2Int up = new Vector2Int(ePos.x, ePos.y + eSize.y + gap);
            if (!DoesRoomOverlap(up, roomSize, occupied))
                candidates.Add(up);

            // Below anchor (negative Z)
            Vector2Int down = new Vector2Int(ePos.x, ePos.y - roomSize.y - gap);
            if (!DoesRoomOverlap(down, roomSize, occupied))
                candidates.Add(down);
        }

        if (candidates.Count > 0)
        {
            // Pick a random valid spot that's touching at least one bedroom or kitchen
            return candidates[Random.Range(0, candidates.Count)];
        }

        // If nothing fits right against a bedroom/kitchen, fall back to the generic
        // placement (still keeps the house contiguous).
        return FindPlacementForRoom(floorLevel, roomSize, occupied);
    }


    private bool DoesRoomOverlap(Vector2Int bottomLeft, Vector2Int size, HashSet<Vector2Int> occupied)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int tile = new Vector2Int(bottomLeft.x + x, bottomLeft.y + z);
                if (occupied.Contains(tile))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the candidate room rectangle touches at least one occupied tile (edge-adjacent).
    /// </summary>
    private bool IsAdjacentToAnyOccupied(Vector2Int bottomLeft, Vector2Int size, HashSet<Vector2Int> occupied)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int tile = new Vector2Int(bottomLeft.x + x, bottomLeft.y + z);

                // 4-neighbors
                Vector2Int[] neighbors =
                {
                    new Vector2Int(tile.x + 1, tile.y),
                    new Vector2Int(tile.x - 1, tile.y),
                    new Vector2Int(tile.x, tile.y + 1),
                    new Vector2Int(tile.x, tile.y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (occupied.Contains(n))
                        return true;
                }
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    // Floor fill between rooms
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns floor tiles under the entire bounding area of rooms on each floor,
    /// including under the rooms themselves. This guarantees no gaps.
    /// </summary>
    private void GenerateFloorsBetweenRooms()
    {
        if (floorTilePrefab == null) return;

        foreach (var kvp in occupiedTilesPerFloor)
        {
            int floorLevel = kvp.Key;
            HashSet<Vector2Int> occupied = kvp.Value;

            if (occupied == null || occupied.Count == 0)
                continue;

            Transform floorParent = GetFloorParentForLevel(floorLevel);
            if (floorParent == null)
                continue;

            // 1) Find bounding rectangle of all room tiles on this floor
            int minX = int.MaxValue, maxX = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;

            foreach (Vector2Int tile in occupied)
            {
                if (tile.x < minX) minX = tile.x;
                if (tile.x > maxX) maxX = tile.x;
                if (tile.y < minZ) minZ = tile.y;
                if (tile.y > maxZ) maxZ = tile.y;
            }

            // Optional: add a little border
            // int padding = 1;
            // minX -= padding;
            // maxX += padding;
            // minZ -= padding;
            // maxZ += padding;

            // 2) Fill every tile in that rectangle with a floor tile
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3 worldPos = new Vector3(
                        x * tileSize,
                        floorLevel * floorHeight + floorYOffset,
                        z * tileSize
                    );

                    Instantiate(floorTilePrefab, worldPos, Quaternion.identity, floorParent);
                }
            }
        }
    }


    // ─────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────

    private RoomLayout GetRandomLayout(RoomTypeSO roomType, bool needsStairs)
    {
        if (roomType == null || roomType.layouts == null || roomType.layouts.Length == 0)
        {
            Debug.LogWarning("RoomTypeSO has no layouts.");
            return null;
        }

        List<RoomLayout> valid = new List<RoomLayout>();

        foreach (RoomLayout layout in roomType.layouts)
        {
            if (layout == null) continue;

            if (needsStairs && layout.hasStairs)
                valid.Add(layout);
            else if (!needsStairs && !layout.hasStairs)
                valid.Add(layout);
        }

        if (valid.Count == 0)
        {
            Debug.LogWarning(
                $"No matching layouts (stairs={needsStairs}) for {roomType.roomName}. Using first layout."
            );
            return roomType.layouts[0];
        }

        return valid[Random.Range(0, valid.Count)];
    }

    private Bounds CalculateRoomBounds(GameObject prefabOrInstance)
    {
        if (prefabOrInstance == null)
            return new Bounds(Vector3.zero, Vector3.one);

        Renderer[] renderers = prefabOrInstance.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(prefabOrInstance.transform.position, Vector3.one * 2f);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw rooms
        Gizmos.color = Color.cyan;
        foreach (RoomInstance room in placedRooms)
        {
            Bounds b = CalculateRoomBounds(room.gameObject);
            Gizmos.DrawWireCube(b.center, b.size);
        }

        // Draw occupied tiles per floor
        Gizmos.color = Color.yellow;
        foreach (var kvp in occupiedTilesPerFloor)
        {
            int floor = kvp.Key;
            foreach (Vector2Int tile in kvp.Value)
            {
                Vector3 pos = new Vector3(
                    tile.x * tileSize,
                    floor * floorHeight + 0.05f,
                    tile.y * tileSize
                );
                Gizmos.DrawCube(pos, new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f));
            }
        }
    }
}
