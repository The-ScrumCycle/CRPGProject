using UnityEngine;

public class CombatTransitionData : MonoBehaviour
{

    GameObject player;
    GameObject MainCamera;

    // Only one instance 
    public static CombatTransitionData Instance { get; private set; }

    // Store Player and main camera positions and rotations for transition back to exploration
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    public void SaveTransitionData()
    {
        playerPosition = player.transform.position;
        playerRotation = player.transform.rotation;
        cameraPosition = MainCamera.transform.position;
        cameraRotation = MainCamera.transform.rotation;
    }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

}
