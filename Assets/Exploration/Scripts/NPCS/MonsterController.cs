using UnityEngine;
using UnityEngine.AI;
using Game.Core;
using Game.Combat;

public class MonsterController : MonoBehaviour
{
    [Header("Movement & Patrol")]
    public float monsterSpeed = 3.0f;
    public float patrolRadius = 15f;
    public float waitTimeMax = 3.0f;
    public float stoppingDistance = 1f;

    [Header("Visibility")]
    public float FOV = 125.0f; //angle of vision
    public LayerMask obstaclesMask;

    [Header("Search & Return")]
    public float searchLength = 5.0f;
    public float maxDistanceFromBase = 30f;

    [Header("Targets")]
    public Transform playerCharacter;


    //Initial states of monster
    private Vector3 basePosition; // monster spawn point
    private float patrolTimer = 0f;
    private float searchTimer = 0f;
    private bool isSearching = false;
    private bool isChasing = false;

    [Header("Ennemy Level and XP")]
    [SerializeField]  int enemyLevel = 12;
    [SerializeField] int xpGiven = 400;//xp dropped by monster


    private NavMeshAgent agent;
    private PlayerController playerController;
    GameStateManager gameStateManager;
    [SerializeField] Animator characterAnimator;

    void Awake()
    {
        //initializes components
        agent = GetComponent<NavMeshAgent>();
        agent.speed = monsterSpeed;
        basePosition = transform.position;

                
        // Updating Monster stopping distance 
        agent.stoppingDistance = stoppingDistance;

        gameStateManager = FindAnyObjectByType<GameStateManager>();

    }

    void Start()
    {
        GetPlayer(); // locate player on scene

        string id = GetComponent<EnemyID>().getEnemyID();

        // Kill monster if he is already dead
        if (EnnemiesState.Instance.IsEnnemyDead(id))
        {
            Destroy(gameObject);
        }
    }

    //finding the player
    private void GetPlayer()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null)
        {
            //just a safety thing
            //playerCharacter = GameObject.FindGameObjectWithTag("Player").transform;
            //this fix is for load save
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerCharacter = playerObj.transform;
            }

        }
        if (playerCharacter == null && playerController != null)
        {
            //if both empty, player position = position of object with the script attached to it
            playerCharacter = playerController.transform;
        }
    }

    void Update()
    {
        if (playerCharacter == null) GetPlayer();
        if (playerCharacter == null) return;
        if (playerController.GetInDialogue()) return;

        UpdateAnimator();
        //if monster can see player
        bool canSee = CheckVisibility();

        if (canSee)
        {
            //start chasing if cansee
            isChasing = true;
            isSearching = false;
            //live update placement of player
            agent.SetDestination(playerCharacter.position);

            //Basically if the ennemy touches the player
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                Debug.Log("Enemy attacked you!");
                GameStateManager.Instance.TransitionToCombat(gameObject);
            }

            // If monster goes too far from base it gives up
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
        //checks for new patrol point if standing still
        if (!agent.hasPath || agent.remainingDistance < 1f)
        {
            patrolTimer += Time.deltaTime;
            //randomize patrol 
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

    // When the monster loses sight of the player it enters a search mode where it rotates in place for a certain duration, trying to find the player again. 
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


    // for combat transition data
    public int GetEnemyLevel()
    {
        return enemyLevel;
    }

    public int GetXPGiven()
    {
        return xpGiven;
    }

    private void UpdateAnimator()
    {
        if (characterAnimator == null || agent == null) return;

        // speed of the player
        float speed = agent.velocity.magnitude;

        characterAnimator.SetFloat("Speed", speed);
    }

 

}
