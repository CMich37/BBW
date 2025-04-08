using UnityEngine;
using System.Collections.Generic;

public class NavMeshGraphNode
{
    public int index; // Index of the triangle
    public Vector3 center;
    public List<NavMeshGraphNode> neighbors = new List<NavMeshGraphNode>();

    public float distance = Mathf.Infinity;
    public NavMeshGraphNode previous = null;

    public NavMeshGraphNode(int i, Vector3 pos)
    {
        index = i;
        center = pos;
    }
}
