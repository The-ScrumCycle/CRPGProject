using UnityEngine;

public class DockController : MonoBehaviour
{
    [Header("Leave Ship Settings")]
    public Transform playerPoint;
    public Transform captainPoint;
    public Transform shipPoint;

    [Header("Controllers")]
    public GameObject player;
    public GameObject captain; 
    public PlayerController playerController;
    public ShipController shipController;
    public CameraController cameraController;

    // get controllers and game objects
    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null && playerController == null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        if (shipController == null)
        {
            shipController = FindObjectOfType<ShipController>();
        }

        if (captain == null)
        {
            captain = GameObject.FindGameObjectWithTag("Captain");
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        ShipController Ship = other.GetComponentInParent<ShipController>();

        if (Ship != null && Ship == shipController)
        {
            LeaveShip();
            Debug.Log("Player has left the ship and is now on the dock.");
        }
    }

    private void LeaveShip()
    {
        player.SetActive(true);
        captain.SetActive(true);

        // teleport player, ship and captain to the dock position.
        if (player != null && playerPoint != null)
        {
            UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(playerPoint.position);
            }
            else
            {
                player.transform.position = playerPoint.position;
            }
        }

        if (captain != null && captainPoint != null)
        {
            captain.transform.position = captainPoint.position;
        }
        
        if (shipController != null && shipPoint != null)
        {
            UnityEngine.AI.NavMeshAgent agent = shipController.GetComponent<UnityEngine.AI.NavMeshAgent>();
            agent.ResetPath(); 
            agent.Warp(shipPoint.position);
        }


        // set control and camera back to player
        shipController.SetControllable(false);
        playerController.SetControllable(true);
        cameraController.SetTarget(player.transform);
        cameraController.SetPlayerCamera();
    }
}
