using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using NUnit.Framework;

public class HouseGenerator : MonoBehaviour
{
    [Header("Config")]
    public float basementHeight = 3f;
    public float atticHeight = 2.5f;
    public float floorHeight = 3f;
    public float hallwayWidth = 10f;
    public float roomSpacing = 5f;

    public float tileSize = 2.5f;

    private Dictionary<int, List<Bounds>> occupiedBoundsPerFloor = new Dictionary<int, List<Bounds>>();


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

    private List<RoomInstance> placedRooms = new List<RoomInstance>();
    private bool isFourFloors;
    private GameObject currentFloorParent;
    private Vector2Int currentGridPosition = Vector2Int.zero;
    private HashSet<Vector2Int> occupiedGridPositions = new HashSet<Vector2Int>();
    private Vector2 foyerPosition = Vector2Int.zero;


    private class RoomInstance
    {
        public GameObject gameObject;
        public RoomTypeSO type;
        public Vector2Int gridPosition;
        public Vector2Int dimensions;
        public int floorLevel;
    }

    void Start()
    {
        GenerateHouse();
    }

    void GenerateHouse()
    {
        // isFourFloors = Random.Range(0, 2) == 1;
        isFourFloors = true;
        Debug.Log("is4f" + isFourFloors);
        CreateBasement();
        CreateFirstFloor();
        
        if (isFourFloors)
        {
            CreateSecondFloor();
        }
        
        CreateAttic();
        ConnectAllRooms();
    }

    void CreateBasement()
    {
        GameObject basement = Instantiate(basementPrefab, Vector3.down * basementHeight, Quaternion.identity);
    }

    void CreateFirstFloor()
    {
        currentFloorParent = new GameObject("First Floor");
        currentFloorParent.transform.position = Vector3.zero;
        // currentGridPosition = Vector2Int.zero; //what does it do

        List<RoomTypeSO> mandatoryRooms = new List<RoomTypeSO> { foyer, kitchen, livingRoom };

        // 2. Keep placing rooms until the list is empty
        while (mandatoryRooms.Count > 0)
        {
            // Pick a random room from the remaining list
            int randomIndex = Random.Range(0, mandatoryRooms.Count);
            RoomTypeSO room = mandatoryRooms[randomIndex];

            // Place it (pass `isFourFloors` only for the foyer)
            PlaceRoom(room, room == foyer ? isFourFloors : false);
            // Debug.Log("placed" + room);

            // Remove it from the list so it doesn't get picked again
            mandatoryRooms.RemoveAt(randomIndex);
        }
        // Debug.Log("foypos == "+foyerPosition);

        // Place optional rooms
        int roomsToPlace = isFourFloors ? Mathf.Max(0, otherRooms.Length - 1) : otherRooms.Length;
        roomsToPlace = Mathf.Min(roomsToPlace, otherRooms.Length); // Safety check
        
        for (int i = 0; i < roomsToPlace; i++)
        {
            PlaceRoom(otherRooms[i]);
        }

        AddBasementEntrance();
    }

    // void PlaceRoom(RoomTypeSO roomType, bool needsStairs = false)
    // {
    //     RoomLayout layout = GetRandomLayout(roomType, needsStairs);

    //     // Calculate prefab bounds
    //     Bounds visualBounds = CalculateRoomBounds(layout.prefab);
    //     Vector3 size = visualBounds.size;

    //     // Convert to grid units
    //     int widthInTiles = Mathf.CeilToInt(visualBounds.size.x / tileSize);
    //     int depthInTiles = Mathf.CeilToInt(visualBounds.size.z / tileSize);
    //     Vector2Int calculatedSize = new Vector2Int(widthInTiles, depthInTiles);

    //     // Get a valid grid position
    //     Vector2Int position = CalculateNextRoomPosition(calculatedSize);

    //     // Instantiate the prefab
    //     GameObject roomObj = Instantiate(
    //         layout.prefab,
    //         new Vector3(position.x * tileSize, currentFloorParent.transform.position.y, position.y * tileSize),
    //         Quaternion.identity,
    //         currentFloorParent.transform
    //     );

    //     // Reserve occupied tiles
    //     for (int x = 0; x < calculatedSize.x; x++)
    //     {
    //         for (int y = 0; y < calculatedSize.y; y++)
    //         {
    //             occupiedGridPositions.Add(position + new Vector2Int(x, y));
    //         }
    //     }

    //     // Register placed room
    //     placedRooms.Add(new RoomInstance {
    //         gameObject = roomObj,
    //         type = roomType,
    //         gridPosition = position,
    //         dimensions = calculatedSize,
    //         floorLevel = isFourFloors ? (currentFloorParent.name == "First Floor" ? 0 : 1) : 0
    //     });

    //     // Log for debugging
    //     if (roomType.roomName == "Foyer")
    //     {
    //         foyerPosition = position;
    //     }
    //     Debug.Log($"Placing {roomType.roomName} at {position} | Grid size: {calculatedSize} | Visual bounds: {visualBounds.size}");
    // }
    void PlaceRoom(RoomTypeSO roomType, bool needsStairs = false)
    {
        RoomLayout layout = GetRandomLayout(roomType, needsStairs);
        Bounds bounds = CalculateRoomBounds(layout.prefab);
        Vector3 rawSize = bounds.size;

        float snappedWidth = Mathf.Round(rawSize.x / tileSize) * tileSize;
        float snappedDepth = Mathf.Round(rawSize.z / tileSize) * tileSize;

        Vector3 size = new Vector3(snappedWidth, rawSize.y, snappedDepth);

        Vector3 centerOffset = bounds.center - layout.prefab.transform.position;


        int width = Mathf.CeilToInt(size.x / tileSize);
        int depth = Mathf.CeilToInt(size.z / tileSize);
        Vector2Int gridSize = new Vector2Int(width, depth);

        Vector2Int gridPos = CalculateNextRoomPosition(gridSize);

        Vector3 worldPosition = new Vector3(
            gridPos.x * tileSize,
            currentFloorParent.transform.position.y,
            gridPos.y * tileSize
        );

        // Center room based on visual bounds
        Vector3 finalPosition = worldPosition - new Vector3(centerOffset.x, 0f, centerOffset.z);

        GameObject roomObj = Instantiate(
            layout.prefab,
            finalPosition,
            Quaternion.identity,
            currentFloorParent.transform
        );


        // Register each tile as occupied based on world-space
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                occupiedGridPositions.Add(new Vector2Int(gridPos.x + x, gridPos.y + z));
            }
        }


        // Register room
        placedRooms.Add(new RoomInstance
        {
            gameObject = roomObj,
            type = roomType,
            gridPosition = gridPos,
            dimensions = gridSize,
            floorLevel = isFourFloors && currentFloorParent.name == "Second Floor" ? 1 : 0
        });

        if (roomType.roomName == "Foyer")
        {
            foyerPosition = gridPos;
        }

        Debug.Log($"Placed {roomType.roomName} at grid ({gridPos}) → world ({worldPosition}) | Size = {width}x{depth}");
    }


    Vector3 GetRoomWorldSize(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Vector3(0f, 0f, 0f); // default size

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.size;
    }

    Vector2Int CalculateNextRoomPosition(Vector2Int dimensions)
    {
        List<Vector2Int> candidatePositions = new List<Vector2Int>();

        if (placedRooms.Count == 0)
        {
            return Vector2Int.zero;
        }

        int currentFloorLevel = isFourFloors && currentFloorParent.name == "Second Floor" ? 1 : 0;

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (RoomInstance room in placedRooms)
        {
            if (room.floorLevel != currentFloorLevel)
                continue;

            Vector2Int basePos = room.gridPosition;
            Vector2Int roomSize = room.dimensions;

            foreach (Vector2Int dir in directions)
            {
                // Calculate candidate position based on direction
                Vector2Int offset = dir.x != 0
                    ? new Vector2Int(roomSize.x, 0)
                    : new Vector2Int(0, roomSize.y);

                // Add 1 tile of spacing in that direction
                offset += dir * Vector2Int.zero; // only one tile gap
;

                Vector2Int candidate = basePos + offset;

                // Check each tile the new room would occupy
                bool overlap = false;
                for (int x = 0; x < dimensions.x; x++)
                {
                    for (int y = 0; y < dimensions.y; y++)
                    {
                        Vector2Int checkTile = candidate + new Vector2Int(x, y);
                        if (occupiedGridPositions.Contains(checkTile))
                        {
                            overlap = true;
                            break;
                        }
                    }
                    if (overlap) break;
                }

                if (!overlap && !candidatePositions.Contains(candidate))
                {
                    candidatePositions.Add(candidate);
                }
            }
        }

        if (candidatePositions.Count == 0)
        {
            Debug.LogWarning("No valid room positions. Placing randomly.");
            return new Vector2Int(Random.Range(-5, 5), Random.Range(-5, 5));
        }

        return candidatePositions[Random.Range(0, candidatePositions.Count)];
    }


    // void CreateSecondFloor()
    // {
    //     currentFloorParent = new GameObject("Second Floor");
    //     currentFloorParent.transform.position = Vector3.up * floorHeight;

    //     // Calculate how many rooms should be on second floor
    //     int roomsOnFirstFloor = placedRooms.Count(r => r.floorLevel == 0);
    //     int roomsForSecondFloor = otherRooms.Length - (roomsOnFirstFloor - 3); // minus mandatory rooms

    //     // Create central hallway
    //     float hallwayLength = (roomsForSecondFloor + 1) * roomSpacing;
    //     GameObject hallway = Instantiate(
    //         hallwayPrefab,
    //         foyerPosition,
    //         Quaternion.identity,
    //         currentFloorParent.transform
    //     );
    //     hallway.transform.localScale = new Vector3(hallwayWidth, 1, hallwayLength);

    //     // Place rooms along the hallway
    //     for (int i = 0; i < roomsForSecondFloor; i++)
    //     {
    //         RoomTypeSO roomType = otherRooms[roomsOnFirstFloor - 3 + i]; // Skip mandatory rooms
    //         RoomLayout layout = GetRandomLayout(roomType, false);

    //         bool leftSide = i % 2 == 0;
    //         float zPos = i * roomSpacing + roomSpacing/2f;

    //         Vector3 position = new Vector3(
    //             leftSide ? -(hallwayWidth/2 + layout.dimensions.x/2) : (hallwayWidth/2 + layout.dimensions.x/2),
    //             floorHeight,
    //             zPos
    //         );

    //         GameObject roomObj = Instantiate(
    //             layout.prefab,
    //             position,
    //             Quaternion.identity,
    //             currentFloorParent.transform
    //         );

    //         placedRooms.Add(new RoomInstance {
    //             gameObject = roomObj,
    //             type = roomType,
    //             gridPosition = new Vector2Int((int)position.x, (int)position.z),
    //             dimensions = layout.dimensions,
    //             floorLevel = 1
    //         });
    //     }
    // }
    
    void CreateSecondFloor()
    {
        currentFloorParent = new GameObject("Second Floor");
        currentFloorParent.transform.position = Vector3.up * floorHeight;

        // Determine rooms for the second floor
        List<RoomTypeSO> secondFloorRooms = new List<RoomTypeSO>();
        int roomsOnFirstFloor = placedRooms.Count(r => r.floorLevel == 0);
        int roomsForSecondFloor = otherRooms.Length - (roomsOnFirstFloor - 3); // Assuming 3 mandatory rooms

        for (int i = 0; i < roomsForSecondFloor; i++)
        {
            int index = roomsOnFirstFloor - 3 + i;
            if (index < otherRooms.Length)
                secondFloorRooms.Add(otherRooms[index]);
        }

        // Collect layouts, depths, and widths
        List<RoomLayout> layouts = new List<RoomLayout>();
        List<float> depths = new List<float>();
        List<float> widths = new List<float>();
        List<bool> isLeftList = new List<bool>();

        float leftTotalDepth = 0f;
        float rightTotalDepth = 0f;

        foreach (RoomTypeSO roomType in secondFloorRooms)
        {
            RoomLayout layout = GetRandomLayout(roomType, false);
            Bounds bounds = CalculateRoomBounds(layout.prefab);
            float depth = bounds.size.z;
            float width = bounds.size.x;

            layouts.Add(layout);
            depths.Add(depth);
            widths.Add(width);

            // Alternate starting with left
            bool isLeft = (layouts.Count % 2 == 1);
            isLeftList.Add(isLeft);

            if (isLeft)
                leftTotalDepth += depth;
            else
                rightTotalDepth += depth;
        }

        // Calculate hallway length based on the larger side
        float hallwayLength = Mathf.Max(leftTotalDepth, rightTotalDepth);

        // Create the hallway centered above the foyer's position
        GameObject hallway = Instantiate(
            hallwayPrefab,
            new Vector3(foyerPosition.x * tileSize, floorHeight, foyerPosition.y * tileSize + hallwayLength / 2),
            Quaternion.identity,
            currentFloorParent.transform
        );
        hallway.transform.localScale = new Vector3(hallwayWidth, 1, hallwayLength);

        // Place rooms along the hallway
        float currentLeftZ = 0f;
        float currentRightZ = 0f;

        for (int i = 0; i < layouts.Count; i++)
        {
            RoomLayout layout = layouts[i];
            float depth = depths[i];
            float width = widths[i];
            bool isLeft = isLeftList[i];

            Vector3 position;

            if (isLeft)
            {
                position = new Vector3(
                    (foyerPosition.x * tileSize) - (hallwayWidth / 2 + width / 2),
                    floorHeight,
                    (foyerPosition.y * tileSize) + currentLeftZ + depth / 2
                );
                currentLeftZ += depth;
            }
            else
            {
                position = new Vector3(
                    (foyerPosition.x * tileSize) + (hallwayWidth / 2 + width / 2),
                    floorHeight,
                    (foyerPosition.y * tileSize) + currentRightZ + depth / 2
                );
                currentRightZ += depth;
            }

            GameObject roomObj = Instantiate(
                layout.prefab,
                position,
                Quaternion.identity,
                currentFloorParent.transform
            );

            placedRooms.Add(new RoomInstance
            {
                gameObject = roomObj,
                type = secondFloorRooms[i],
                gridPosition = new Vector2Int(
                    Mathf.FloorToInt(position.x / tileSize),
                    Mathf.FloorToInt(position.z / tileSize)
                ),
                dimensions = new Vector2Int(
                    Mathf.CeilToInt(width / tileSize),
                    Mathf.CeilToInt(depth / tileSize)
                ),
                floorLevel = 1
            });
        }
    }

    void CreateAttic()
    {
        // Find bounds of top floor
        string topFloorName = isFourFloors ? "Second Floor" : "First Floor";
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        
        foreach (var room in placedRooms)
        {
            if (room.gameObject.transform.parent.name == topFloorName)
            {
                var roomBounds = CalculateRoomBounds(room.gameObject);
                if (!hasBounds)
                {
                    bounds = roomBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(roomBounds);
                }
            }
        }
        
        // Create attic
        float atticY = isFourFloors ? floorHeight * 2 + atticHeight/2 : floorHeight + atticHeight/2;
        GameObject attic = Instantiate(
            atticPrefab,
            new Vector3(bounds.center.x, atticY, bounds.center.z),
            Quaternion.identity
        );
        attic.transform.localScale = new Vector3(bounds.size.x, atticHeight, bounds.size.z);
        
        AddAtticEntrance();
    }

    void ConnectAllRooms()
    {
        // Connect rooms on each floor separately
        for (int floor = 0; floor < (isFourFloors ? 2 : 1); floor++)
        {
            List<RoomInstance> floorRooms = placedRooms.FindAll(r => r.floorLevel == floor);
            
            for (int i = 0; i < floorRooms.Count; i++)
            {
                for (int j = i + 1; j < floorRooms.Count; j++)
                {
                    if (AreRoomsAdjacent(floorRooms[i], floorRooms[j]))
                    {
                        CreateDoorBetweenRooms(floorRooms[i], floorRooms[j]);
                    }
                }
            }
        }
    }

    bool AreRoomsAdjacent(RoomInstance room1, RoomInstance room2)
    {
        Bounds bounds1 = CalculateRoomBounds(room1.gameObject);
        Bounds bounds2 = CalculateRoomBounds(room2.gameObject);
        
        // Expand bounds slightly to detect adjacency
        bounds1.Expand(0.1f);
        bounds2.Expand(0.1f);
        
        // Check if bounds intersect in 2D (XZ plane)
        return bounds1.Intersects(bounds2);
    }

    void CreateDoorBetweenRooms(RoomInstance room1, RoomInstance room2)
    {
        Bounds bounds1 = CalculateRoomBounds(room1.gameObject);
        Bounds bounds2 = CalculateRoomBounds(room2.gameObject);
        
        // Find the overlapping edge
        Vector3 doorPosition = (bounds1.center + bounds2.center) / 2f;
        
        // Adjust height based on floor level
        doorPosition.y = room1.floorLevel * floorHeight + 1f; // 1m above floor
        
        Instantiate(doorPrefab, doorPosition, Quaternion.identity);
    }

    RoomLayout GetRandomLayout(RoomTypeSO roomType, bool needsStairs)
    {
        // Debug.Log(roomType + " " + needsStairs);
        if (needsStairs)
        {
            // Filter for layouts with stairs if needed (for foyer)
            List<RoomLayout> validLayouts = new List<RoomLayout>();
            foreach (var layout in roomType.layouts)
            {
                if (layout.hasStairs)
                    validLayouts.Add(layout);
            }

            if (validLayouts.Count == 0)
            {
                Debug.LogWarning($"No stair layouts found for {roomType.roomName}");
                return roomType.layouts[0]; // Fallback
            }

            return validLayouts[Random.Range(0, validLayouts.Count)];
        }
        else
        {
            // Filter for layouts with stairs if needed (for foyer)
            List<RoomLayout> validLayouts = new List<RoomLayout>();
            foreach (var layout in roomType.layouts)
            {
                if (!layout.hasStairs)
                    validLayouts.Add(layout);
            }

            if (validLayouts.Count == 0)
            {
                Debug.LogWarning($"No stair layouts found for {roomType.roomName}");
                return roomType.layouts[0]; // Fallback
            }

            return validLayouts[Random.Range(0, validLayouts.Count)];
        }
        
    }

    Bounds CalculateRoomBounds(GameObject room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) 
        {
            Debug.LogWarning($"Room {room.name} has no renderers, using default bounds");
            return new Bounds(room.transform.position, Vector3.one * 5f);
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    void AddBasementEntrance()
    {
        if (placedRooms.Count == 0 || doorPrefab == null) 
        {
            Debug.LogWarning("Cannot add basement entrance - no rooms or door prefab");
            return;
        }
        
        // Find all first-floor rooms
        List<RoomInstance> firstFloorRooms = placedRooms.FindAll(
            r => r.gameObject.transform.parent.name == "First Floor"
        );
        
        if (firstFloorRooms.Count == 0) return;
        
        // Select random first-floor room (excluding foyer if desired)
        RoomInstance selectedRoom = firstFloorRooms[Random.Range(0, firstFloorRooms.Count)];
        
        // Place door at the room's position (adjust as needed)
        Vector3 doorPos = selectedRoom.gameObject.transform.position;
        doorPos.y = 0; // Align with floor level
        
        Instantiate(doorPrefab, doorPos, Quaternion.identity, selectedRoom.gameObject.transform);
    }

    void AddAtticEntrance()
    {
        if (placedRooms.Count == 0 || doorPrefab == null) 
        {
            Debug.LogWarning("Cannot add attic entrance - no rooms or door prefab");
            return;
        }
        
        // Find all top-floor rooms
        string topFloorName = isFourFloors ? "Second Floor" : "First Floor";
        List<RoomInstance> topFloorRooms = placedRooms.FindAll(
            r => r.gameObject.transform.parent.name == topFloorName
        );
        
        if (topFloorRooms.Count == 0) return;
        
        // Select random top-floor room
        RoomInstance selectedRoom = topFloorRooms[Random.Range(0, topFloorRooms.Count)];
        
        // Place door at the room's position (adjust as needed)
        Vector3 doorPos = selectedRoom.gameObject.transform.position;
        doorPos.y = isFourFloors ? floorHeight * 2 : floorHeight; // Top floor height
        
        Instantiate(doorPrefab, doorPos, Quaternion.identity, selectedRoom.gameObject.transform);
    }

    void DrawPrefabBounds(RoomInstance room)
    {
        if (room?.gameObject == null) return;

        Bounds bounds = CalculateRoomBounds(room.gameObject);

        // Draw visual bounds as a wireframe cube
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

    #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(bounds.center + Vector3.up * 0.5f, 
            $"{room.type.roomName}\nCenter: {bounds.center}\nSize: {bounds.size}");
    #endif
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        foreach (var room in placedRooms)
        {
            DrawPrefabBounds(room);
        }


        Gizmos.color = Color.yellow;
        foreach (var tile in occupiedGridPositions)
        {
            Vector3 pos = new Vector3(tile.x * tileSize, 0.05f, tile.y * tileSize);
            Gizmos.DrawCube(pos, new Vector3(tileSize * 0.95f, 0.1f, tileSize * 0.95f));
        }

        Gizmos.color = Color.gray;
        int gridSize = 20;
        for (int x = -gridSize; x <= gridSize; x++)
        {
            Gizmos.DrawLine(
                new Vector3(x * tileSize, 0, -gridSize * tileSize),
                new Vector3(x * tileSize, 0, gridSize * tileSize)
            );
        }
        for (int z = -gridSize; z <= gridSize; z++)
        {
            Gizmos.DrawLine(
                new Vector3(-gridSize * tileSize, 0, z * tileSize),
                new Vector3(gridSize * tileSize, 0, z * tileSize)
            );
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-50, 0, 0), new Vector3(50, 0, 0));
        Gizmos.DrawLine(new Vector3(-50, floorHeight, 0), new Vector3(50, floorHeight, 0));
        if (isFourFloors)
        {
            Gizmos.DrawLine(new Vector3(-50, floorHeight * 2, 0), new Vector3(50, floorHeight * 2, 0));
        }
    }

}