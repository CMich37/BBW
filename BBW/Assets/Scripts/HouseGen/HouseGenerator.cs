//debuggggggggg
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HouseGenerator : MonoBehaviour
{
    [Header("Config")]
    public float basementHeight = 3f;
    public float atticHeight = 2.5f;
    public float floorHeight = 3f;
    public float hallwayWidth = 10f;
    public float roomSpacing = 0f;

    [Header("Prefabs")]
    public GameObject basementPrefab;
    public GameObject atticPrefab;
    public GameObject hallwayPrefab;
    public GameObject doorPrefab;

    [Header("Room Types")]
    public RoomTypeSO foyer;
    public RoomTypeSO kitchen;
    public RoomTypeSO livingRoom;
    public RoomTypeSO[] otherRooms;

    [Header("Room Placement")]
    [Range(0f, 1f)]
    public float branchOutProbability = 0.3f; // Probability to place room away from compact center

    [Header("Debug")]
    public bool debugLogging = false;

    // Track all occupied space per floor using Bounds
    private bool isFourFloors;
    private Dictionary<int, List<Bounds>> occupiedBoundsPerFloor = new Dictionary<int, List<Bounds>>();
    private List<RoomInstance> placedRooms = new List<RoomInstance>();

    private class RoomInstance
    {
        public GameObject gameObject;
        public RoomTypeSO type;
        public Vector3 position; // World position (x, z on floor plane, y = height)
        public Vector2 dimensions; // Width and depth
        public int floorLevel;
        public Bounds bounds; // For collision detection
        public RoomLayout layout; // Store layout reference
    }

    void Start()
    {
        GenerateHouse();
    }

    void GenerateHouse()
    {
        // Initialize all floor bound lists
        occupiedBoundsPerFloor[0] = new List<Bounds>();
        occupiedBoundsPerFloor[1] = new List<Bounds>();
        occupiedBoundsPerFloor[2] = new List<Bounds>();
        occupiedBoundsPerFloor[3] = new List<Bounds>();

        isFourFloors = false;
        if (isFourFloors)
        {
            Debug.Log("is 4 floors");
        }
        else
        {
            Debug.Log("is 3 floors");
        }
        CreateBasement();
        CreateFirstFloor();
    }

    void CreateBasement()
    {
        GameObject basement = Instantiate(basementPrefab, Vector3.down * basementHeight, Quaternion.identity);
    }

    void CreateFirstFloor()
    {
        // Place core rooms
        List<RoomTypeSO> coreRooms = new List<RoomTypeSO>();
        coreRooms.Add(foyer);
        coreRooms.Add(livingRoom);
        coreRooms.Add(kitchen);

        List<int> order = GetRandomOrder(3);
        Debug.Log("order is: " + string.Join(", ", order));
        for (int i = 0; i < order.Count; i++)
        {
            bool isFirst = (i == 0);
            PlaceRoom(1, coreRooms[order[i] - 1], isFirst, isFourFloors);
        }
        Debug.Log($"Placed {placedRooms.Count} rooms on floor 1");
    }

    void PlaceRoom(int floor, RoomTypeSO roomType, bool isFirst, bool needsStairs = false)
    {
        // Select a layout from the room type
        RoomLayout selectedLayout = SelectLayout(roomType, needsStairs);
        if (selectedLayout == null)
        {
            Debug.LogError($"No suitable layout found for {roomType.roomName}");
            return;
        }

        // Get dimensions from the prefab's renderers
        Bounds prefabBounds = GetChildRendererBounds(selectedLayout.prefab);
        // Vector2 roomDimensions = new Vector2(Mathf.Abs(prefabBounds.size.x), Mathf.Abs(prefabBounds.size.z));
        Vector2 roomDimensions = new Vector2(Mathf.Abs(selectedLayout.dimensions.x), Mathf.Abs(selectedLayout.dimensions.y));

        if (debugLogging)
        {
            Debug.Log($"[PlaceRoom] {roomType.roomName} - Prefab bounds: {prefabBounds}, Dimensions: {roomDimensions}");
        }

        Vector3 roomPosition;

        if (isFirst)
        {
            // First room is always at origin on the flat plane
            roomPosition = Vector3.zero;
            if (debugLogging)
                Debug.Log($"[PlaceRoom] {roomType.roomName} - First room, placing at origin");
        }
        else
        {
            // Find a valid position adjacent to existing rooms
            roomPosition = FindAdjacentPosition(floor, roomDimensions);
            if (roomPosition == Vector3.zero && placedRooms.Count > 0)
            {
                Debug.LogWarning($"Could not find valid position for {roomType.roomName}");
                return;
            }
        }

        // Create the room instance
        RoomInstance roomInstance = new RoomInstance
        {
            type = roomType,
            position = roomPosition,
            dimensions = roomDimensions,
            floorLevel = floor,
            layout = selectedLayout
        };

        // Calculate world bounds (accounts for room spacing)
        Bounds roomBounds = new Bounds(
            roomPosition,
            new Vector3(roomDimensions.x, floorHeight, roomDimensions.y)
        );
        roomInstance.bounds = roomBounds;

        // Instantiate the room prefab
        // roomInstance.gameObject = Instantiate(
        //     selectedLayout.prefab,
        //     roomPosition + Vector3.up * (floor - 1) * floorHeight,
        //     Quaternion.identity
        // );

        Vector3 instantiatePos = roomPosition - new Vector3(prefabBounds.center.x, 0, prefabBounds.center.z);
        instantiatePos += Vector3.up * (floor - 1) * floorHeight;

        roomInstance.gameObject = Instantiate(
            selectedLayout.prefab,
            instantiatePos,
            Quaternion.identity
        );

        // Add to tracking lists
        placedRooms.Add(roomInstance);
        occupiedBoundsPerFloor[floor].Add(roomBounds);

        if (debugLogging)
        {
            Debug.Log($"[PlaceRoom] Placed {roomType.roomName} at {roomPosition} on floor {floor}. Bounds: {roomBounds}");
        }
    }

    /// <summary>
    /// Selects a random layout from the room type.
    /// If needsStairs is true, prioritizes layouts with stairs.
    /// </summary>
    RoomLayout SelectLayout(RoomTypeSO roomType, bool needsStairs)
    {
        if (roomType.layouts == null || roomType.layouts.Length == 0)
        {
            return null;
        }

        if (needsStairs)
        {
            RoomLayout[] stairLayouts = roomType.layouts.Where(l => l.hasStairs).ToArray();
            if (stairLayouts.Length > 0)
            {
                return stairLayouts[Random.Range(0, stairLayouts.Length)];
            }
            Debug.LogWarning($"{roomType.roomName} has no layouts with stairs, selecting random layout");
        }

        return roomType.layouts[Random.Range(0, roomType.layouts.Length)];
    }

    /// <summary>
    /// Finds a valid position for a new room adjacent to existing rooms.
    /// Attempts to place room compactly first, then may branch out randomly.
    /// Accounts for roomSpacing between adjacent rooms.
    /// </summary>
    private Vector3 FindAdjacentPosition(int floor, Vector2 roomDimensions)
    {
        // Get only rooms on this floor
        List<RoomInstance> floorsRooms = placedRooms.Where(r => r.floorLevel == floor).ToList();

        if (floorsRooms.Count == 0)
        {
            return Vector3.zero;
        }

        if (debugLogging)
            Debug.Log($"[FindAdjacentPosition] Finding position for room {roomDimensions}. Floor {floor} has {floorsRooms.Count} existing rooms.");

        List<Vector3> candidatePositions = new List<Vector3>();

        // Generate candidate positions adjacent to all placed rooms on this floor
        foreach (RoomInstance existingRoom in floorsRooms)
        {
            // Try all four sides of the existing room
            Vector3[] adjacentPositions = GetAdjacentPositions(existingRoom, roomDimensions);

            if (debugLogging)
                Debug.Log($"[FindAdjacentPosition] Testing {existingRoom.type.roomName} at {existingRoom.position}. Generated 4 candidate positions.");

            foreach (Vector3 candidatePos in adjacentPositions)
            {
                bool isValid = IsValidPosition(floor, candidatePos, roomDimensions);

                if (debugLogging)
                    Debug.Log($"[FindAdjacentPosition]   Candidate {candidatePos} - Valid: {isValid}");

                if (isValid)
                {
                    candidatePositions.Add(candidatePos);
                }
            }
        }

        if (candidatePositions.Count == 0)
        {
            Debug.LogWarning("No valid adjacent positions found for room");
            return Vector3.zero;
        }

        if (debugLogging)
            Debug.Log($"[FindAdjacentPosition] Found {candidatePositions.Count} valid candidate positions");

        // Decide whether to place compactly (near center) or branch out
        Vector3 chosenPosition;
        if (Random.value < branchOutProbability && candidatePositions.Count > 1)
        {
            // Branch out: pick a random position
            chosenPosition = candidatePositions[Random.Range(0, candidatePositions.Count)];
            if (debugLogging)
                Debug.Log("Room placed with branch-out strategy");
        }
        else
        {
            // Compact: pick position closest to center (0,0)
            chosenPosition = candidatePositions.OrderBy(pos => new Vector2(pos.x, pos.z).sqrMagnitude).First();
            if (debugLogging)
                Debug.Log("Room placed with compact strategy");
        }

        return chosenPosition;
    }

    /// <summary>
    /// Returns four potential positions for a room adjacent to an existing room.
    /// Each side accounts for room spacing.
    /// </summary>
    private Vector3[] GetAdjacentPositions(RoomInstance existingRoom, Vector2 newRoomDimensions)
    {
        Vector3[] positions = new Vector3[4];
        Vector3 existingPos = existingRoom.position;
        float existingWidth = existingRoom.dimensions.x;
        float existingDepth = existingRoom.dimensions.y;
        float newWidth = newRoomDimensions.x;
        float newDepth = newRoomDimensions.y;
        float spacing = roomSpacing;

        // Right side (+X direction)
        positions[0] = existingPos + Vector3.right * (existingWidth / 2f + newWidth / 2f + spacing);
        positions[0].z = existingPos.z; // Align Z

        // Left side (-X direction)
        positions[1] = existingPos - Vector3.right * (existingWidth / 2f + newWidth / 2f + spacing);
        positions[1].z = existingPos.z; // Align Z

        // Front side (+Z direction)
        positions[2] = existingPos + Vector3.forward * (existingDepth / 2f + newDepth / 2f + spacing);
        positions[2].x = existingPos.x; // Align X

        // Back side (-Z direction)
        positions[3] = existingPos - Vector3.forward * (existingDepth / 2f + newDepth / 2f + spacing);
        positions[3].x = existingPos.x; // Align X

        return positions;
    }

    /// <summary>
    /// Checks if a position is valid (no overlaps, respects spacing).
    /// Uses a small epsilon to allow adjacent (touching) bounds without considering them overlapping.
    /// </summary>
    private bool IsValidPosition(int floor, Vector3 position, Vector2 dimensions)
    {
        // Create bounds for the new room
        Bounds newBounds = new Bounds(
            position,
            new Vector3(dimensions.x, floorHeight, dimensions.y)
        );

        // Small epsilon to allow touching bounds (spacing=0 means walls are flush)
        const float epsilon = 0.01f;
        Bounds expandedNewBounds = newBounds;
        expandedNewBounds.Expand(new Vector3(-epsilon * 2f, 0, -epsilon * 2f));

        if (debugLogging)
        {
            Debug.Log($"[IsValidPosition] Testing position {position} with bounds {newBounds}");
            Debug.Log($"[IsValidPosition] Checking against {occupiedBoundsPerFloor[floor].Count} occupied bounds on floor {floor}");
        }

        // Check against all existing bounds on this floor
        foreach (Bounds existingBounds in occupiedBoundsPerFloor[floor])
        {
            bool intersects = expandedNewBounds.Intersects(existingBounds);

            if (debugLogging)
            {
                Debug.Log($"[IsValidPosition]   Existing bounds {existingBounds} - Intersects: {intersects}");
            }

            if (intersects)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the combined bounds of all child renderers on a prefab.
    /// Used to calculate room dimensions when the parent has no renderer.
    /// </summary>
    // Bounds GetChildRendererBounds(GameObject go)
    // {
    //     Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

    //     if (renderers.Length > 0)
    //     {
    //         Bounds bounds = renderers[0].bounds;
    //         for (int i = 1; i < renderers.Length; i++)
    //         {
    //             bounds.Encapsulate(renderers[i].bounds);
    //         }
    //         return bounds;
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"No renderers found on {go.name} or its children");
    //         return new Bounds();
    //     }

    //     // Collider[] colliders = go.GetComponentsInChildren<MeshCollider>();

    //     // if (colliders.Length > 0)
    //     // {
    //     //     Bounds bounds = colliders[0].bounds;
    //     //     for (int i = 1; i < colliders.Length; i++)
    //     //     {
    //     //         bounds.Encapsulate(colliders[i].bounds);
    //     //     }
    //     //     return bounds;
    //     // }
    //     // else
    //     // {
    //     //     Debug.LogWarning($"No renderers found on {go.name} or its children");
    //     //     return new Bounds();
    //     // }


    // }

    // Bounds GetChildRendererBounds(GameObject go)
    // {
    //     Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

    //     if (debugLogging)
    //     {
    //         Debug.Log($"[GetChildRendererBounds] {go.name} - Found {renderers.Length} renderers");
    //     }

    //     if (renderers.Length > 0)
    //     {
    //         Bounds bounds = renderers[0].bounds;

    //         if (debugLogging)
    //         {
    //             Debug.Log($"  [0] {renderers[0].gameObject.name}: Center={bounds.center}, Size={bounds.size}, Extents={bounds.extents}");
    //         }

    //         for (int i = 1; i < renderers.Length; i++)
    //         {
    //             Debug.Log($"  Before encapsulating [{i}]: Center={bounds.center}, Size={bounds.size}");
    //             Debug.Log($"  [{i}] {renderers[i].gameObject.name}: Center={renderers[i].bounds.center}, Size={renderers[i].bounds.size}");

    //             bounds.Encapsulate(renderers[i].bounds);

    //             Debug.Log($"  After encapsulating [{i}]: Center={bounds.center}, Size={bounds.size}");
    //         }

    //         if (debugLogging)
    //         {
    //             Debug.Log($"[GetChildRendererBounds] FINAL {go.name}: Center={bounds.center}, Size={bounds.size}, Extents={bounds.extents}");
    //         }

    //         return bounds;
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"No renderers found on {go.name} or its children");
    //         return new Bounds();
    //     }
    // }

    Bounds GetChildRendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        if (debugLogging)
        {
            Debug.Log($"[GetChildRendererBounds] {go.name} - Found {renderers.Length} renderers");
        }

        // Filter out renderers with zero size (empty renderers)
        List<Renderer> validRenderers = new List<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.bounds.size.sqrMagnitude > 0.001f) // Skip near-zero renderers
            {
                validRenderers.Add(r);
            }
            else if (debugLogging)
            {
                Debug.Log($"  Skipping empty renderer: {r.gameObject.name}");
            }
        }

        if (validRenderers.Count > 0)
        {
            Bounds bounds = validRenderers[0].bounds;
            
            if (debugLogging)
            {
                Debug.Log($"  [0] {validRenderers[0].gameObject.name}: Center={bounds.center}, Size={bounds.size}");
            }
            
            for (int i = 1; i < validRenderers.Count; i++)
            {
                if (debugLogging)
                {
                    Debug.Log($"  Before encapsulating [{i}]: Center={bounds.center}, Size={bounds.size}");
                    Debug.Log($"  [{i}] {validRenderers[i].gameObject.name}: Center={validRenderers[i].bounds.center}, Size={validRenderers[i].bounds.size}");
                }

                bounds.Encapsulate(validRenderers[i].bounds);

                if (debugLogging)
                {
                    Debug.Log($"  After encapsulating [{i}]: Center={bounds.center}, Size={bounds.size}");
                }
            }
            
            if (debugLogging)
            {
                Debug.Log($"[GetChildRendererBounds] FINAL {go.name}: Center={bounds.center}, Size={bounds.size}");
            }

            return bounds;
        }
        else
        {
            Debug.LogWarning($"No valid renderers found on {go.name} or its children");
            return new Bounds();
        }
    }

    List<int> GetRandomOrder(int count)
    {
        List<int> possible = Enumerable.Range(1, count).ToList();
        Debug.Log("possible is: " + string.Join(", ", possible));
        List<int> listNumbers = new List<int>();

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, possible.Count);
            listNumbers.Add(possible[index]);
            possible.RemoveAt(index);
        }

        return listNumbers;
    }

    void CreateSecondFloor()
    {
        // TODO: Implement second floor generation
    }

    void CreateAttic()
    {
        // TODO: Implement attic generation
    }

    // Add this method to HouseGenerator class

    // void OnDrawGizmos()
    // {
    //     if (!Application.isPlaying || placedRooms == null)
    //         return;

    //     // Draw bounds for each placed room
    //     foreach (RoomInstance room in placedRooms)
    //     {
    //         // Draw the calculated bounds (collision detection box) in GREEN
    //         Gizmos.color = Color.green;
    //         DrawBoundsCube(room.bounds);

    //         // Draw the actual prefab bounds in RED
    //         if (room.gameObject != null)
    //         {
    //             Bounds actualBounds = GetGameObjectBounds(room.gameObject);
    //             Gizmos.color = Color.red;
    //             DrawBoundsCube(actualBounds);
    //         }

    //         // Draw center point in YELLOW
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawSphere(room.position, 0.2f);
    //     }
    // }

    // void DrawBoundsCube(Bounds bounds)
    // {
    //     Vector3 center = bounds.center;
    //     Vector3 extents = bounds.extents;

    //     // 8 corners of the box
    //     Vector3[] corners = new Vector3[8];
    //     corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
    //     corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
    //     corners[2] = center + new Vector3(extents.x, extents.y, -extents.z);
    //     corners[3] = center + new Vector3(-extents.x, extents.y, -extents.z);
    //     corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
    //     corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
    //     corners[6] = center + new Vector3(extents.x, extents.y, extents.z);
    //     corners[7] = center + new Vector3(-extents.x, extents.y, extents.z);

    //     // Draw 12 edges
    //     // Bottom face
    //     Gizmos.DrawLine(corners[0], corners[1]);
    //     Gizmos.DrawLine(corners[1], corners[2]);
    //     Gizmos.DrawLine(corners[2], corners[3]);
    //     Gizmos.DrawLine(corners[3], corners[0]);

    //     // Top face
    //     Gizmos.DrawLine(corners[4], corners[5]);
    //     Gizmos.DrawLine(corners[5], corners[6]);
    //     Gizmos.DrawLine(corners[6], corners[7]);
    //     Gizmos.DrawLine(corners[7], corners[4]);

    //     // Vertical edges
    //     Gizmos.DrawLine(corners[0], corners[4]);
    //     Gizmos.DrawLine(corners[1], corners[5]);
    //     Gizmos.DrawLine(corners[2], corners[6]);
    //     Gizmos.DrawLine(corners[3], corners[7]);
    // }

    // Bounds GetGameObjectBounds(GameObject go)
    // {
    //     Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
    //     if (renderers.Length == 0)
    //         return new Bounds();

    //     Bounds bounds = renderers[0].bounds;
    //     for (int i = 1; i < renderers.Length; i++)
    //     {
    //         bounds.Encapsulate(renderers[i].bounds);
    //     }
    //     return bounds;
    // }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || placedRooms == null)
            return;

        // Draw bounds for each placed room
        foreach (RoomInstance room in placedRooms)
        {
            // Draw the calculated bounds (collision detection box) in GREEN
            Gizmos.color = Color.green;
            DrawBoundsCube(room.bounds);

            // Draw the actual prefab bounds in RED
            if (room.gameObject != null)
            {
                Bounds actualBounds = GetGameObjectBounds(room.gameObject);
                Gizmos.color = Color.red;
                DrawBoundsCube(actualBounds);

                // Draw individual renderer bounds in CYAN
                // Renderer[] renderers = room.gameObject.GetComponentsInChildren<Renderer>();
                // Gizmos.color = Color.cyan;
                // foreach (Renderer renderer in renderers)
                // {
                //     DrawBoundsCube(renderer.bounds);
                // }

                Renderer[] renderers = room.gameObject.GetComponentsInChildren<Renderer>();
                Gizmos.color = Color.cyan;
                
                if (debugLogging)
                {
                    Debug.Log($"[OnDrawGizmos] {room.type.roomName} - Individual renderer bounds:");
                }
                
                foreach (Renderer renderer in renderers)
                {
                    if (debugLogging)
                    {
                        Debug.Log($"  {renderer.gameObject.name}: Center={renderer.bounds.center}, Size={renderer.bounds.size}");
                    }
                    DrawBoundsCube(renderer.bounds);
                }
            }

            // Draw center point in YELLOW
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(room.position, 0.2f);
        }
    }

    void DrawBoundsCube(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        // 8 corners of the box
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
        corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
        corners[2] = center + new Vector3(extents.x, extents.y, -extents.z);
        corners[3] = center + new Vector3(-extents.x, extents.y, -extents.z);
        corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
        corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
        corners[6] = center + new Vector3(extents.x, extents.y, extents.z);
        corners[7] = center + new Vector3(-extents.x, extents.y, extents.z);

        // Draw 12 edges
        // Bottom face
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);

        // Top face
        Gizmos.DrawLine(corners[4], corners[5]);
        Gizmos.DrawLine(corners[5], corners[6]);
        Gizmos.DrawLine(corners[6], corners[7]);
        Gizmos.DrawLine(corners[7], corners[4]);

        // Vertical edges
        Gizmos.DrawLine(corners[0], corners[4]);
        Gizmos.DrawLine(corners[1], corners[5]);
        Gizmos.DrawLine(corners[2], corners[6]);
        Gizmos.DrawLine(corners[3], corners[7]);
    }

    Bounds GetGameObjectBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds();

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }
}

//////debug end
///


// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;
// using NUnit.Framework;

// public class HouseGenerator : MonoBehaviour
// {
//     [Header("Config")]
//     public float basementHeight = 3f;
//     public float atticHeight = 2.5f;
//     public float floorHeight = 3f;
//     public float hallwayWidth = 10f;
//     public float roomSpacing = 0f;


//     [Header("Prefabs")]
//     public GameObject basementPrefab;
//     public GameObject atticPrefab;
//     public GameObject hallwayPrefab;
//     public GameObject doorPrefab;


//     [Header("Room Types")]
//     public RoomTypeSO foyer;
//     public RoomTypeSO kitchen;
//     public RoomTypeSO livingRoom;
//     public RoomTypeSO[] otherRooms;

//     [Header("Room Placement")]
//     // [Range(0f, 1f)]
//     public float branchOutProbability = 0.3f; // Probability to place room away from 


//     // Track all occupied space per floor using Bounds
//     //old
//     private bool isFourFloors;
//     private Vector2 foyerPosition = Vector2Int.zero;
//         //new
//     private Dictionary<int, List<Bounds>> occupiedBoundsPerFloor = new Dictionary<int, List<Bounds>>();
//     private List<RoomInstance> placedRooms = new List<RoomInstance>();


//     private class RoomInstance
//     {
//         public GameObject gameObject;
//         public RoomTypeSO type;
//         public Vector3 position; // World position (x, z on floor plane, y = height)
//         public Vector2 dimensions; // Width and depth
//         public int floorLevel;
//         public Bounds bounds; // For collision detection
//         public RoomLayout layout; // Store layout reference
//     }

//     void Start()
//     {
//         GenerateHouse();
//     }

//     void GenerateHouse()
//     {
//         occupiedBoundsPerFloor[0] = new List<Bounds>();
//         occupiedBoundsPerFloor[1] = new List<Bounds>();
//         occupiedBoundsPerFloor[2] = new List<Bounds>();
//         occupiedBoundsPerFloor[3] = new List<Bounds>();


//         // isFourFloors = Random.Range(0, 2) == 1;
//         isFourFloors = false;
//         if (isFourFloors)
//         {
//             Debug.Log("is 4 floors");
//         }
//         else
//         {
//             Debug.Log("is 3 floors");
//         }
//         CreateBasement();
//         CreateFirstFloor();


//         // if (isFourFloors)
//         // {
//         //     CreateSecondFloor();
//         // }

//         // CreateAttic();
//         // ConnectAllRooms();
//     }

//     void CreateBasement()
//     {
//         GameObject basement = Instantiate(basementPrefab, Vector3.down * basementHeight, Quaternion.identity);
//     }

//     void CreateFirstFloor()
//     {
//         //Place core
//         List<RoomTypeSO> coreRooms = new List<RoomTypeSO>();
//         coreRooms.Add(foyer);
//         coreRooms.Add(livingRoom);
//         coreRooms.Add(kitchen);

//         List<int> order = GetRandomOrder(3);
//         Debug.Log("order is" + string.Join(", ", order));
//         for (int i = 0; i < order.Count; i++)
//         {
//             bool isFirst = false;
//             if (i == 0)
//             {
//                 isFirst = true;
//             }
//             PlaceRoom(1, coreRooms[order[i] - 1], isFirst, isFourFloors);
//         }
//         Debug.Log($"Placed {placedRooms.Count} rooms on floor 1");

//         // //Check how man secondary rooms to place on first floor
//         // if (isFourFloors)
//         // {
//         //     //If we place multiple secondary rooms on the first floor
//         //     roomsToPlace = GetRandomOrder.Range(0, otherRooms.Count);
//         //     if (roomsToPlace >= 2)
//         //     {
//         //         List<int> secOrder = GetRandomOrder(roomsToPlace);

//         //     }
//         //     //If we place one secondary rooms on the first floor
//         //     else if (roomsToPlace == 1)
//         //     {

//         //     }
//         //     //If we place no secondary rooms on the first floor
//         //     return;
//         // }
//         // //There is no second floor
//         // else
//         // {

//         // }
//     }

//     void PlaceRoom(int floor, RoomTypeSO roomType, bool isFirst, bool needsStairs = false)
//     {
//         RoomLayout selectedLayout = SelectLayout(roomType, needsStairs);
//         if (selectedLayout == null)
//         {
//             Debug.LogError($"No suitable layout found for {roomType.roomName}");
//             return;
//         }

//         Bounds prefabBounds = GetChildRendererBounds(selectedLayout.prefab);
//         Vector2 roomDimensions = new Vector2(Mathf.Abs(prefabBounds.size.x), Mathf.Abs(prefabBounds.size.z));

//         Vector3 roomPosition;

//         if (isFirst)
//         {
//             // First room is always at origin on the flat plane
//             roomPosition = Vector3.zero;
//         }
//         else
//         {
//             // Find a valid position adjacent to existing rooms
//             roomPosition = FindAdjacentPosition(floor, roomDimensions);
//             if (roomPosition == Vector3.zero && placedRooms.Count > 0)
//             {
//                 Debug.LogWarning($"Could not find valid position for {roomType.roomName}");
//                 return;
//             }
//         }

//         // Create the room instance
//         RoomInstance roomInstance = new RoomInstance
//         {
//             type = roomType,
//             position = roomPosition,
//             dimensions = roomDimensions,
//             floorLevel = floor,
//             layout = selectedLayout
//         };

//         // Calculate world bounds (accounts for room spacing)
//         Bounds roomBounds = new Bounds(
//             roomPosition,
//             new Vector3(roomDimensions.x, floorHeight, roomDimensions.y)
//         );
//         roomInstance.bounds = roomBounds;

//         // Instantiate the room prefab
//         roomInstance.gameObject = Instantiate(
//             selectedLayout.prefab,
//             roomPosition + Vector3.up * (floor - 1) * floorHeight,
//             Quaternion.identity
//         );

//         // Add to tracking lists
//         placedRooms.Add(roomInstance);
//         occupiedBoundsPerFloor[floor].Add(roomBounds);

//         Debug.Log($"Placed {roomType.roomName} at {roomPosition} on floor {floor} with dimensions {roomDimensions}");

//         //Checked till here

//     }

//     RoomLayout SelectLayout(RoomTypeSO roomType, bool needsStairs)
//     {
//         if (roomType.layouts == null || roomType.layouts.Length == 0)
//         {
//             return null;
//         }

//         if (needsStairs)
//         {
//             RoomLayout[] stairLayouts = roomType.layouts.Where(l => l.hasStairs).ToArray();
//             if (stairLayouts.Length > 0)
//             {
//                 return stairLayouts[Random.Range(0, stairLayouts.Length)];
//             }
//             Debug.LogWarning($"{roomType.roomName} has no layouts with stairs, selecting random layout");
//         }

//         return roomType.layouts[Random.Range(0, roomType.layouts.Length)];
//     }

//     List<int> GetRandomOrder(int count)
//     {
//         List<int> possible = Enumerable.Range(1, count).ToList();
//         Debug.Log("possible is" + string.Join(", ", possible));
//         List<int> listNumbers = new List<int>();

//         for (int i = 0; i < count; i++)
//         {
//             int index = Random.Range(0, possible.Count);
//             listNumbers.Add(possible[index]);
//             possible.RemoveAt(index);
//         }

//         return listNumbers;
//     }

//     Bounds GetChildRendererBounds(GameObject go)
//     {
//         Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

//         if (renderers.Length > 0)
//         {
//             Bounds bounds = renderers[0].bounds;
//             for (int i = 1, ni = renderers.Length; i < ni; i++)
//             {
//                 bounds.Encapsulate(renderers[i].bounds);
//             }
//             return bounds;
//         }
//         else
//         {
//             return new Bounds();
//         }
//     }

//     private Vector3 FindAdjacentPosition(int floor, Vector2 roomDimensions)
//     {
//         if (placedRooms.Count == 0)
//         {
//             return Vector3.zero;
//         }

//         List<Vector3> candidatePositions = new List<Vector3>();

//         // Generate candidate positions adjacent to all placed rooms on this floor
//         foreach (RoomInstance existingRoom in placedRooms)
//         {
//             if (existingRoom.floorLevel != floor)
//                 continue;

//             // Try all four sides of the existing room
//             Vector3[] adjacentPositions = GetAdjacentPositions(existingRoom, roomDimensions);

//             foreach (Vector3 candidatePos in adjacentPositions)
//             {
//                 if (IsValidPosition(floor, candidatePos, roomDimensions))
//                 {
//                     candidatePositions.Add(candidatePos);
//                 }
//             }

//         }

//         if (candidatePositions.Count == 0)
//         {
//             Debug.LogWarning("No valid adjacent positions found for room");
//             return Vector3.zero;
//         }

//         // Decide whether to place compactly (near center) or branch out
//         Vector3 chosenPosition;
//         if (Random.value < branchOutProbability && candidatePositions.Count > 1)
//         {
//             // Branch out: pick a random position
//             chosenPosition = candidatePositions[Random.Range(0, candidatePositions.Count)];
//             Debug.Log("Room placed with branch-out strategy");
//         }
//         else
//         {
//             // Compact: pick position closest to center (0,0)
//             chosenPosition = candidatePositions.OrderBy(pos => new Vector2(pos.x, pos.z).sqrMagnitude).First();
//             Debug.Log("Room placed with compact strategy");
//         }

//         return chosenPosition;
//     }

//     private Vector3[] GetAdjacentPositions(RoomInstance existingRoom, Vector2 newRoomDimensions)
//     {
//         Vector3[] positions = new Vector3[4];
//         Vector3 existingPos = existingRoom.position;
//         float existingWidth = existingRoom.dimensions.x;
//         float existingDepth = existingRoom.dimensions.y;
//         float newWidth = newRoomDimensions.x;
//         float newDepth = newRoomDimensions.y;
//         float spacing = roomSpacing;

//         // Right side (+X direction)
//         positions[0] = existingPos + Vector3.right * (existingWidth / 2f + newWidth / 2f + spacing);
//         positions[0].z = existingPos.z; // Align Z

//         // Left side (-X direction)
//         positions[1] = existingPos - Vector3.right * (existingWidth / 2f + newWidth / 2f + spacing);
//         positions[1].z = existingPos.z; // Align Z

//         // Front side (+Z direction)
//         positions[2] = existingPos + Vector3.forward * (existingDepth / 2f + newDepth / 2f + spacing);
//         positions[2].x = existingPos.x; // Align X

//         // Back side (-Z direction)
//         positions[3] = existingPos - Vector3.forward * (existingDepth / 2f + newDepth / 2f + spacing);
//         positions[3].x = existingPos.x; // Align X

//         return positions;
//     }

//     private bool IsValidPosition(int floor, Vector3 position, Vector2 dimensions)
//     {
//         Bounds newBounds = new Bounds(
//             position,
//             new Vector3(dimensions.x, floorHeight, dimensions.y)
//         );

//         // // Expand bounds by spacing amount to check for spacing violations
//         // Vector3 expandSize = new Vector3(roomSpacing * 2f, 0, roomSpacing * 2f);
//         // newBounds.Expand(expandSize);

//         foreach (Bounds existingBounds in occupiedBoundsPerFloor[floor])
//         {
//             if (newBounds.Intersects(existingBounds))
//             {
//                 return false;
//             }
//         }

//         return true;
//     }

//     void CreateSecondFloor()
//     {

//     }

//     void CreateAttic()
//     {

//     }


// }

