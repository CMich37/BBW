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
    private Vector3 foyerPosition = Vector3Int.zero;


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
        isFourFloors = Random.Range(0, 2) == 1;
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
            Debug.Log("placed" + room);

            // Remove it from the list so it doesn't get picked again
            mandatoryRooms.RemoveAt(randomIndex);
        }

        // Place optional rooms
        int roomsToPlace = isFourFloors ? Mathf.Max(0, otherRooms.Length - 1) : otherRooms.Length;
        roomsToPlace = Mathf.Min(roomsToPlace, otherRooms.Length); // Safety check
        
        for (int i = 0; i < roomsToPlace; i++)
        {
            PlaceRoom(otherRooms[i]);
        }

        AddBasementEntrance();
    }

    void PlaceRoom(RoomTypeSO roomType, bool needsStairs = false)
    {
        RoomLayout layout = GetRandomLayout(roomType, needsStairs);

        // Calculate prefab bounds
        Bounds visualBounds = CalculateRoomBounds(layout.prefab);
        Vector3 size = visualBounds.size;

        // Convert to grid units
        int widthInTiles = Mathf.CeilToInt(size.x / roomSpacing);
        int depthInTiles = Mathf.CeilToInt(size.z / roomSpacing);
        Vector2Int calculatedSize = new Vector2Int(widthInTiles, depthInTiles);

        // Get a valid grid position
        Vector2Int position = CalculateNextRoomPosition(calculatedSize);

        // Instantiate the prefab
        GameObject roomObj = Instantiate(
            layout.prefab,
            new Vector3(position.x * roomSpacing, currentFloorParent.transform.position.y, position.y * roomSpacing),
            Quaternion.identity,
            currentFloorParent.transform
        );

        // Reserve occupied tiles
        for (int x = 0; x < calculatedSize.x; x++)
        {
            for (int y = 0; y < calculatedSize.y; y++)
            {
                occupiedGridPositions.Add(position + new Vector2Int(x, y));
            }
        }

        // Register placed room
        placedRooms.Add(new RoomInstance {
            gameObject = roomObj,
            type = roomType,
            gridPosition = position,
            dimensions = calculatedSize,
            floorLevel = isFourFloors ? (currentFloorParent.name == "First Floor" ? 0 : 1) : 0
        });

        // Log for debugging
        Debug.Log($"Placing {roomType.roomName} at {position} | Grid size: {calculatedSize} | Visual bounds: {visualBounds.size}");
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
                // Apply spacing offset
                Vector2Int candidate = basePos + dir * (roomSize + Vector2Int.one);

                // Check each tile the new room would occupy
                bool overlap = false;
                for (int x = 0; x < dimensions.x; x++)
                {
                    for (int y = 0; y < dimensions.y; y++)
                    {
                        Vector2Int checkTile = candidate + new Vector2Int(x, y);
                        if (occupiedGridPositions.Contains(checkTile))
                        {
                            Debug.LogWarning($"Tile {checkTile} is already occupied. Cannot place room of size {dimensions} at {candidate}.");
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
            Debug.LogWarning("No valid positions. Defaulting.");
            return new Vector2Int(Random.Range(-10, 10), Random.Range(-10, 10));
        }

        return candidatePositions[Random.Range(0, candidatePositions.Count)];
    }

    void CreateSecondFloor()
    {
        currentFloorParent = new GameObject("Second Floor");
        currentFloorParent.transform.position = Vector3.up * floorHeight;

        // Calculate how many rooms should be on second floor
        int roomsOnFirstFloor = placedRooms.Count(r => r.floorLevel == 0);
        int roomsForSecondFloor = otherRooms.Length - (roomsOnFirstFloor - 3); // minus mandatory rooms

        // Create central hallway
        float hallwayLength = (roomsForSecondFloor + 1) * roomSpacing;
        GameObject hallway = Instantiate(
            hallwayPrefab,
            new Vector3(0, floorHeight, hallwayLength/2f),
            Quaternion.identity,
            currentFloorParent.transform
        );
        hallway.transform.localScale = new Vector3(hallwayWidth, 1, hallwayLength);

        // Place rooms along the hallway
        for (int i = 0; i < roomsForSecondFloor; i++)
        {
            RoomTypeSO roomType = otherRooms[roomsOnFirstFloor - 3 + i]; // Skip mandatory rooms
            RoomLayout layout = GetRandomLayout(roomType, false);

            bool leftSide = i % 2 == 0;
            float zPos = i * roomSpacing + roomSpacing/2f;

            Vector3 position = new Vector3(
                leftSide ? -(hallwayWidth/2 + layout.dimensions.x/2) : (hallwayWidth/2 + layout.dimensions.x/2),
                floorHeight,
                zPos
            );

            GameObject roomObj = Instantiate(
                layout.prefab,
                position,
                Quaternion.identity,
                currentFloorParent.transform
            );

            placedRooms.Add(new RoomInstance {
                gameObject = roomObj,
                type = roomType,
                gridPosition = new Vector2Int((int)position.x, (int)position.z),
                dimensions = layout.dimensions,
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

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw room bounds
        foreach (var room in placedRooms)
        {
            Gizmos.color = room.floorLevel == 0 ? Color.blue : Color.green;
            Bounds bounds = CalculateRoomBounds(room.gameObject);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            
            // Label rooms
            UnityEditor.Handles.Label(
                bounds.center, 
                $"{room.type.roomName}\nFloor: {room.floorLevel}"
            );
        }
        Gizmos.color = Color.yellow;
        foreach (var tile in occupiedGridPositions)
        {
            Vector3 pos = new Vector3(tile.x * roomSpacing, 0.1f, tile.y * roomSpacing);
            Gizmos.DrawCube(pos, Vector3.one * 0.5f);
        }

        
        // Draw floor separators
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-50, 0, 0), new Vector3(50, 0, 0));
        Gizmos.DrawLine(new Vector3(-50, floorHeight, 0), new Vector3(50, floorHeight, 0));
        if (isFourFloors)
        {
            Gizmos.DrawLine(new Vector3(-50, floorHeight*2, 0), new Vector3(50, floorHeight*2, 0));
        }
    }
}