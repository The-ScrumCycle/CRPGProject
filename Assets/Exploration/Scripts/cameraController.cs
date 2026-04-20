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
    [SerializeField] private float lockOffset = 10f;
    [SerializeField] private float angle = 60f;
    private float rotationNum = 0;
    private Vector3 distanceUP = new Vector3(0, 5f, 10f);
    private Quaternion cameraRotation = Quaternion.Euler(60f, 180f, 0f);
    // flag to block camera movement when dialogue is open
    private bool inputBlocked = false;

    // Removed duplicate fields and added saved player settings
    private float savedPlayerZoom = 13f;
    private float savedPlayerCameraSpeed = 20f;
    private float savedPlayerMinZoom = 5f;
    private float savedPlayerMaxZoom = 20f;
    private float savedPlayerZoomSpeed = 0.4f;
    private float savedPlayerRotateSpeed = 20f;
    private float savedPlayerLockOffset = 10f;
    private float savedPlayerAngle = 60f;



    void Start()
    {
        // The camera goes to it's default position
        target = GameObject.FindGameObjectWithTag("Player").transform;
        if (target != null)
            SetLockCamera();
            transform.rotation = Quaternion.Euler(60f, 180f, 0f);

    }


    void Update()
    {
        if (target == null) return;
        if (inputBlocked) return;

        zoomController();

        if (Input.GetKeyDown("q"))
        {
            freeCamera = !freeCamera;
            if (freeCamera)
            {
                transform.rotation = cameraRotation;
            }
        }

        if (!freeCamera)
        {
            SetLockCamera();
        }
        else
        {
            setFreeCamera();
        }
    }

    // to block camera movement when dialogue is open
    public void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;
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

        else if (Input.GetKey("e"))
        {
            transform.RotateAround(target.position, -Vector3.up, rotateSpeed * Time.deltaTime);
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
            distanceUP = new Vector3(0, zoom, lockOffset);
        }

        // in front
        if (rotationNum == 1)
        {
            distanceUP = new Vector3(0, zoom, -lockOffset);
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

        // ai code here 
        float yRot = 180f;
        if (rotationNum == 1) yRot = 0f;
        if (rotationNum == 2) yRot = 90f;
        if (rotationNum == 3) yRot = 270f;
        transform.rotation = Quaternion.Euler(angle, yRot, 0f);

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
    
    // quickly make camera go back to player 
    public void GoToPlayer()
    {
        Vector3 targetPosition = target.position;
        transform.position = targetPosition;
        SetLockCamera();
        zoom = 15f;
    }

    // set camera mode for ship settings ( change settings for ship)
    public void SetShipCamera()
    {
        // save current settings for player camera
        savedPlayerZoom = zoom;
        savedPlayerCameraSpeed = cameraSpeed;
        savedPlayerMinZoom = minZoom;
        savedPlayerMaxZoom = maxZoom;
        savedPlayerZoomSpeed = zoomSpeed;
        savedPlayerRotateSpeed = rotateSpeed;
        savedPlayerLockOffset = lockOffset;
        savedPlayerAngle = angle;

        // change to general ship settings
        zoom = 35f;
        cameraSpeed = 60f;
        minZoom = 30f;
        maxZoom = 150f;
        zoomSpeed = 3f;
        rotateSpeed = 90f;
        lockOffset = 35f;
        angle = 25f;

        cameraRotation = Quaternion.Euler(angle, 180f, 0f);
        SetLockCamera();
    }

    public void SetPlayerCamera()
    {
        // restore player camera settings
        zoom = savedPlayerZoom;
        cameraSpeed = savedPlayerCameraSpeed;
        minZoom = savedPlayerMinZoom;
        maxZoom = savedPlayerMaxZoom;
        zoomSpeed = savedPlayerZoomSpeed;
        rotateSpeed = savedPlayerRotateSpeed;
        lockOffset = savedPlayerLockOffset;
        angle = savedPlayerAngle;

        cameraRotation = Quaternion.Euler(angle, 180f, 0f);
        SetLockCamera();
    }



}
