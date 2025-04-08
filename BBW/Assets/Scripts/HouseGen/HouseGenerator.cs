using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomType
{
    public string name; // "Living Room", "Kitchen", etc.
    public GameObject[] variants; // A, B, C versions
    public bool isFirstFloorOnly;
    public bool canHaveBasementEntrance;
    public bool canHaveAtticEntrance;
}

public class HouseGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public RoomType[] roomTypes; // Assign all room types and variants in Inspector
    public GameObject doorPrefab;
    public GameObject windowPrefab;

    [Header("House Settings")]
    public bool hasBasement = true;
    public bool hasAttic = true;
    public int minFloors = 1;
    public int maxFloors = 2;
    public float roomSpacing = 10f;
    // public bool allRoomsOnFirstFloor = false;

    private List<GameObject> allRooms = new List<GameObject>();

    void Start()
    {
        GenerateHouse();
    }

    void GenerateHouse()
    {
        int totalFloors = Random.Range(minFloors, maxFloors + 1);
        bool distributeRooms = totalFloors > 1;

        // Generate main floors
        for (int floorNum = 0; floorNum < totalFloors; floorNum++)
        {
            GenerateFloor(floorNum, distributeRooms);
        }

        if (hasBasement) GenerateBasement();
        if (hasAttic) GenerateAttic();
        AddDoorsAndWindows();
    }

    void GenerateFloor(int floorNum, bool distributeRooms)
    {
        List<RoomType> availableRooms = new List<RoomType>(roomTypes);
        List<RoomType> roomsForThisFloor = new List<RoomType>();

        // First floor must have required rooms
        if (floorNum == 0)
        {
            foreach (RoomType roomType in roomTypes)
            {
                if (roomType.isFirstFloorOnly)
                {
                    roomsForThisFloor.Add(roomType);
                    availableRooms.Remove(roomType);
                }
            }
        }

        // Add remaining rooms
        if (!distributeRooms || floorNum == 0)
        {
            roomsForThisFloor.AddRange(availableRooms);
        }
        else
        {
            int roomsToAdd = Mathf.Min(availableRooms.Count, Random.Range(2, 5));
            for (int i = 0; i < roomsToAdd && availableRooms.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, availableRooms.Count);
                roomsForThisFloor.Add(availableRooms[randomIndex]);
                availableRooms.RemoveAt(randomIndex);
            }
        }

        // Spawn rooms with random variants
        Vector3 spawnPos = new Vector3(0, floorNum * 3f, 0);
        foreach (RoomType roomType in roomsForThisFloor)
        {
            GameObject variant = roomType.variants[Random.Range(0, roomType.variants.Length)];
            GameObject newRoom = Instantiate(variant, spawnPos, Quaternion.identity);
            allRooms.Add(newRoom);
            spawnPos.x += roomSpacing;
        }
    }

    void GenerateBasement()
    {
        Vector3 basementPos = new Vector3(0, -3f, 0);
        foreach (GameObject room in allRooms)
        {
            if (room.transform.position.y == 0)
            {
                Instantiate(room, basementPos, Quaternion.identity);
            }
        }
    }

    void GenerateAttic()
    {
        float topFloorY = (maxFloors - 1) * 3f;
        Vector3 atticPos = new Vector3(0, topFloorY + 3f, 0);
        foreach (GameObject room in allRooms)
        {
            if (room.transform.position.y == topFloorY)
            {
                Instantiate(room, atticPos, Quaternion.identity);
            }
        }
    }

    void AddDoorsAndWindows()
    {
        foreach (GameObject room in allRooms)
        {
            foreach (Transform wall in room.transform)
            {
                if (wall.name.Contains("Wall"))
                {
                    if (IsWallTouchingAnotherRoom(wall))
                    {
                        Vector3 doorPos = wall.position + wall.forward * 0.1f;
                        Instantiate(doorPrefab, doorPos, wall.rotation, wall);
                    }
                    else if (!room.name.Contains("Basement") && !room.name.Contains("Attic"))
                    {
                        Vector3 windowPos = wall.position + wall.forward * 0.1f;
                        Instantiate(windowPrefab, windowPos, wall.rotation, wall);
                    }
                }
            }
        }
    }

    bool IsWallTouchingAnotherRoom(Transform wall)
    {
        RaycastHit hit;
        if (Physics.Raycast(wall.position, wall.forward, out hit, 1f))
        {
            if (hit.transform.parent != null && hit.transform.parent != wall.parent)
            {
                return true;
            }
        }
        return false;
    }
}