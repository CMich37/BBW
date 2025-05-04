// using System.Collections.Generic;
// using UnityEngine;

// public class HouseGenerator : MonoBehaviour
// {
//     [Header("Config")]
//     public float basementHeight = 3f;
//     public float atticHeight = 2.5f;
//     public float floorHeight = 3f;
//     public float hallwayWidth = 10f;
//     public float roomSpacing = 5f;

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

//     private List<RoomInstance> placedRooms = new List<RoomInstance>();
//     private bool isFourFloors;
//     private GameObject currentFloorParent;
//     private Vector2Int currentGridPosition = Vector2Int.zero;

//     private class RoomInstance
//     {
//         public GameObject gameObject;
//         public RoomTypeSO type;
//         public Vector2Int gridPosition;
//         public Vector2Int dimensions;
//         public int floorLevel;
//     }

//     void Start()
//     {
//         GenerateHouse();
//     }

//     void GenerateHouse()
//     {
//         isFourFloors = Random.Range(0, 2) == 1;
//         CreateBasement();
//         CreateFirstFloor();
        
//         if (isFourFloors)
//         {
//             CreateSecondFloor();
//         }
        
//         CreateAttic();
//         ConnectAllRooms();
//     }

//     void CreateBasement()
//     {
//         GameObject basement = Instantiate(basementPrefab, Vector3.down * basementHeight, Quaternion.identity);
//     }

//     void CreateFirstFloor()
//     {
//         currentFloorParent = new GameObject("First Floor");
//         currentFloorParent.transform.position = Vector3.zero;
//         currentGridPosition = Vector2Int.zero;

//         // Place mandatory rooms first
//         PlaceRoom(foyer, isFourFloors); // Foyer first
//         PlaceRoom(kitchen);
//         PlaceRoom(livingRoom);

//         // Place optional rooms
//         int roomsToPlace = isFourFloors ? Mathf.Max(0, otherRooms.Length - 1) : otherRooms.Length;
//         roomsToPlace = Mathf.Min(roomsToPlace, otherRooms.Length); // Safety check
        
//         for (int i = 0; i < roomsToPlace; i++)
//         {
//             PlaceRoom(otherRooms[i]);
//         }

//         AddBasementEntrance();
//     }

//     void PlaceRoom(RoomTypeSO roomType, bool needsStairs = false)
//     {
//         RoomLayout layout = GetRandomLayout(roomType, needsStairs);
        
//         // Find position (simple grid-based placement)
//         Vector2Int position = currentGridPosition;
        
//         // Update grid position for next room
//         currentGridPosition.x += layout.dimensions.x + 1; // +1 for wall thickness
        
//         GameObject roomObj = Instantiate(
//             layout.prefab,
//             new Vector3(position.x, currentFloorParent.transform.position.y, position.y),
//             Quaternion.identity,
//             currentFloorParent.transform
//         );
        
//         placedRooms.Add(new RoomInstance {
//             gameObject = roomObj,
//             type = roomType,
//             gridPosition = position,
//             dimensions = layout.dimensions,
//             floorLevel = isFourFloors ? (currentFloorParent.name == "First Floor" ? 0 : 1) : 0
//         });
//     }

//     void CreateSecondFloor()
//     {
//         currentFloorParent = new GameObject("Second Floor");
//         currentFloorParent.transform.position = Vector3.up * floorHeight;
        
//         // Create central hallway
//         float hallwayLength = (otherRooms.Length - (placedRooms.Count - 3) + 1) * roomSpacing;
//         GameObject hallway = Instantiate(
//             hallwayPrefab,
//             new Vector3(0, floorHeight, hallwayLength/2f),
//             Quaternion.identity,
//             currentFloorParent.transform
//         );
//         hallway.transform.localScale = new Vector3(hallwayWidth, 1, hallwayLength);

//         // Place remaining rooms along the hallway
//         int roomsPlacedOnFirstFloor = placedRooms.Count - 3; // minus foyer, kitchen, living room
//         for (int i = 0; i < otherRooms.Length - roomsPlacedOnFirstFloor; i++)
//         {
//             RoomTypeSO roomType = otherRooms[roomsPlacedOnFirstFloor + i];
//             RoomLayout layout = GetRandomLayout(roomType, false);
            
//             bool leftSide = i % 2 == 0;
//             float zPos = i * roomSpacing + roomSpacing/2f;
            
//             Vector2Int position = new Vector2Int(
//                 leftSide ? -(int)(hallwayWidth/2 + layout.dimensions.x/2) : (int)(hallwayWidth/2 + layout.dimensions.x/2),
//                 (int)zPos
//             );
            
//             GameObject roomObj = Instantiate(
//                 layout.prefab,
//                 new Vector3(position.x, floorHeight, position.y),
//                 Quaternion.identity,
//                 currentFloorParent.transform
//             );
            
//             placedRooms.Add(new RoomInstance {
//                 gameObject = roomObj,
//                 type = roomType,
//                 gridPosition = position,
//                 dimensions = layout.dimensions,
//                 floorLevel = 1
//             });
//         }
//     }

//     void CreateAttic()
//     {
//         // Find bounds of top floor
//         string topFloorName = isFourFloors ? "Second Floor" : "First Floor";
//         Bounds bounds = new Bounds();
//         bool hasBounds = false;
        
//         foreach (var room in placedRooms)
//         {
//             if (room.gameObject.transform.parent.name == topFloorName)
//             {
//                 var roomBounds = CalculateRoomBounds(room.gameObject);
//                 if (!hasBounds)
//                 {
//                     bounds = roomBounds;
//                     hasBounds = true;
//                 }
//                 else
//                 {
//                     bounds.Encapsulate(roomBounds);
//                 }
//             }
//         }
        
//         // Create attic
//         float atticY = isFourFloors ? floorHeight * 2 + atticHeight/2 : floorHeight + atticHeight/2;
//         GameObject attic = Instantiate(
//             atticPrefab,
//             new Vector3(bounds.center.x, atticY, bounds.center.z),
//             Quaternion.identity
//         );
//         attic.transform.localScale = new Vector3(bounds.size.x, atticHeight, bounds.size.z);
        
//         AddAtticEntrance();
//     }

//     void ConnectAllRooms()
//     {
//         // Connect rooms on each floor separately
//         for (int floor = 0; floor < (isFourFloors ? 2 : 1); floor++)
//         {
//             List<RoomInstance> floorRooms = placedRooms.FindAll(r => r.floorLevel == floor);
            
//             for (int i = 0; i < floorRooms.Count; i++)
//             {
//                 for (int j = i + 1; j < floorRooms.Count; j++)
//                 {
//                     if (AreRoomsAdjacent(floorRooms[i], floorRooms[j]))
//                     {
//                         CreateDoorBetweenRooms(floorRooms[i], floorRooms[j]);
//                     }
//                 }
//             }
//         }
//     }

//     bool AreRoomsAdjacent(RoomInstance room1, RoomInstance room2)
//     {
//         // Check if rooms are adjacent in X axis
//         bool adjacentX = (room1.gridPosition.x + room1.dimensions.x == room2.gridPosition.x) ||
//                          (room2.gridPosition.x + room2.dimensions.x == room1.gridPosition.x);
        
//         // Check if rooms overlap in Z axis
//         bool overlapZ = (room1.gridPosition.y < room2.gridPosition.y + room2.dimensions.y) &&
//                        (room1.gridPosition.y + room1.dimensions.y > room2.gridPosition.y);
        
//         // Check if rooms are adjacent in Z axis
//         bool adjacentZ = (room1.gridPosition.y + room1.dimensions.y == room2.gridPosition.y) ||
//                          (room2.gridPosition.y + room2.dimensions.y == room1.gridPosition.y);
        
//         // Check if rooms overlap in X axis
//         bool overlapX = (room1.gridPosition.x < room2.gridPosition.x + room2.dimensions.x) &&
//                        (room1.gridPosition.x + room1.dimensions.x > room2.gridPosition.x);
        
//         return (adjacentX && overlapZ) || (adjacentZ && overlapX);
//     }

//     void CreateDoorBetweenRooms(RoomInstance room1, RoomInstance room2)
//     {
//         Vector3 doorPosition = (room1.gameObject.transform.position + room2.gameObject.transform.position) / 2f;
//         Instantiate(doorPrefab, doorPosition, Quaternion.identity);
//     }

//     RoomLayout GetRandomLayout(RoomTypeSO roomType, bool needsStairs)
//     {
//         if (needsStairs)
//         {
//             // Filter for layouts with stairs if needed (for foyer)
//             List<RoomLayout> validLayouts = new List<RoomLayout>();
//             foreach (var layout in roomType.layouts)
//             {
//                 if (layout.hasStairs) 
//                     validLayouts.Add(layout);
//             }
            
//             if (validLayouts.Count == 0)
//             {
//                 Debug.LogWarning($"No stair layouts found for {roomType.roomName}");
//                 return roomType.layouts[0]; // Fallback
//             }
            
//             return validLayouts[Random.Range(0, validLayouts.Count)];
//         }
        
//         // Return any random layout if stairs aren't required
//         return roomType.layouts[Random.Range(0, roomType.layouts.Length)];
//     }

//     Bounds CalculateRoomBounds(GameObject room)
//     {
//         Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
//         if (renderers.Length == 0) 
//         {
//             Debug.LogWarning($"Room {room.name} has no renderers, using default bounds");
//             return new Bounds(room.transform.position, Vector3.one * 5f);
//         }
        
//         Bounds bounds = renderers[0].bounds;
//         for (int i = 1; i < renderers.Length; i++)
//         {
//             bounds.Encapsulate(renderers[i].bounds);
//         }
//         return bounds;
//     }

//     void AddBasementEntrance()
//     {
//         if (placedRooms.Count == 0 || doorPrefab == null) 
//         {
//             Debug.LogWarning("Cannot add basement entrance - no rooms or door prefab");
//             return;
//         }
        
//         // Find all first-floor rooms
//         List<RoomInstance> firstFloorRooms = placedRooms.FindAll(
//             r => r.gameObject.transform.parent.name == "First Floor"
//         );
        
//         if (firstFloorRooms.Count == 0) return;
        
//         // Select random first-floor room (excluding foyer if desired)
//         RoomInstance selectedRoom = firstFloorRooms[Random.Range(0, firstFloorRooms.Count)];
        
//         // Place door at the room's position (adjust as needed)
//         Vector3 doorPos = selectedRoom.gameObject.transform.position;
//         doorPos.y = 0; // Align with floor level
        
//         Instantiate(doorPrefab, doorPos, Quaternion.identity, selectedRoom.gameObject.transform);
//     }

//     void AddAtticEntrance()
//     {
//         if (placedRooms.Count == 0 || doorPrefab == null) 
//         {
//             Debug.LogWarning("Cannot add attic entrance - no rooms or door prefab");
//             return;
//         }
        
//         // Find all top-floor rooms
//         string topFloorName = isFourFloors ? "Second Floor" : "First Floor";
//         List<RoomInstance> topFloorRooms = placedRooms.FindAll(
//             r => r.gameObject.transform.parent.name == topFloorName
//         );
        
//         if (topFloorRooms.Count == 0) return;
        
//         // Select random top-floor room
//         RoomInstance selectedRoom = topFloorRooms[Random.Range(0, topFloorRooms.Count)];
        
//         // Place door at the room's position (adjust as needed)
//         Vector3 doorPos = selectedRoom.gameObject.transform.position;
//         doorPos.y = isFourFloors ? floorHeight * 2 : floorHeight; // Top floor height
        
//         Instantiate(doorPrefab, doorPos, Quaternion.identity, selectedRoom.gameObject.transform);
//     }

//     void OnDrawGizmos()
//     {
//         if (!Application.isPlaying) return;
        
//         // Draw room bounds
//         foreach (var room in placedRooms)
//         {
//             Gizmos.color = room.floorLevel == 0 ? Color.blue : Color.green;
//             Bounds bounds = CalculateRoomBounds(room.gameObject);
//             Gizmos.DrawWireCube(bounds.center, bounds.size);
            
//             // Label rooms
//             UnityEditor.Handles.Label(
//                 bounds.center, 
//                 $"{room.type.roomName}\nFloor: {room.floorLevel}"
//             );
//         }
        
//         // Draw floor separators
//         Gizmos.color = Color.red;
//         Gizmos.DrawLine(new Vector3(-50, 0, 0), new Vector3(50, 0, 0));
//         Gizmos.DrawLine(new Vector3(-50, floorHeight, 0), new Vector3(50, floorHeight, 0));
//         if (isFourFloors)
//         {
//             Gizmos.DrawLine(new Vector3(-50, floorHeight*2, 0), new Vector3(50, floorHeight*2, 0));
//         }
//     }
// }



// // using System.Collections.Generic;
// // using UnityEngine;

// // public class HouseGenerator : MonoBehaviour
// // {
// //     [Header("Config")]
// //     public float basementHeight = 3f;
// //     public float atticHeight = 2.5f;
// //     public float floorHeight = 3f;
// //     public float hallwayWidth = 10f;
// //     public float roomSpacing = 5f;

// //     [Header("Prefabs")]
// //     public GameObject basementPrefab;
// //     public GameObject atticPrefab;
// //     public GameObject hallwayPrefab;
// //     public GameObject doorPrefab;

// //     [Header("Room Types")]
// //     public RoomTypeSO foyer;
// //     public RoomTypeSO kitchen;
// //     public RoomTypeSO livingRoom;
// //     public RoomTypeSO[] otherRooms;

// //     private List<RoomInstance> placedRooms = new List<RoomInstance>();
// //     private bool isFourFloors;
// //     private GameObject currentFloorParent;

// //     private class RoomInstance
// //     {
// //         public GameObject gameObject;
// //         public RoomTypeSO type;
// //         public Vector2Int position;
// //         public Vector2Int dimensions;
// //     }

// //     void Start()
// // {
// //     GenerateHouse();
// // }

// // void GenerateHouse()
// // {
// //     isFourFloors = Random.Range(0, 2) == 1;
// //     CreateBasement();
// //     CreateFirstFloor();
    
// //     if (isFourFloors)
// //     {
// //         CreateSecondFloor();
// //     }
    
// //     CreateAttic();
// //     ConnectAllRooms();
// // }

// // void CreateBasement()
// // {
// //     GameObject basement = Instantiate(basementPrefab, Vector3.down * basementHeight, Quaternion.identity);
// // }

// // void CreateFirstFloor()
// // {
// //     currentFloorParent = new GameObject("First Floor");
// //     currentFloorParent.transform.position = Vector3.zero;
    
// //     // Place mandatory rooms
// //     PlaceRoom(foyer, isFourFloors);
// //     PlaceRoom(kitchen);
// //     PlaceRoom(livingRoom);

// //     // Place optional rooms - CRITICAL FIX HERE
// //     if (isFourFloors)
// //     {
// //         // For 4-floor houses: ensure at least 1 room remains for 2nd floor
// //         int maxPossible = Mathf.Max(0, otherRooms.Length - 1); // Always leave 1+ for 2nd floor
// //         int extraRooms = Random.Range(0, maxPossible + 1); // +1 because max is inclusive
        
// //         for (int i = 0; i < extraRooms; i++)
// //         {
// //             PlaceRoom(otherRooms[i]);
// //         }
// //     }
// //     else
// //     {
// //         // 3-floor house - place all rooms
// //         foreach (var room in otherRooms)
// //         {
// //             PlaceRoom(room);
// //         }
// //     }

// //     AddBasementEntrance();
// // }

// // void PlaceRoom(RoomTypeSO roomType, bool needsStairs = false)
// // {
// //     RoomLayout layout = GetRandomLayout(roomType, needsStairs);
// //     Vector2Int position = FindPlacementPosition(layout.dimensions);
    
// //     GameObject roomObj = Instantiate(
// //         layout.prefab,
// //         new Vector3(position.x, currentFloorParent.transform.position.y, position.y),
// //         Quaternion.identity,
// //         currentFloorParent.transform
// //     );
    
// //     placedRooms.Add(new RoomInstance {
// //         gameObject = roomObj,
// //         type = roomType,
// //         position = position,
// //         dimensions = layout.dimensions
// //     });
// // }

// // RoomLayout GetRandomLayout(RoomTypeSO roomType, bool needsStairs)
// // {
// //     if (needsStairs)
// //     {
// //         List<RoomLayout> validLayouts = new List<RoomLayout>();
// //         foreach (var layout in roomType.layouts)
// //             if (layout.hasStairs) validLayouts.Add(layout);
// //         return validLayouts[Random.Range(0, validLayouts.Count)];
// //     }
// //     return roomType.layouts[Random.Range(0, roomType.layouts.Length)];
// // }

// // Vector2Int FindPlacementPosition(Vector2Int dimensions)
// // {
// //     if (placedRooms.Count == 0) return Vector2Int.zero;

// //     // Try 4 possible positions around last room
// //     RoomInstance lastRoom = placedRooms[placedRooms.Count - 1];
// //     Vector2Int[] testPositions = {
// //         new Vector2Int(lastRoom.position.x + lastRoom.dimensions.x + 2, lastRoom.position.y), // Right
// //         new Vector2Int(lastRoom.position.x - dimensions.x - 2, lastRoom.position.y),          // Left
// //         new Vector2Int(lastRoom.position.x, lastRoom.position.y + lastRoom.dimensions.y + 2), // Up
// //         new Vector2Int(lastRoom.position.x, lastRoom.position.y - dimensions.y - 2)           // Down
// //     };

// //     foreach (Vector2Int testPos in testPositions)
// //     {
// //         if (!Physics.CheckBox(
// //             new Vector3(testPos.x, 0, testPos.y),
// //             new Vector3(dimensions.x/2f, 1f, dimensions.y/2f)))
// //         {
// //             return testPos;
// //         }
// //     }
    
// //     // Fallback - stack vertically if no space
// //     return new Vector2Int(0, lastRoom.position.y + lastRoom.dimensions.y + 2);
// // }

// // void CreateSecondFloor()
// // {
// //     currentFloorParent = new GameObject("Second Floor");
// //     currentFloorParent.transform.position = Vector3.up * floorHeight;
    
// //     // Create hallway
// //     int remainingRooms = otherRooms.Length - (placedRooms.Count - 3);
// //     float hallwayLength = (remainingRooms + 1) * roomSpacing;
    
// //     GameObject hallway = Instantiate(
// //         hallwayPrefab,
// //         new Vector3(0, floorHeight, hallwayLength/2f),
// //         Quaternion.identity,
// //         currentFloorParent.transform
// //     );
// //     hallway.transform.localScale = new Vector3(hallwayWidth, 1, hallwayLength);

// //     // Place remaining rooms
// //     for (int i = 0; i < remainingRooms; i++)
// //     {
// //         RoomTypeSO roomType = otherRooms[placedRooms.Count - 3 + i];
// //         RoomLayout layout = GetRandomLayout(roomType, false);
        
// //         bool leftSide = i % 2 == 0;
// //         float zPos = i * roomSpacing + roomSpacing/2f;
        
// //         GameObject roomObj = Instantiate(
// //             layout.prefab,
// //             new Vector3(
// //                 leftSide ? -hallwayWidth - layout.dimensions.x/2 : hallwayWidth + layout.dimensions.x/2,
// //                 floorHeight,
// //                 zPos
// //             ),
// //             Quaternion.identity,
// //             currentFloorParent.transform
// //         );
        
// //         placedRooms.Add(new RoomInstance {
// //             gameObject = roomObj,
// //             type = roomType,
// //             position = new Vector2Int(leftSide ? -1 : 1, (int)zPos),
// //             dimensions = layout.dimensions
// //         });
// //     }
// // }

// // void CreateAttic()
// // {
// //     // Calculate bounds from all rooms on current floor
// //     Bounds bounds = new Bounds();
// //     bool hasBounds = false;
    
// //     foreach (var room in placedRooms)
// //     {
// //         if (room.gameObject.transform.parent == currentFloorParent.transform)
// //         {
// //             var roomBounds = CalculateRoomBounds(room.gameObject);
// //             if (!hasBounds)
// //             {
// //                 bounds = roomBounds;
// //                 hasBounds = true;
// //             }
// //             else
// //             {
// //                 bounds.Encapsulate(roomBounds);
// //             }
// //         }
// //     }
    
// //     // Create attic
// //     float atticY = isFourFloors ? floorHeight * 2 + atticHeight/2 : floorHeight + atticHeight/2;
// //     GameObject attic = Instantiate(
// //         atticPrefab,
// //         new Vector3(bounds.center.x, atticY, bounds.center.z),
// //         Quaternion.identity
// //     );
// //     attic.transform.localScale = new Vector3(bounds.size.x, atticHeight, bounds.size.z);
    
// //     AddAtticEntrance();
// // }

// // void ConnectAllRooms()
// // {
// //     foreach (var room in placedRooms)
// //     {
// //         var roomBounds = CalculateRoomBounds(room.gameObject);
// //         Collider[] hitColliders = Physics.OverlapBox(
// //             roomBounds.center,
// //             roomBounds.extents,
// //             room.gameObject.transform.rotation
// //         );
        
// //         foreach (var hit in hitColliders)
// //         {
// //             if (hit.gameObject != room.gameObject && 
// //                 !hit.transform.IsChildOf(room.gameObject.transform))
// //             {
// //                 Vector3 doorPos = (roomBounds.center + hit.bounds.center) / 2f;
// //                 Instantiate(doorPrefab, doorPos, Quaternion.identity);
// //             }
// //         }
// //     }
// // }

// // // Helper method to calculate bounds for a room
// // Bounds CalculateRoomBounds(GameObject room)
// // {
// //     Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
// //     if (renderers.Length == 0) return new Bounds(room.transform.position, Vector3.one * 5f);
    
// //     Bounds bounds = renderers[0].bounds;
// //     for (int i = 1; i < renderers.Length; i++)
// //     {
// //         bounds.Encapsulate(renderers[i].bounds);
// //     }
// //     return bounds;
// // }

// // void AddBasementEntrance()
// // {
// //     if (placedRooms.Count == 0 || doorPrefab == null) return;
    
// //     // Find all first-floor rooms
// //     List<RoomInstance> firstFloorRooms = placedRooms.FindAll(
// //         r => r.gameObject.transform.parent.name == "First Floor"
// //     );
    
// //     if (firstFloorRooms.Count == 0) return;
    
// //     // Select random first-floor room
// //     int randomIndex = Random.Range(0, firstFloorRooms.Count);
// //     RoomInstance selectedRoom = firstFloorRooms[randomIndex];
    
// //     // Place door at the room's position (adjust as needed)
// //     Instantiate(doorPrefab, 
// //         selectedRoom.gameObject.transform.position,
// //         Quaternion.identity,
// //         selectedRoom.gameObject.transform);
// // }

// // void AddAtticEntrance()
// // {
// //     if (placedRooms.Count == 0 || doorPrefab == null) return;
    
// //     // Find all top-floor rooms
// //     string topFloorName = isFourFloors ? "Second Floor" : "First Floor";
// //     List<RoomInstance> topFloorRooms = placedRooms.FindAll(
// //         r => r.gameObject.transform.parent.name == topFloorName
// //     );
    
// //     if (topFloorRooms.Count == 0) return;
    
// //     // Select random top-floor room
// //     int randomIndex = Random.Range(0, topFloorRooms.Count);
// //     RoomInstance selectedRoom = topFloorRooms[randomIndex];
    
// //     // Place door at the room's position (adjust as needed)
// //     Instantiate(doorPrefab,
// //         selectedRoom.gameObject.transform.position + Vector3.up * 1.5f, // Slightly above floor
// //         Quaternion.identity,
// //         selectedRoom.gameObject.transform);
// // }

// // }

////////kris
///
// using System.Collections.Generic;
// using UnityEngine;

// public class HouseGenerator : MonoBehaviour
// {
//     [Header("Config")]
//     public float basementHeight = 3f;
//     public float atticHeight = 2.5f;
//     public float floorHeight = 3f;
//     public float hallwayWidth = 10f;
//     public float roomSpacing = 5f;

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

//     private List<RoomInstance> placedRooms = new List<RoomInstance>();
//     private bool isFourFloors;
//     private GameObject currentFloorParent;

//     private class RoomInstance
//     {
//         public GameObject gameObject;
//         public RoomTypeSO type;
//         public Vector2Int position;
//         public Vector2Int dimensions;
//     }

//     void Start()
// {
//     GenerateHouse();
// }

// void GenerateHouse()
// {
//     isFourFloors = Random.Range(0, 2) == 1;
//     CreateBasement();
//     CreateFirstFloor();
    
//     if (isFourFloors)
//     {
//         CreateSecondFloor();
//     }
    
//     CreateAttic();
//     ConnectAllRooms();
// }

// void CreateBasement()
// {
//     GameObject basement = Instantiate(basementPrefab, Vector3.down * basementHeight, Quaternion.identity);
// }

// void CreateFirstFloor()
// {
//     currentFloorParent = new GameObject("First Floor");
//     currentFloorParent.transform.position = Vector3.zero;
    
//     // Place mandatory rooms
//     PlaceRoom(foyer, isFourFloors);
//     PlaceRoom(kitchen);
//     PlaceRoom(livingRoom);

//     // Place optional rooms - CRITICAL FIX HERE
//     if (isFourFloors)
//     {
//         // For 4-floor houses: ensure at least 1 room remains for 2nd floor
//         int maxPossible = Mathf.Max(0, otherRooms.Length - 1); // Always leave 1+ for 2nd floor
//         int extraRooms = Random.Range(0, maxPossible + 1); // +1 because max is inclusive
        
//         for (int i = 0; i < extraRooms; i++)
//         {
//             PlaceRoom(otherRooms[i]);
//         }
//     }
//     else
//     {
//         // 3-floor house - place all rooms
//         foreach (var room in otherRooms)
//         {
//             PlaceRoom(room);
//         }
//     }

//     AddBasementEntrance();
// }

// void PlaceRoom(RoomTypeSO roomType, bool needsStairs = false)
// {
//     RoomLayout layout = GetRandomLayout(roomType, needsStairs);
//     Vector2Int position = FindPlacementPosition(layout.dimensions);
    
//     GameObject roomObj = Instantiate(
//         layout.prefab,
//         new Vector3(position.x, currentFloorParent.transform.position.y, position.y),
//         Quaternion.identity,
//         currentFloorParent.transform
//     );
    
//     placedRooms.Add(new RoomInstance {
//         gameObject = roomObj,
//         type = roomType,
//         position = position,
//         dimensions = layout.dimensions
//     });
// }

// RoomLayout GetRandomLayout(RoomTypeSO roomType, bool needsStairs)
// {
//     if (needsStairs)
//     {
//         List<RoomLayout> validLayouts = new List<RoomLayout>();
//         foreach (var layout in roomType.layouts)
//             if (layout.hasStairs) validLayouts.Add(layout);
//         return validLayouts[Random.Range(0, validLayouts.Count)];
//     }
//     return roomType.layouts[Random.Range(0, roomType.layouts.Length)];
// }

// Vector2Int FindPlacementPosition(Vector2Int dimensions)
// {
//     if (placedRooms.Count == 0) return Vector2Int.zero;

//     // Try 4 possible positions around last room
//     RoomInstance lastRoom = placedRooms[placedRooms.Count - 1];
//     Vector2Int[] testPositions = {
//         new Vector2Int(lastRoom.position.x + lastRoom.dimensions.x + 2, lastRoom.position.y), // Right
//         new Vector2Int(lastRoom.position.x - dimensions.x - 2, lastRoom.position.y),          // Left
//         new Vector2Int(lastRoom.position.x, lastRoom.position.y + lastRoom.dimensions.y + 2), // Up
//         new Vector2Int(lastRoom.position.x, lastRoom.position.y - dimensions.y - 2)           // Down
//     };

//     foreach (Vector2Int testPos in testPositions)
//     {
//         if (!Physics.CheckBox(
//             new Vector3(testPos.x, 0, testPos.y),
//             new Vector3(dimensions.x/2f, 1f, dimensions.y/2f)))
//         {
//             return testPos;
//         }
//     }
    
//     // Fallback - stack vertically if no space
//     return new Vector2Int(0, lastRoom.position.y + lastRoom.dimensions.y + 2);
// }

// void CreateSecondFloor()
// {
//     currentFloorParent = new GameObject("Second Floor");
//     currentFloorParent.transform.position = Vector3.up * floorHeight;
    
//     // Create hallway
//     int remainingRooms = otherRooms.Length - (placedRooms.Count - 3);
//     float hallwayLength = (remainingRooms + 1) * roomSpacing;
    
//     GameObject hallway = Instantiate(
//         hallwayPrefab,
//         new Vector3(0, floorHeight, hallwayLength/2f),
//         Quaternion.identity,
//         currentFloorParent.transform
//     );
//     hallway.transform.localScale = new Vector3(hallwayWidth, 1, hallwayLength);

//     // Place remaining rooms
//     for (int i = 0; i < remainingRooms; i++)
//     {
//         RoomTypeSO roomType = otherRooms[placedRooms.Count - 3 + i];
//         RoomLayout layout = GetRandomLayout(roomType, false);
        
//         bool leftSide = i % 2 == 0;
//         float zPos = i * roomSpacing + roomSpacing/2f;
        
//         GameObject roomObj = Instantiate(
//             layout.prefab,
//             new Vector3(
//                 leftSide ? -hallwayWidth - layout.dimensions.x/2 : hallwayWidth + layout.dimensions.x/2,
//                 floorHeight,
//                 zPos
//             ),
//             Quaternion.identity,
//             currentFloorParent.transform
//         );
        
//         placedRooms.Add(new RoomInstance {
//             gameObject = roomObj,
//             type = roomType,
//             position = new Vector2Int(leftSide ? -1 : 1, (int)zPos),
//             dimensions = layout.dimensions
//         });
//     }
// }

// void CreateAttic()
// {
//     // Calculate bounds from all rooms on current floor
//     Bounds bounds = new Bounds();
//     bool hasBounds = false;
    
//     foreach (var room in placedRooms)
//     {
//         if (room.gameObject.transform.parent == currentFloorParent.transform)
//         {
//             var roomBounds = CalculateRoomBounds(room.gameObject);
//             if (!hasBounds)
//             {
//                 bounds = roomBounds;
//                 hasBounds = true;
//             }
//             else
//             {
//                 bounds.Encapsulate(roomBounds);
//             }
//         }
//     }
    
//     // Create attic
//     float atticY = isFourFloors ? floorHeight * 2 + atticHeight/2 : floorHeight + atticHeight/2;
//     GameObject attic = Instantiate(
//         atticPrefab,
//         new Vector3(bounds.center.x, atticY, bounds.center.z),
//         Quaternion.identity
//     );
//     attic.transform.localScale = new Vector3(bounds.size.x, atticHeight, bounds.size.z);
    
//     AddAtticEntrance();
// }

// void ConnectAllRooms()
// {
//     foreach (var room in placedRooms)
//     {
//         var roomBounds = CalculateRoomBounds(room.gameObject);
//         Collider[] hitColliders = Physics.OverlapBox(
//             roomBounds.center,
//             roomBounds.extents,
//             room.gameObject.transform.rotation
//         );
        
//         foreach (var hit in hitColliders)
//         {
//             if (hit.gameObject != room.gameObject && 
//                 !hit.transform.IsChildOf(room.gameObject.transform))
//             {
//                 Vector3 doorPos = (roomBounds.center + hit.bounds.center) / 2f;
//                 Instantiate(doorPrefab, doorPos, Quaternion.identity);
//             }
//         }
//     }
// }

// // Helper method to calculate bounds for a room
// Bounds CalculateRoomBounds(GameObject room)
// {
//     Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
//     if (renderers.Length == 0) return new Bounds(room.transform.position, Vector3.one * 5f);
    
//     Bounds bounds = renderers[0].bounds;
//     for (int i = 1; i < renderers.Length; i++)
//     {
//         bounds.Encapsulate(renderers[i].bounds);
//     }
//     return bounds;
// }

// void AddBasementEntrance()
// {
//     if (placedRooms.Count == 0 || doorPrefab == null) return;
    
//     // Find all first-floor rooms
//     List<RoomInstance> firstFloorRooms = placedRooms.FindAll(
//         r => r.gameObject.transform.parent.name == "First Floor"
//     );
    
//     if (firstFloorRooms.Count == 0) return;
    
//     // Select random first-floor room
//     int randomIndex = Random.Range(0, firstFloorRooms.Count);
//     RoomInstance selectedRoom = firstFloorRooms[randomIndex];
    
//     // Place door at the room's position (adjust as needed)
//     Instantiate(doorPrefab, 
//         selectedRoom.gameObject.transform.position,
//         Quaternion.identity,
//         selectedRoom.gameObject.transform);
// }

// void AddAtticEntrance()
// {
//     if (placedRooms.Count == 0 || doorPrefab == null) return;
    
//     // Find all top-floor rooms
//     string topFloorName = isFourFloors ? "Second Floor" : "First Floor";
//     List<RoomInstance> topFloorRooms = placedRooms.FindAll(
//         r => r.gameObject.transform.parent.name == topFloorName
//     );
    
//     if (topFloorRooms.Count == 0) return;
    
//     // Select random top-floor room
//     int randomIndex = Random.Range(0, topFloorRooms.Count);
//     RoomInstance selectedRoom = topFloorRooms[randomIndex];
    
//     // Place door at the room's position (adjust as needed)
//     Instantiate(doorPrefab,
//         selectedRoom.gameObject.transform.position + Vector3.up * 1.5f, // Slightly above floor
//         Quaternion.identity,
//         selectedRoom.gameObject.transform);
// }

// }original

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
        currentGridPosition = Vector2Int.zero;

        // Place mandatory rooms first
        PlaceRoom(foyer, isFourFloors); // Foyer first
        PlaceRoom(kitchen);
        PlaceRoom(livingRoom);

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
        
        // Calculate position based on previous rooms
        Vector2Int position = CalculateNextRoomPosition(layout.dimensions);
        
        GameObject roomObj = Instantiate(
            layout.prefab,
            new Vector3(position.x * roomSpacing, currentFloorParent.transform.position.y, position.y * roomSpacing),
            Quaternion.identity,
            currentFloorParent.transform
        );
        
        placedRooms.Add(new RoomInstance {
            gameObject = roomObj,
            type = roomType,
            gridPosition = position,
            dimensions = layout.dimensions,
            floorLevel = isFourFloors ? (currentFloorParent.name == "First Floor" ? 0 : 1) : 0
        });
    }

    Vector2Int CalculateNextRoomPosition(Vector2Int dimensions)
    {
        if (placedRooms.Count == 0 || placedRooms[placedRooms.Count-1].floorLevel != (isFourFloors ? (currentFloorParent.name == "First Floor" ? 0 : 1) : 0))
        {
            return Vector2Int.zero;
        }
        
        RoomInstance lastRoom = placedRooms[placedRooms.Count-1];
        return new Vector2Int(
            lastRoom.gridPosition.x + lastRoom.dimensions.x,
            0
        );
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
        
        // Return any random layout if stairs aren't required
        return roomType.layouts[Random.Range(0, roomType.layouts.Length)];
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