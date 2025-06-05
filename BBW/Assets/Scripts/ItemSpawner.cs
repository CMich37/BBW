
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]

public class ShelfItemSpawner : MonoBehaviour
{
    public SpawnData spawnData;
    public List<ItemSpawnRule> spawnRules;

    void Start() => TrySpawnItems();

    private void TrySpawnItems()
    {
        var type = spawnData.furnitureType;
        var offsets = GetOffsetsForFurniture(type);

        foreach (var offset in offsets)
        {
            var validItems = spawnRules
                .Where(rule =>
                    GlobalItemSpawnTracker.GetSpawnCount(rule.prefab.name) < rule.maxGlobalSpawns)
                .ToList();

            if (validItems.Count == 0) return;

            var chosen = validItems[Random.Range(0, validItems.Count)];
            Vector3 spawnPos = transform.TransformPoint(offset);

            var go = Instantiate(chosen.prefab, spawnPos, Quaternion.identity);
            go.transform.SetParent(transform);
            GlobalItemSpawnTracker.RegisterSpawn(chosen.prefab.name);
        }
    }

    private List<Vector3> GetOffsetsForFurniture(FurnitureType type)
    {
        switch (type)
        {
            case FurnitureType.Lshelf:
                return new List<Vector3> {
                    new Vector3(-0.4f, 0.6f, 0),
                    new Vector3(0.4f, 0.6f, 0),
                };
            case FurnitureType.Dresser:
                return new List<Vector3> {
                    new Vector3(0, 0.5f, 0),
                };

            default:
                return new List<Vector3> { new Vector3(0, 0.5f, 0) };
        }
    }
}


    [System.Serializable]
    public class ItemSpawnRule
    {
        public GameObject prefab;
        public int minGlobalSpawns = 0;
        public int maxGlobalSpawns = 99;
    }
    public static class GlobalItemSpawnTracker
    {
        private static Dictionary<string, int> itemSpawnCounts = new Dictionary<string, int>();

        public static void RegisterSpawn(string itemName)
        {
            if (!itemSpawnCounts.ContainsKey(itemName))
                itemSpawnCounts[itemName] = 0;
            itemSpawnCounts[itemName]++;
        }

        public static int GetSpawnCount(string itemName)
        {
            return itemSpawnCounts.TryGetValue(itemName, out int count) ? count : 0;
        }
    }