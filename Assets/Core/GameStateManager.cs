using UnityEngine;
using UnityEngine.SceneManagement;
public class GameStateManager : MonoBehaviour
{
    public CombatTransitionData combatTransitionData;
    GameObject player;
    public PlayerController playerController;
    GameObject MainCamera;

    // singleton instance of the GameStateManager
    public static GameStateManager Instance { get; private set; }

    // current state of the game
    public GameState CurrentState { get; private set; } = GameState.Exploration;

    // set up the state used.
    private void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        Debug.Log("Game state changed to: " + CurrentState);
    }

    // get the current state of the game.
    public GameState GetCurrentState()
    {
        Debug.Log("Current game state: " + CurrentState);
        return CurrentState;
    }

    // transition to combat state and scene
    public void TransitionToCombat()
    {
        SetState(GameState.Combat);

        // save player and camera positions/rotations
        combatTransitionData.SaveTransitionData();

        SceneManager.LoadScene("CombatScene");
    }

    // transition to exploration state and scene
    public void TransitionToExploration()
    {
        SetState(GameState.Exploration);
        SceneManager.LoadScene("Exploration");

        // restore player and camera positions/rotations
        playerController.agent.Warp(combatTransitionData.playerPosition);
        player.transform.rotation = combatTransitionData.playerRotation;
        MainCamera.transform.position = combatTransitionData.cameraPosition;
        MainCamera.transform.rotation = combatTransitionData.cameraRotation;

    }



    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
