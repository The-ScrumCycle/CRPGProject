using UnityEngine;
using UnityEngine.SceneManagement;
public class GameStateManager : MonoBehaviour
{
    public CombatTransitionData combatTransitionData;
    GameObject player;
    public PlayerController playerController;
    GameObject MainCamera;
    private bool returningFromCombat = false;

    // singleton instance of the GameStateManager
    public static GameStateManager Instance { get; private set; }

    // current state of the game
    public GameStates CurrentState { get; private set; } = GameStates.Exploration;

    // set up the state used.
    private void SetState(GameStates newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        Debug.Log("Game state changed to: " + CurrentState);
    }

    // get the current state of the game.
    public GameStates GetCurrentState()
    {
        Debug.Log("Current game state: " + CurrentState);
        return CurrentState;
    }

    // transition to combat state and scene
    public void TransitionToCombat(GameObject ennemy)
    {
        SetState(GameStates.Combat);

        // save player and camera positions/rotations
        combatTransitionData.SaveTransitionData(ennemy);
        playerController.agent.enabled = false;

        SceneManager.LoadScene("CombatScene");
        EnnemiesState.Instance.SetDeadEnnemy(ennemy);
    }

    // transition to exploration state and scene
    public void TransitionToExploration()
    {
        returningFromCombat = true;
        SetState(GameStates.Exploration);
        SceneManager.LoadScene("Exploration");

    }

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
        if (scene.name == "Exploration" && returningFromCombat)
        {
            // restore player and camera positions/rotations
            playerController.agent.enabled = true;
            playerController.agent.Warp(combatTransitionData.playerPosition);
            player.transform.rotation = combatTransitionData.playerRotation;
            MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            MainCamera.transform.position = combatTransitionData.cameraPosition;
            MainCamera.transform.rotation = combatTransitionData.cameraRotation;
            returningFromCombat = false;
        }
    }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        player = GameObject.FindGameObjectWithTag("Player");
        MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    void Start()
    {
        
    }

    void Update()
    {


    }
}
