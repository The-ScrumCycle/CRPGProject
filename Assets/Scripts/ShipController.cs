using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float shipSpeed = 12f;
    [SerializeField] private bool isControllable = false;

    [Header("Components")]
    private Renderer shipRenderer;
    public Camera cam;
    public LayerMask waterMask;
    public UnityEngine.AI.NavMeshAgent agent;


    void Awake()
    {
        // Grab ship renderer
        shipRenderer = GetComponent<Renderer>();
        if (cam == null) cam = Camera.main;

    }

    void Start()
    {
        // Ground is not walkable 
        //int Ground = UnityEngine.AI.NavMesh.GetAreaFromName("Ground");
        //agent.areaMask &= ~(1 << Ground);

    }

    // Update is called once per frame
    void Update()
    {
        // if Ship is not controllable, return
        if (!isControllable) return;


        // ship movimentation 
        HandleClickMove();
        updateShipSpeed();

    }

    // ship movimentation 
    private void HandleClickMove()
    {
        // if we don't have a camera or agent, return
        if (agent == null || cam == null) return;

        // if left mouse button is pressed, move the player to the clicked position
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, waterMask, QueryTriggerInteraction.Ignore))
            {
                agent.SetDestination(hit.point);
            }
        }
    }

    // update ship speed
    private void updateShipSpeed()
    {
        agent.speed = shipSpeed;
    }

    // set if the ship is controllable or not
    public void SetControllable(bool controllable)
    {
        isControllable = controllable;
        if (!controllable)
        {
            agent.ResetPath();
        }
    }


}