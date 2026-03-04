using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;


// Camera controller script
public class CameraController : MonoBehaviour
{

    [Header("Target")]
    [SerializeField] private Transform target;


    [Header("Camera")]
    [SerializeField] private float zoom = 13f;
    [SerializeField] private float cameraSpeed = 20f;
    private bool freeCamera = true;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomSpeed = 0.4f;
    [SerializeField] private float rotateSpeed = 20f;
    private float rotationNum = 0;
    private Vector3 distanceUP = new Vector3(0, 5f, 10f);
    private Quaternion cameraRotation = Quaternion.Euler(60f, 180f, 0f);



    void Start()
    {
        // The camera goes to it's default position
        transform.rotation = Quaternion.Euler(60f, 180f, 0f);
        if (target != null)
            SetLockCamera();

        target = GameObject.FindGameObjectWithTag("Player").transform;
    }


    void Update()
    {
        // if we don't have a target, return 
        if (target == null) return;

        // set camera mode
        zoomController();
        if (Input.GetKeyDown("q"))
        {
            freeCamera = !freeCamera;
        }
        if (!freeCamera)
        {
            SetLockCamera();
        }
        else
        {
            if (Input.GetKeyDown("q"))
            {
                transform.rotation = cameraRotation;
            }
            setFreeCamera();
        }
    }


    // to switch between player / ship as focus of the camera
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null) SetLockCamera();
    }


    // free camera, player can move independently of the camera
    void setFreeCamera()
    {
        Vector3 movement = Vector3.zero;

        // fix camera
        Vector3 camUP = transform.up;
        camUP.Normalize();

        Vector3 camRight = transform.right;
        camRight.Normalize();

        // WASD 
        if (Input.GetKey("w"))
        {
            movement += camUP;
        }
        if (Input.GetKey("s"))
        {
            movement -= camUP;
        }
        if (Input.GetKey("a"))
        {
            movement -= camRight;
        }
        if (Input.GetKey("d"))
        {
            movement += camRight;
        }

        // apply speed boost
        movement.y = 0f;
        movement = movement.normalized * cameraSpeed * Time.deltaTime;

        // new position
        transform.position = transform.position + movement;
        transform.position = new Vector3(transform.position.x, zoom, transform.position.z);

        // camera turn around 
        if (Input.GetKey("r"))
        {
            transform.RotateAround(target.position, Vector3.up, rotateSpeed * Time.deltaTime);
            cameraRotation = transform.rotation;
        }
    }


    // the Camera is an isometric camera that follows the players around. follows the player around
    void SetLockCamera()
    {
        Vector3 targetPosition = target.position;

        // camera turn around 
        if (Input.GetKeyDown("r"))
        {
            rotationNum = rotationNum + 1;
            // set standard again 
            if (rotationNum == 4)
            {
                rotationNum = 0;
            }
        }

        // back
        if (rotationNum == 0)
        {
            distanceUP = new Vector3(0, zoom, 10f);
        }

        // in front
        if (rotationNum == 1)
        {
            distanceUP = new Vector3(0, zoom, -10f);
        }

        // in Left
        if (rotationNum == 2)
        {
            distanceUP = new Vector3(-10f, zoom, 0f);
        }

        // in Right
        if (rotationNum == 3)
        {
            distanceUP = new Vector3(10f, zoom, 0f);
        }


        // Camera position
        transform.position = targetPosition + distanceUP;
        transform.LookAt(target.position);

    }


    // the camera gets closer or farther with the mouse wheel
    void zoomController()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        // respect limits of the zoom
        if (Mathf.Abs(scroll) > 0)
        {
            float playerPositionY = target.position.y;
            float minZ = playerPositionY + minZoom;
            float maxZ = playerPositionY + maxZoom;

            zoom -= scroll * zoomSpeed;
            zoom = Mathf.Clamp(zoom, minZ, maxZ);

        }

    }
    

    // set camera mode for ship settings ( change settings for ship)
   public void SetShipCamera()
    {
        zoom = 40f;
        cameraSpeed = 60f;
        minZoom = 30f;
        maxZoom = 150f;
        zoomSpeed = 3f;
        rotateSpeed = 90f;
    }

}
