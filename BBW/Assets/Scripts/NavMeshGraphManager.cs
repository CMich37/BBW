using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavMeshGraphManager : MonoBehaviour
{
    public static NavMeshGraphManager instance;

    private List<NavMeshGraphNode> nodes;

    void Awake()
    {
        instance = this;
        BuildGraph();
    }

    void BuildGraph()
    {
        nodes = new List<NavMeshGraphNode>();

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();

        // 1. Create nodes (1 per triangle)
        for (int i = 0; i < tri.indices.Length; i += 3)
        {
            Vector3 a = tri.vertices[tri.indices[i]];
            Vector3 b = tri.vertices[tri.indices[i + 1]];
            Vector3 c = tri.vertices[tri.indices[i + 2]];

            Vector3 center = (a + b + c) / 3f;
            nodes.Add(new NavMeshGraphNode(i / 3, center));
        }

        // 2. Connect neighbors (based on shared edges)
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                float dist = Vector3.Distance(nodes[i].center, nodes[j].center);
                if (dist < 3f) // adjustable based on mesh density
                {
                    nodes[i].neighbors.Add(nodes[j]);
                    nodes[j].neighbors.Add(nodes[i]);
                }
            }
        }
    }

    public List<Vector3> Dijkstra(Vector3 startPos, Vector3 endPos)
    {
        NavMeshGraphNode startNode = FindClosestNode(startPos);
        NavMeshGraphNode endNode = FindClosestNode(endPos);

        if (startNode == null || endNode == null) return null;

        foreach (var node in nodes)
        {
            node.distance = Mathf.Infinity;
            node.previous = null;
        }

        startNode.distance = 0;
        List<NavMeshGraphNode> open = new List<NavMeshGraphNode> { startNode };

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.distance.CompareTo(b.distance));
            var current = open[0];
            open.RemoveAt(0);

            if (current == endNode)
                break;

            foreach (var neighbor in current.neighbors)
            {
                float alt = current.distance + Vector3.Distance(current.center, neighbor.center);
                if (alt < neighbor.distance)
                {
                    neighbor.distance = alt;
                    neighbor.previous = current;
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }
        }

        // Reconstruct path
        List<Vector3> path = new List<Vector3>();
        var temp = endNode;
        while (temp != null)
        {
            path.Insert(0, temp.center);
            temp = temp.previous;
        }

        return path;
    }

    NavMeshGraphNode FindClosestNode(Vector3 pos)
    {
        NavMeshGraphNode closest = null;
        float minDist = Mathf.Infinity;
        foreach (var node in nodes)
        {
            float dist = Vector3.Distance(pos, node.center);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }
        return closest;
    }
}
