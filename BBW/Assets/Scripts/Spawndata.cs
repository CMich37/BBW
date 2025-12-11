using UnityEngine;
using System.Collections.Generic;
public enum FurnitureType
{
    Cabinet, Lshelf, Sshelf, Dresser, Fridge,
    Closet, Nstand, Table, Sink, Desk, Ctable
}

[CreateAssetMenu(fileName = "SO_SpawnData", menuName = "Spawn System/Spawn Data")]
public class SpawnData : ScriptableObject
{
    public FurnitureType furnitureType;
    public List<GameObject> itemPrefabs = new List<GameObject>();
    public List<float> weights = new List<float>();
    public List<Vector3> localSpawnOffsets = new List<Vector3>();
}
