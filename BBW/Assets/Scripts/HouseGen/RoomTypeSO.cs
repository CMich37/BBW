using UnityEngine;

[CreateAssetMenu(fileName = "RoomTypeSO", menuName = "Scriptable Objects/RoomTypeSO")]
public class RoomTypeSO : ScriptableObject
{
    public string roomName;
    public RoomLayout[] layouts;
    public bool mustBeOnFirstFloor;
}
