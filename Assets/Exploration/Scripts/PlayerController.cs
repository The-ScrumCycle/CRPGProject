using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

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
    public NavMeshAgent agent;

    public static PlayerController Instance { get; private set; }



    void Awake()
    {
        // Grab player renderer
        playerRenderer = GetComponent<Renderer>();
        if (cam == null) cam = Camera.main;


        // Player should not be destroyed on scene load and should be only one instance. 
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        cam = Camera.main;

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

    // standard Unity functions that handle scene loading
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Exploration")
            return;
        cam = Camera.main;
    }

}