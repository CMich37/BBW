using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HouseGenerator : MonoBehaviour
{
    [Header("Config")]
    public float basementHeight = 3f;
    public float atticHeight = 2.5f;
    public float floorHeight = 2.58f;
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
    public bool showSideDirections = true;

    [Header("Tracking")]
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
        public int rotationSteps; // NEW: 0=0°, 1=90°, 2=180°, 3=270° (Y axis, CW)
    }

    private int foyerListPos;
    private GameObject foyerStairs;

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

        isFourFloors = true;

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
        if (isFourFloors)
        {
            CreateSecondFloor();
        }
        
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
            if (coreRooms[order[i] - 1].roomName == "Foyer")
            {
                Debug.Log("foyer stairs being added as number" + i);
                foyerListPos = order[i] - 1;
                // return;
            }
            PlaceRoom(1, coreRooms[order[i] - 1], isFirst, isFourFloors, false, new Vector2());
        }
        Debug.Log($"Placed {placedRooms.Count} rooms on floor 1");

        // === SECONDARY ROOMS LOGIC ===
        // Check how many secondary rooms to place on first floor
        if (isFourFloors)
        {
            // If 4 floors, randomly decide how many secondary rooms to place on floor 1
            int numSecondaryRooms = Random.Range(0, otherRooms.Length); // 0 to otherRooms.Length - 1
            
            if (numSecondaryRooms == 0)
            {
                // Place no secondary rooms on first floor - save them for upper floors
                Debug.Log("Placing 0 secondary rooms on floor 1 (saving for upper floors)");
                return;
            }
            else if (numSecondaryRooms == 1)
            {
                // Place 1 secondary room - pick random one from otherRooms
                int randomIndex = Random.Range(0, otherRooms.Length);
                PlaceRoom(1, otherRooms[randomIndex], false, false, false, new Vector2());
                Debug.Log($"Placed 1 secondary room on floor 1: {otherRooms[randomIndex].roomName}");
            }
            else
            {
                // Place multiple secondary rooms - randomize which ones and their order
                List<int> secOrder = GetRandomOrder(numSecondaryRooms);
                Debug.Log($"Placing {numSecondaryRooms} secondary rooms on floor 1 in order: {string.Join(", ", secOrder)}");
                
                for (int i = 0; i < secOrder.Count; i++)
                {
                    int roomIndex = secOrder[i] - 1; // Convert to 0-based index
                    PlaceRoom(1, otherRooms[roomIndex], false, false, false, new Vector2());
                }
            }
        }
        else
        {
            // If only 3 floors, place ALL secondary rooms on first floor
            if (otherRooms.Length > 0)
            {
                List<int> secOrder = GetRandomOrder(otherRooms.Length);
                Debug.Log($"3 floors detected - Placing all {otherRooms.Length} secondary rooms on floor 1 in order: {string.Join(", ", secOrder)}");
                
                for (int i = 0; i < secOrder.Count; i++)
                {
                    int roomIndex = secOrder[i] - 1; // Convert to 0-based index
                    PlaceRoom(1, otherRooms[roomIndex], false, false, false, new Vector2());
                }
                
                Debug.Log($"Floor 1 complete with all secondary rooms. Total rooms: {placedRooms.Count}");
            }
            else
            {
                Debug.LogWarning("No secondary rooms available in otherRooms array");
            }
        }
    }

    void PlaceRoom(int floor, RoomTypeSO roomType, bool isFirst, bool needsStairs = false, bool overSomething = false, Vector2 somethingPosition = new Vector2())
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
        Vector2 roomDimensions = new Vector2(Mathf.Abs(prefabBounds.size.x), Mathf.Abs(prefabBounds.size.z));
        // Vector2 roomDimensions = new Vector2(Mathf.Abs(selectedLayout.dimensions.x), Mathf.Abs(selectedLayout.dimensions.y));

        if (debugLogging)
        {
            Debug.Log($"[PlaceRoom] {roomType.roomName} - Prefab bounds: {prefabBounds}, Dimensions: {roomDimensions}");
        }

        Vector3 roomPosition;

        if (isFirst && floor == 1)
        {
            // First room is always at origin on the flat plane
            roomPosition = Vector3.zero;
            if (debugLogging)
                Debug.Log($"[PlaceRoom] {roomType.roomName} - First room, placing at origin");
        }
        else if (isFirst && floor == 2)
        {
            // First room is always at origin on the flat plane
            float foyerFlushOffset = roomDimensions.x / 2;
            roomPosition = new Vector3(somethingPosition.x - (5+foyerFlushOffset), 0, somethingPosition.y);
            if (debugLogging)
                Debug.Log($"[PlaceRoom] {roomType.roomName} - Placing over foyer");
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
        if (!isFourFloors)
        {
            return;
        }

        List<RoomTypeSO> remainingRooms = GetRemainingSecondaryRooms();

        List<int> order = GetRandomOrder(remainingRooms.Count);

        RoomInstance firstFloorFoyer = placedRooms.Find(r => r.floorLevel == 1 && r.type == foyer);

        if (firstFloorFoyer == null)
        {
            Debug.LogError("Could not find foyer on first floor!");
            return;
        }

        //stair update
        Vector2 foyerPos = new Vector2(firstFloorFoyer.position.x, firstFloorFoyer.position.z);
        print(foyerListPos);

        // foyerStairs = placedRooms[foyerListPos].gameObject.transform.Find("stairs").gameObject;
        Transform stairsTransform = firstFloorFoyer.gameObject.transform.Find("stairs");
        if (stairsTransform == null)
            {
                Debug.LogError("No 'stairs' child found on foyer prefab!");
                return;
            }
        foyerStairs = stairsTransform.gameObject;

        Bounds stairBounds = GetChildRendererBounds(foyerStairs);
        occupiedBoundsPerFloor[2].Add(stairBounds);
        //

        for (int i = 0; i < order.Count; i++)
        {
            bool isFirst = (i == 0);
            if (isFirst)
            {
                PlaceRoom(2, remainingRooms[order[i] - 1], isFirst, isFourFloors, true, foyerPos);

            }
            else
            {
                PlaceRoom(2, remainingRooms[order[i] - 1], isFirst, isFourFloors, true, new Vector2());
            }
        }

        return;
    }

    List<RoomTypeSO> GetRemainingSecondaryRooms()
    {
        List<RoomTypeSO> remaining = new List<RoomTypeSO>();
        
        // Get all secondary rooms that have been placed on floor 1
        List<RoomTypeSO> placedSecondaryRooms = placedRooms
            .Where(r => r.floorLevel == 1 && otherRooms.Contains(r.type))
            .Select(r => r.type)
            .ToList();
        
        // Find which secondary rooms haven't been placed yet
        foreach (RoomTypeSO room in otherRooms)
        {
            if (!placedSecondaryRooms.Contains(room))
            {
                remaining.Add(room);
            }
        }
        
        return remaining;
    }


    //Rotations
    // Given a LOCAL side index and how many 90-degree CW steps the room was rotated,
    // returns the WORLD side index that local side now points toward.
    // Side indices: 0=+X(Right), 1=-X(Left), 2=+Z(Front), 3=-Z(Back)
    // CW rotation maps: +X→+Z→-X→-Z→+X  which in indices is: 0→2→1→3→0
    // The lookup table encodes: worldSide = rotationMap[rotSteps][localSide]
    private static readonly int[,] localToWorld = new int[4, 4]
    {
        { 0, 1, 2, 3 }, // 0°:   local == world
        { 3, 2, 0, 1 }, // 90°:  local+X(0)→world-Z(3), local-X(1)→world+Z(2), local+Z(2)→world+X(0), local-Z(3)→world-X(1)
        { 1, 0, 3, 2 }, // 180°: local+X(0)→world-X(1), local-X(1)→world+X(0), local+Z(2)→world-Z(3), local-Z(3)→world+Z(2)
        { 2, 3, 1, 0 }, // 270°: local+X(0)→world+Z(2), local-X(1)→world-Z(3), local+Z(2)→world-X(1), local-Z(3)→world+X(0)
    };

    // Inverse: given a WORLD side and rotation steps, returns the LOCAL side index to check in walkableSides[]
    private int WorldSideToLocal(int worldSide, int rotSteps)
    {
        for (int local = 0; local < 4; local++)
            if (localToWorld[rotSteps, local] == worldSide) return local;
        return worldSide; // fallback (should never reach)
    }

    // Is a specific WORLD-SPACE side of a placed room walkable?
    private bool IsWorldSideWalkable(RoomInstance room, int worldSideIndex)
    {
        if (room.layout == null || room.layout.walkableSides == null) return true;
        int localSide = WorldSideToLocal(worldSideIndex, room.rotationSteps);
        return room.layout.walkableSides[localSide];
    }
    ///////////

    void CreateAttic()
    {
        // TODO: Implement attic generation
    }

///Debugging

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

    //             // Draw individual renderer bounds in CYAN

    //             Renderer[] renderers = room.gameObject.GetComponentsInChildren<Renderer>();
    //             Gizmos.color = Color.cyan;
                
    //             if (debugLogging)
    //             {
    //                 Debug.Log($"[OnDrawGizmos] {room.type.roomName} - Individual renderer bounds:");
    //             }
                
    //             foreach (Renderer renderer in renderers)
    //             {
    //                 if (debugLogging)
    //                 {
    //                     Debug.Log($"  {renderer.gameObject.name}: Center={renderer.bounds.center}, Size={renderer.bounds.size}");
    //                 }
    //                 DrawBoundsCube(renderer.bounds);
    //             }
    //         }

    //         // Draw center point in YELLOW
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawSphere(room.position, 0.2f);
    //     }
    // }

    void OnDrawGizmos()
    {
    if (!Application.isPlaying || placedRooms == null)
        return;

    // Draw bounds for each placed room
    foreach (RoomInstance room in placedRooms)
    {
        // Draw the calculated bounds (collision detection box) in GREEN
        // Gizmos.color = Color.green;
        // DrawBoundsCube(room.bounds);

        // // Draw the actual prefab bounds in RED
        // if (room.gameObject != null)
        // {
        //     Bounds actualBounds = GetGameObjectBounds(room.gameObject);
        //     Gizmos.color = Color.red;
        //     DrawBoundsCube(actualBounds);

        //     // Draw individual renderer bounds in CYAN
        //     Renderer[] renderers = room.gameObject.GetComponentsInChildren<Renderer>();
        //     Gizmos.color = Color.cyan;
            
        //     if (debugLogging)
        //     {
        //         Debug.Log($"[OnDrawGizmos] {room.type.roomName} - Individual renderer bounds:");
        //     }
            
        //     foreach (Renderer renderer in renderers)
        //     {
        //         if (debugLogging)
        //         {
        //             Debug.Log($"  {renderer.gameObject.name}: Center={renderer.bounds.center}, Size={renderer.bounds.size}");
        //         }
        //         DrawBoundsCube(renderer.bounds);
        //     }
        // }

        // Draw center point in YELLOW
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(room.position, 0.2f);

        // ========== NEW: SIDE DIRECTION RAYS ==========
        if (showSideDirections)
        {
            Vector3 center = room.position;
            // Half extents of the room (width/2, depth/2)
            float halfWidth = room.dimensions.x * 0.5f;
            float halfDepth = room.dimensions.y * 0.5f;
            float rayLength = 2.0f; // Length of the ray beyond the wall

            // +X (Right) - Red
            Gizmos.color = Color.red;
            Vector3 startXPos = center + Vector3.right * halfWidth;
            Gizmos.DrawLine(startXPos, startXPos + Vector3.right * rayLength);

            // -X (Left) - Green
            Gizmos.color = Color.green;
            Vector3 startXNeg = center + Vector3.left * halfWidth;
            Gizmos.DrawLine(startXNeg, startXNeg + Vector3.left * rayLength);

            // +Z (Front) - Blue
            Gizmos.color = Color.blue;
            Vector3 startZPos = center + Vector3.forward * halfDepth;
            Gizmos.DrawLine(startZPos, startZPos + Vector3.forward * rayLength);

            // -Z (Back) - Yellow (or magenta for better contrast)
            Gizmos.color = Color.yellow;
            Vector3 startZNeg = center + Vector3.back * halfDepth;
            Gizmos.DrawLine(startZNeg, startZNeg + Vector3.back * rayLength);
        }
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
