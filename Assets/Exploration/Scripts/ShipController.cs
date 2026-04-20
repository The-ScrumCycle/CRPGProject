using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Core.Save;

public class ShipController : MonoBehaviour, ISaveable
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
        // Only Water is walkable
        //int water = UnityEngine.AI.NavMesh.GetAreaFromName("Water");
        //agent.areaMask = 1 << water;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
        }
    }

    public bool IsControllable()
    {
        return isControllable;
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
        if (agent == null || cam == null) return;
        if (!agent.isOnNavMesh) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            Debug.Log("clicking");

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, waterMask, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("hit water");

                if (!UnityEngine.AI.NavMesh.SamplePosition(hit.point, out UnityEngine.AI.NavMeshHit navHit, 10f, agent.areaMask))
                    return;

                if (Vector3.Distance(agent.nextPosition, navHit.position) < 0.15f)
                    return;

                UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();

                bool foundPath = UnityEngine.AI.NavMesh.CalculatePath(
                    agent.nextPosition,
                    navHit.position,
                    agent.areaMask,
                    path
                );

                if (foundPath && path.status != UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                {
                    agent.isStopped = false;
                    agent.ResetPath();
                    agent.SetPath(path);
                }
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

    public void SetSaveData(SaveData saveData)
    {
        saveData.ship.position = transform.position;
        saveData.ship.rotation = transform.rotation;
        saveData.ship.controllable = isControllable;
    }

    public void LoadSaveData(SaveData saveData)
    {
        if (agent != null && agent.enabled)
        {
            agent.Warp(saveData.ship.position);
        }
        else
        {
            transform.position = saveData.ship.position;
        }
        transform.rotation = saveData.ship.rotation;
        SetControllable(saveData.ship.controllable);
    }


}