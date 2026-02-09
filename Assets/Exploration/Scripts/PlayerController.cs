using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float playerSpeed = 5f;
    [SerializeField] private bool isControllable = true;

    [Header("Visibility")]
    [SerializeField] private bool isVisible = true;


    [Header("Components")]
    private Renderer playerRenderer;
    public Animator characterAnimator;
    public Camera cam;
    public LayerMask groundMask;
    public UnityEngine.AI.NavMeshAgent agent;



    void Awake()
    {
        // Grab player renderer
        playerRenderer = GetComponent<Renderer>();
        if (cam == null) cam = Camera.main;


        // Player should not be destroyed on scene load
        DontDestroyOnLoad(gameObject);

    }

    void Start()
    {
        // water is not walkable 
        int water = UnityEngine.AI.NavMesh.GetAreaFromName("Water");
        agent.areaMask &= ~(1 << water);
    }

    // Update is called once per frame
    void Update()
    {
        // if Player is not controllable, return
        if (!isControllable) return;


        // player movimentation 
        HandleClickMove();
        UpdateAnimator();
        updatePlayerSpeed();

    }

    // player movimentation 
    private void HandleClickMove()
    {
        // if we don't have a camera or agent, return
        if (agent == null || cam == null) return;

        // if left mouse button is pressed, move the player to the clicked position
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.CompareTag("Water"))
                {
                    return;
                }
                agent.SetDestination(hit.point);
            }
        }
    }

    // so the animator knows if the character is running or not
    private void UpdateAnimator()
    {
        if (characterAnimator == null || agent == null) return;

        // speed of the player
        float speed = agent.velocity.magnitude;

        characterAnimator.SetFloat("Speed", speed);
    }

    // update player speed
    private void updatePlayerSpeed()
    {
        agent.speed = playerSpeed;
    }

    // set if the player is controllable or not
    public void SetControllable(bool controllable)
    {
        isControllable = controllable;
        if(!controllable)
        {
            agent.ResetPath();
        }
    }

    //Check if player is visible or not
    public bool GetVisibleStatus()
    {
        return isVisible;
    }

    


}