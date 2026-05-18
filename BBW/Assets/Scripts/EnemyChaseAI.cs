using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChaseAI : MonoBehaviour
{
    public Transform player;
    public float repathRate = 0.2f;
    public float chaseRange = 999f;

    private NavMeshAgent agent;
    private float repathTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > chaseRange)
            return;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            agent.SetDestination(player.position);
            repathTimer = repathRate;
        }
    }
}
