using UnityEngine;

public enum FurnitureType
{
    Cabinet, Lshelf, Sshelf, Dresser, Fridge,
    Closet, Nstand, Table, Sink, Desk, Ctable
}

[CreateAssetMenu(fileName = "SO_SpawnData", menuName = "Spawn System/Spawn Data")]
public class SpawnData : ScriptableObject
{
    public FurnitureType furnitureType;
}
