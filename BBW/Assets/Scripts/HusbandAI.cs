using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HusbandAI : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Target")]
    public Transform player;

    [Header("Suspicion")]
    public float suspicion = 0f;
    public float maxSuspicion = 100f;
    public float chaseThreshold = 75f;
    public float suspicionGainPerSecond = 25f;
    public float suspicionDecayPerSecond = 0f;
    public float detectionRange = 18f;
    public float fieldOfView = 90f;
    public LayerMask lineOfSightMask;

    [Header("Chase")]
    public float killRange = 1.5f;
    public float updateRate = 0.2f;

    private float updateTimer;
    private bool isChasing;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null)
        {
            Debug.LogError("HusbandAI: No player found.");
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("HusbandAI: Enemy is NOT on NavMesh.");
            enabled = false;
        }
    }

    void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;

        UpdateSuspicion();

        if (suspicion >= chaseThreshold)
            isChasing = true;

        if (isChasing)
            ChasePlayer();
    }

    void UpdateSuspicion()
    {
        if (CanSeePlayer())
        {
            suspicion += suspicionGainPerSecond * Time.deltaTime;
        }
        else
        {
            suspicion -= suspicionDecayPerSecond * Time.deltaTime;
        }

        suspicion = Mathf.Clamp(suspicion, 0f, maxSuspicion);
    }

    bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > detectionRange)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);

        if (angle > fieldOfView * 0.5f)
            return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 targetPos = player.position + Vector3.up * 1.2f;

        if (Physics.Raycast(eyePos, targetPos - eyePos, out RaycastHit hit, detectionRange, lineOfSightMask))
        {
            return hit.transform == player || hit.transform.CompareTag("Player");
        }

        return false;
    }

    void ChasePlayer()
    {
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateRate)
        {
            agent.SetDestination(player.position);
            updateTimer = 0f;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= killRange)
            KillPlayer();
    }

    void KillPlayer()
    {
        Debug.Log("The husband caught you.");
        Time.timeScale = 0f;
    }
}