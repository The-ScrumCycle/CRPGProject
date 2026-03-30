using UnityEngine;

public class NPCController : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent agent;
    private PlayerController playerController;
    private Transform playerCharacter;
    [SerializeField] Animator characterAnimator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       characterAnimator = GetComponent<Animator>();
       agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAnimator();
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
