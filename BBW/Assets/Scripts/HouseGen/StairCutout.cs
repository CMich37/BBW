using UnityEngine;

public class StairCutout : MonoBehaviour
{
    [Tooltip("Trigger box that defines the opening needed on the floor above.")]
    public BoxCollider cutoutTrigger;

    public Bounds WorldBounds
    {
        get
        {
            if (cutoutTrigger == null) return new Bounds(transform.position, Vector3.zero);
            return cutoutTrigger.bounds; // world-space bounds
        }
    }
}
