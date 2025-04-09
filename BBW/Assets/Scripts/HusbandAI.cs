using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HusbandAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerController;

    [Header("Settings")]
    public float chaseRange = 25f;
    public float killRange = 1.5f;
    public float updateRate = 0.25f; // How often he updates his path

    private float updateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Only chase if within range
        if (distance <= chaseRange)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateRate)
            {
                agent.SetDestination(player.position);
                updateTimer = 0;
            }

            // Caught the player
            if (distance <= killRange)
            {
                KillPlayer();
            }
        }
        else
        {
            agent.ResetPath();
        }
    }

    void KillPlayer()
    {
        Debug.Log("The husband caught you. You're dead.");
        // You could add playerController.Die() here or trigger animation
        Time.timeScale = 0f; // Freeze game
    }
}
