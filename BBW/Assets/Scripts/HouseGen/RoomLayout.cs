using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "RoomLayoutSO", menuName = "Scriptable Objects/RoomLayoutSO")]
public class RoomLayout : ScriptableObject
{
    public GameObject prefab;
    public Vector2Int dimensions; // Width x Length in Unity units
    public bool hasStairs; // Only used for foyer variants
}