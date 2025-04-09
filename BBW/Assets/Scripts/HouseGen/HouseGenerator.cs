using System.Collections.Generic;
using UnityEngine;

public class HouseGenerator : MonoBehaviour
{
    [Header("Config")]
    public float basementHeight = 3f;
    public float atticHeight = 2.5f;
    public float floorHeight = 3f;
    public float hallwayWidth = 2f;
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

    private class RoomInstance
    {
        public GameObject gameObject;
        public RoomTypeSO type;
        public Vector2Int position;
        public Vector2Int dimensions;
    }

    void Start()
    {
        GenerateHouse();
    }

    void GenerateHouse()
    {
        isFourFloors = Random.Range(0, 2) == 1;
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
        
        // Place mandatory rooms
        PlaceRoom(foyer, isFourFloors);
        PlaceRoom(kitchen);
        PlaceRoom(livingRoom);

        // Place optional rooms
        int roomsToPlace = isFourFloors ? 
            Random.Range(0, otherRooms.Length) :  // 4-floor: 0 to all-1
            otherRooms.Length;                   // 3-floor: all rooms

        for (int i = 0; i < roomsToPlace; i++)
        {
            PlaceRoom(otherRooms[i]);
        }

        AddBasementEntrance();
    }

    void PlaceRoom(RoomTypeSO roomType, bool needsStairs = false)
    {
        RoomLayout layout = GetRandomLayout(roomType, needsStairs);
        Vector2Int position = FindPlacementPosition(layout.dimensions);
        
        GameObject roomObj = Instantiate(
            layout.prefab,
            new Vector3(position.x, currentFloorParent.transform.position.y, position.y),
            Quaternion.identity,
            currentFloorParent.transform
        );
        
        placedRooms.Add(new RoomInstance {
            gameObject = roomObj,
            type = roomType,
            position = position,
            dimensions = layout.dimensions
        });
    }

    RoomLayout GetRandomLayout(RoomTypeSO roomType, bool needsStairs)
    {
        if (needsStairs)
        {
            List<RoomLayout> validLayouts = new List<RoomLayout>();
            foreach (var layout in roomType.layouts)
                if (layout.hasStairs) validLayouts.Add(layout);
            return validLayouts[Random.Range(0, validLayouts.Count)];
        }
        return roomType.layouts[Random.Range(0, roomType.layouts.Length)];
    }

    Vector2Int FindPlacementPosition(Vector2Int dimensions)
    {
        if (placedRooms.Count == 0) return Vector2Int.zero;

        // Simple placement - extend in a line
        RoomInstance lastRoom = placedRooms[placedRooms.Count - 1];
        return new Vector2Int(
            lastRoom.position.x + lastRoom.dimensions.x + 2,
            lastRoom.position.y
        );
    }

    void CreateSecondFloor()
    {
        currentFloorParent = new GameObject("Second Floor");
        currentFloorParent.transform.position = Vector3.up * floorHeight;
        
        // Create hallway
        int remainingRooms = otherRooms.Length - (placedRooms.Count - 3);
        float hallwayLength = (remainingRooms + 1) * roomSpacing;
        
        GameObject hallway = Instantiate(
            hallwayPrefab,
            new Vector3(0, floorHeight, hallwayLength/2f),
            Quaternion.identity,
            currentFloorParent.transform
        );
        hallway.transform.localScale = new Vector3(hallwayWidth, 1, hallwayLength);

        // Place remaining rooms
        for (int i = 0; i < remainingRooms; i++)
        {
            RoomTypeSO roomType = otherRooms[placedRooms.Count - 3 + i];
            RoomLayout layout = GetRandomLayout(roomType, false);
            
            bool leftSide = i % 2 == 0;
            float zPos = i * roomSpacing + roomSpacing/2f;
            
            GameObject roomObj = Instantiate(
                layout.prefab,
                new Vector3(
                    leftSide ? -hallwayWidth - layout.dimensions.x/2 : hallwayWidth + layout.dimensions.x/2,
                    floorHeight,
                    zPos
                ),
                Quaternion.identity,
                currentFloorParent.transform
            );
            
            placedRooms.Add(new RoomInstance {
                gameObject = roomObj,
                type = roomType,
                position = new Vector2Int(leftSide ? -1 : 1, (int)zPos),
                dimensions = layout.dimensions
            });
        }
    }

    void CreateAttic()
    {
        Bounds bounds = new Bounds();
        foreach (var room in placedRooms)
        {
            if (room.gameObject.transform.parent == currentFloorParent.transform)
            {
                bounds.Encapsulate(room.gameObject.GetComponent<Renderer>().bounds);
            }
        }
        
        float atticY = isFourFloors ? floorHeight * 2 + atticHeight/2 : floorHeight + atticHeight/2;
        GameObject attic = Instantiate(
            atticPrefab,
            new Vector3(bounds.center.x, atticY, bounds.center.z),
            Quaternion.identity
        );
        attic.transform.localScale = new Vector3(bounds.size.x, atticHeight, bounds.size.z);
        
        AddAtticEntrance();
    }

    void AddBasementEntrance()
    {
        if (placedRooms.Count == 0) return;
        int randomIndex = Random.Range(0, placedRooms.Count);
        // Implement your basement entrance logic here
    }

    void AddAtticEntrance()
    {
        List<RoomInstance> topFloorRooms = placedRooms.FindAll(
            r => r.gameObject.transform.parent == currentFloorParent.transform
        );
        if (topFloorRooms.Count == 0) return;
        int randomIndex = Random.Range(0, topFloorRooms.Count);
        // Implement your attic entrance logic here
    }

    void ConnectAllRooms()
    {
        // Implement your door connection logic here
        // Example pseudo-code:
        foreach (var room in placedRooms)
        {
            Collider[] hitColliders = Physics.OverlapBox(
                room.gameObject.transform.position,
                new Vector3(room.dimensions.x/2, 1, room.dimensions.y/2)
            );
            
            foreach (var hit in hitColliders)
            {
                if (hit.gameObject != room.gameObject)
                {
                    // Place door between rooms
                    Vector3 doorPos = (room.gameObject.transform.position + hit.transform.position) / 2;
                    Instantiate(doorPrefab, doorPos, Quaternion.identity);
                }
            }
        }
    }
}