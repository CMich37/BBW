using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "RoomLayoutSO", menuName = "Scriptable Objects/RoomLayoutSO")]
public class RoomLayout : ScriptableObject
{
    public GameObject prefab;
    public Vector2 dimensions; // Width x Length in Unity units
    public bool hasStairs; // Only used for foyer variants
    public float weight = 0;

    [Header("Walkable Sides")]
    [Tooltip("Which sides a player can cross. Order: [0]=+X (Right), [1]=-X (Left), [2]=+Z (Front), [3]=-Z (Back)")]
    public bool[] walkableSides = new bool[4] { true, true, true, true };
}