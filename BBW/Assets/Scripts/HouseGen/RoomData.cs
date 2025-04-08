using UnityEngine;

[System.Serializable]
public class RoomData
{
    public string roomName; // "LivingRoom", "Bathroom", etc.
    public GameObject[] prefabVariants; // A/B/C layouts
    public Material wallMaterial;
    public bool canHaveBasementEntrance;
    public bool canHaveAtticEntrance;
    public bool isFirstFloorOnly;
}
