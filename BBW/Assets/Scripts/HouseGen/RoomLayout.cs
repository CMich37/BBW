using UnityEngine;

[System.Serializable]
public class RoomLayout
{
    public GameObject prefab;
    public Vector2Int dimensions; // Width x Length in Unity units
    public bool hasStairs; // Only used for foyer variants
}