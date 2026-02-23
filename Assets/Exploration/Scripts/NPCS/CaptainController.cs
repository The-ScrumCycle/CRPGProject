using UnityEngine;

public class CaptainController : MonoBehaviour
{

    [Header("Components")]
    public GameObject Player;   
    public PlayerController playerController;
    public ShipController shipController;
    public CameraController cameraController;


    void Start()
    {
        playerController = Player.GetComponent<PlayerController>();
        shipController = GameObject.FindAnyObjectByType<ShipController>();
        //cameraController = main.Camera.main.GetComponent<CameraController>();



    }

    // when I click on captain , I should be able to load in the boat
    void OnMouseDown ()
    {
        if (Vector3.Distance(Player.transform.position, transform.position) > 10f)
        {
            return;
        }

        Debug.Log("clicked");
        BoardShip();

    }

    // when I click on captain , I should be able to board the boat
    public void BoardShip()
    {
        playerController.SetControllable(false);
        shipController.SetControllable(true);
        cameraController.SetTarget(shipController.transform);
        cameraController.SetShipCamera();
    }

    // when I click on Docks, I should be able to leave the boat
    public void LeaveShip()
    {
        shipController.SetControllable(false);
        playerController.SetControllable(true);
        cameraController.SetTarget(playerController.transform);
    }



}
