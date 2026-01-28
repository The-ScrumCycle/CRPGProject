using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    [Header("Movement & Patrol")]
    public float monsterSpeed = 5.0f;
    public float patrolRadius = 15f;
    public float waitTimeMax = 3.0f;

    [Header("Visibility")]
    public float FOV = 125.0f;
    public LayerMask obstaclesMask;

    [Header("Search & Return")]
    public float searchLength = 5.0f;
    public float maxDistanceFromBase = 30f;

    [Header("Targets")]
    // ADD THIS LINE to make the slot appear in the Inspector
    public Transform playerCharacter;

    private Vector3 basePosition;
    private float patrolTimer = 0f;
    private float searchTimer = 0f;
    private bool isSearching = false;
    private bool isChasing = false;

    private NavMeshAgent agent;
    private PlayerController playerController;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = monsterSpeed;
        basePosition = transform.position;

        // Try to find the script automatically if not assigned
        playerController = FindAnyObjectByType<PlayerController>();

        // If you drag the player into the new slot, this ensures we use that object
        if (playerCharacter == null && playerController != null)
        {
            playerCharacter = playerController.transform;
        }
    }

    void Update()
    {
        // Safety check: Don't run logic if player is missing
        if (playerCharacter == null) return;

        bool canSee = CheckVisibility();

        if (canSee)
        {
            isChasing = true;
            isSearching = false;
            agent.SetDestination(playerCharacter.position);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                Debug.Log("Enemy attacked you!");
            }

            if (Vector3.Distance(transform.position, basePosition) > maxDistanceFromBase)
            {
                StopChase();
            }
        }
        else if (isChasing)
        {
            SearchBehavior();
        }
        else
        {
            PatrolBehavior();
        }
    }

    void PatrolBehavior()
    {
        if (!agent.hasPath || agent.remainingDistance < 1f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= Random.Range(1f, waitTimeMax))
            {
                Vector3 randomPoint = basePosition + Random.insideUnitSphere * patrolRadius;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, 1))
                {
                    agent.SetDestination(hit.position);
                    patrolTimer = 0;
                }
            }
        }
    }

    void SearchBehavior()
    {
        if (!isSearching)
        {
            isSearching = true;
            searchTimer = 0f;
            agent.ResetPath();
            Debug.Log("Lost track... searching.");
        }

        searchTimer += Time.deltaTime;
        transform.Rotate(0, 120 * Time.deltaTime, 0);

        if (searchTimer >= searchLength)
        {
            StopChase();
        }
    }

    void StopChase()
    {
        isChasing = false;
        isSearching = false;
        Debug.Log("Going back to base.");
        agent.SetDestination(basePosition);
    }

    bool CheckVisibility()
    {
        if (playerController == null || !playerController.GetVisibleStatus()) return false;

        Vector3 dir = (playerCharacter.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dir) < FOV * 0.5f)
        {
            float dist = Vector3.Distance(transform.position, playerCharacter.position);
            // Ensure Obstacles Mask is set to 'Default' or 'Environment' in inspector
            return !Physics.Raycast(transform.position + Vector3.up, dir, dist, obstaclesMask);
        }
        return false;
    }
}