using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// Main Script that controls Followers actions exploration the overworld
public class FollowerController : MonoBehaviour
{
    [Header("Party Settings")]
    [SerializeField] private bool inParty = false;
    [SerializeField] public FollowerID followerID;

    [Header("Distance Settings")]
    [SerializeField] float maxDistanceToPlayer = 50f;
    [SerializeField] float minDistanceToPlayer = 4f;
    [SerializeField] float acceptableDistance  = 10f;
    [SerializeField] float DistanceRadius      = 10f;

    [Header("Movimentation Settings")]
    [SerializeField] float followSpeed         = 5f;
    [SerializeField] float WanderSpeed         = 5f;
    [SerializeField] float wanderRadius        = 10f;
    [SerializeField] float wanderTimer         = 8f;
    [SerializeField] float Wandertime          = 0f;
    
    private NavMeshAgent agent;
    private PlayerController playerController;
    private Transform playerCharacter;
    [SerializeField] Animator characterAnimator;

    void Awake()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        agent            = GetComponent<UnityEngine.AI.NavMeshAgent>();
        playerCharacter  = playerController.transform;
        followSpeed      = playerController.playerSpeed;
        WanderSpeed      = playerController.playerSpeed * 0.7f;
        agent.speed      = followSpeed;

        // testing, add followers to party
        //PartyManager.Instance.AddFollowerActive(followerID);


    }

    void Update()
    {
        CheckInPartyStatus();
        if (!inParty) return;
        if (playerCharacter == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, playerCharacter.position);
        UpdateAnimator();
        GetCloseToPlayer(distanceToPlayer);
        FollowPlayer(distanceToPlayer);
        WanderAround(distanceToPlayer);
    }

    void CheckInPartyStatus()
    {
        inParty = PartyManager.Instance.IsFollowerActive(followerID);
    }

    bool CloseToPlayer(float distanceToPlayer)
    {
        return distanceToPlayer <= acceptableDistance;
    }

    // if the follower is too far from the player, he teleports close to the player
    void GetCloseToPlayer(float distanceToPlayer)
    {
        if(distanceToPlayer > maxDistanceToPlayer)
        {
            Vector3 newPosition  = playerCharacter.position + Random.insideUnitSphere * DistanceRadius;
            newPosition = new Vector3(newPosition.x, playerCharacter.position.y, newPosition.z);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newPosition, out hit, DistanceRadius, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    // follower has agent so can use agent functions 
    void FollowPlayer(float distanceToPlayer)
    {
        if (!CloseToPlayer(distanceToPlayer))
        {
            agent.stoppingDistance = minDistanceToPlayer;
            agent.speed = followSpeed;
            agent.SetDestination(playerCharacter.position);
        }
  
    }

    // if close to player, the follower can wander around after a certain time 
    void WanderAround(float distanceToPlayer)
    {
        if(!CloseToPlayer(distanceToPlayer)) return;
        Wandertime += Time.deltaTime;
        if (Wandertime >= wanderTimer)
        {
            Debug.Log("Wandering around...");
            agent.stoppingDistance = 0f;
            agent.speed = WanderSpeed;
            Vector3 newPosition = playerCharacter.position + Random.insideUnitSphere * wanderRadius;
            newPosition = new Vector3(newPosition.x, playerCharacter.position.y, newPosition.z);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newPosition, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            Wandertime = 0f;
        }
    }

    // so the animator knows if the character is running or not
    private void UpdateAnimator()
    {
        if (characterAnimator == null || agent == null) return;

        // speed of the follower
        float speed = agent.velocity.magnitude;

        characterAnimator.SetFloat("Speed", speed);
    }





}
