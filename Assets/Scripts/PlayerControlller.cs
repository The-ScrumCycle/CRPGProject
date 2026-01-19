using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float mouseSpeed = 20.0f;
    public float playerSpeed = 5f;
    public float rotateSpeed = 20f;
    private float rotationNum = 0;
    private Vector3 distanceUP = new Vector3(0, 5f, 10f);
    private Quaternion cameraRotation = Quaternion.Euler(60f, 180f, 0f);

    [Header("Camera")]
    public float zoom = 13f;
    public float cameraSpeed = 20f;
    bool freeCamera = true;
    public float minZoom   = 5f;
    public float maxZoom   = 20f;
    public float zoomSpeed = 0.4f;

    [Header("Components")]
    private Renderer playerRenderer;
    public CharacterController characterController;
    public Transform cameraTransform;
    public Animator characterAnimator;
    public Camera cam;
    public LayerMask groundMask;
    public UnityEngine.AI.NavMeshAgent agent;


    void Awake()
    {
        // Grab player renderer
        playerRenderer = GetComponent<Renderer>();

    }


    void Start()
    {
        // The camera goes to it's default position
        SetLockCamera();
        cameraTransform.rotation = Quaternion.Euler(60f, 180f, 0f);


        // water is not walkable 
        int water = UnityEngine.AI.NavMesh.GetAreaFromName("Water");
        agent.areaMask &= ~(1 << water);
    }

    // Update is called once per frame
    void Update()
    {
        // set camera mode
        zoomController();
        if (Input.GetKeyDown("q"))
        {
            freeCamera = !freeCamera;
        }
        if (!freeCamera)
        {
            SetLockCamera();
        } else
        {
            if (Input.GetKeyDown("q"))
            {
                cameraTransform.rotation = cameraRotation;
            }
            setFreeCamera();
        }

        // player movimentation 
        HandleClickMove();
        UpdateAnimator();
        agent.speed = playerSpeed;

    }

    // free camera, player can move independently of the camera
    void setFreeCamera()
    {
        Vector3 movement = Vector3.zero;

        // fix camera
        Vector3 camUP = cameraTransform.up;
        camUP.Normalize();

        Vector3 camRight = cameraTransform.right;
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
        cameraTransform.position = cameraTransform.position + movement;
        cameraTransform.position = new Vector3(cameraTransform.position.x, zoom, cameraTransform.position.z);

        // camera turn around 
        if (Input.GetKey("r"))
        {
            cameraTransform.RotateAround(transform.position, Vector3.up, rotateSpeed * Time.deltaTime);
            cameraRotation = cameraTransform.rotation;
        }
    }


    // the Camera is an isometric camera that follows the players around. follows the player around
    void SetLockCamera()
    {
        Vector3 playerPosition = transform.position;

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
        Debug.Log(rotationNum);
        cameraTransform.position = playerPosition + distanceUP;
        cameraTransform.LookAt(transform.position);

    }


    // the camera gets closer or farther with the mouse wheel
    void zoomController()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        // respect limits of the zoom
        if (Mathf.Abs(scroll) > 0)
        {
            float playerPositionY = this.transform.position.y;
            float minZ = playerPositionY + minZoom;
            float maxZ = playerPositionY + maxZoom;

            zoom -= scroll * zoomSpeed;
            zoom = Mathf.Clamp(zoom, minZ, maxZ);

        }

    }


    // player movimentation 
    private void HandleClickMove()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("hit tag: " + hit.collider.tag);
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


}