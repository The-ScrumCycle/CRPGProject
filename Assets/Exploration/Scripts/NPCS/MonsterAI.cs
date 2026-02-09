using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    [Header("Movement & Patrol")]
    public float monsterSpeed = 3.0f;
    public float patrolRadius = 15f;
    public float waitTimeMax = 3.0f;
    public float stoppingDistance = 1f;

    [Header("Visibility")]
    public float FOV = 125.0f;
    public LayerMask obstaclesMask;

    [Header("Search & Return")]
    public float searchLength = 5.0f;
    public float maxDistanceFromBase = 30f;

    [Header("Targets")]
    public Transform playerCharacter;

    private Vector3 basePosition;
    private float patrolTimer = 0f;
    private float searchTimer = 0f;
    private bool isSearching = false;
    private bool isChasing = false;

    private NavMeshAgent agent;
    private PlayerController playerController;
    GameStateManager gameStateManager;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = monsterSpeed;
        basePosition = transform.position;

        playerController = FindAnyObjectByType<PlayerController>();
        if(playerController == null)
        {
            playerCharacter = GameObject.FindGameObjectWithTag("Player").transform;
        }

        playerCharacter = playerController.transform;
        if (playerCharacter == null && playerController != null)
        {
            playerCharacter = playerController.transform;
        }


        // Updating Monster stopping distance 
        agent.stoppingDistance = stoppingDistance;

        gameStateManager = FindAnyObjectByType<GameStateManager>();

        EnnemiesState.Instance.AddEnnemy(gameObject);

    }

    void Update()
    {
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
                GameStateManager.Instance.TransitionToCombat(gameObject);
            }

            // If monster goes too far from base, it gives up
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

    // Monster patrols around its base position within a defined radius, waiting for a random time before moving to the next point.
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

    // When the monster loses sight of the player, it enters a search mode where it rotates in place for a certain duration, trying to find the player again. 
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

    // If the monster fails to find the player after searching, it stops chasing and returns to its base position.
    void StopChase()
    {
        isChasing = false;
        isSearching = false;
        Debug.Log("Going back to base.");
        agent.SetDestination(basePosition);
    }

    // The monster checks if the player is within its field of view and not obstructed by any obstacles. 
    bool CheckVisibility()
    {
        if (playerController == null || !playerController.GetVisibleStatus()) return false;

       //Checks if player is in monsters area
        float distanceToPlayer = Vector3.Distance(transform.position, playerCharacter.position);
        if (distanceToPlayer > patrolRadius)
            return false;

        Vector3 dir = (playerCharacter.position - transform.position).normalized;
       
        if (Vector3.Angle(transform.forward, dir) < FOV * 0.5f)
        {
            return !Physics.Raycast(transform.position + Vector3.up, dir, distanceToPlayer, obstaclesMask);
        }
        return false;
    }

}