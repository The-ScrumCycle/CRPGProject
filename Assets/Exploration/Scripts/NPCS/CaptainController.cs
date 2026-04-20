using UnityEngine;
using Dialogue.Core;
using Dialogue.Data;
using State;
using Core.Save;

public class CaptainController : MonoBehaviour, ISaveable
{

    [Header("Components")]
    // player object used for distance checks before interaction
    public GameObject Player;   
    // movement/controller references switched when boarding or leaving ship
    public PlayerController playerController;

    public ShipController shipController;
    public CameraController cameraController;
    // reusable dialogue ui driver that connects DialogueRunner to dialogue widgets
    [SerializeField] private UIRunner uiRunner;
    [SerializeField] private DialogueRunner runner;

    [SerializeField] private MusicController musicController;

    // tracks whether this dialogue ended because player chose to board
    private bool boardedFromDialogue;

    private GameState state;
    public Sprite Face;

    private string actionBoard = "board";
    private string SkipIntro = "introOver";

    // stats selection 
    private string set_strength_2     = "set_strength_2";
    private string set_strength_5     = "set_strength_5";
    private string set_strength_8     = "set_strength_8";
    private string set_intelligence_2 = "set_intelligence_2";
    private string set_intelligence_5 = "set_intelligence_5";
    private string set_intelligence_8 = "set_intelligence_8";
    private string set_charisma_2     = "set_charisma_2";
    private string set_charisma_5     = "set_charisma_5";
    private string set_charisma_8     = "set_charisma_8";
    private string respec             = "respec";
    private string foundMalakor       = "foundMalakor";


    private void Awake()
    {
        // find the ui runner in scene if not assigned in inspector
        if (uiRunner == null)
        {
            uiRunner = FindObjectOfType<UIRunner>();
        }

        if (uiRunner == null)
        {
            Debug.LogError("CaptainController: No UIRunner found in scene.");
            return;
        }
        // load captain dialogue graph and create a runner component
        runner = gameObject.AddComponent<DialogueRunner>();
        runner.DialogueGraph = DialogueGraphLoader.LoadGraph("Captain Jack");
    }

    private void Start()
    {
        state = GameState.Instance;
        SearchForPlayer();

        musicController = MusicController.Instance;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
        }
    }

    private void OnDestroy()
    {
        // unsubscribe to avoid duplicate callbacks after domain reloads
        if (uiRunner == null)
        {
            return;
        }

        uiRunner.OptionSelectedAction -= OnOptionSelected;
        uiRunner.DialogueEndedAction -= OnDialogueEnded;
    }


    // set up player object and player controller reference
    void SearchForPlayer()
    {
        if (Player == null)
        {
            Player = GameObject.FindGameObjectWithTag("Player");
            if (Player == null)
            {
                Debug.LogError("Player not found");
                return;
            }
        }

        if (playerController == null)
        {
            playerController = Player.GetComponent<PlayerController>();
        }
    }

    // when I click on captain , I should be able to load in the boat
    void OnMouseDown ()
    {
        // ignore click while a dialogue session is already active
        if (uiRunner != null && uiRunner.IsDialogueActive)
        {
            return;
        }

        if (Player == null)
        {
            SearchForPlayer();
            if (Player == null)
            {
                Debug.LogError("Player not found");
                return;
            }
            else if (playerController == null)
            {
                Debug.LogError("PlayerController not found on player");
                return;
            }
        }

        if (Vector3.Distance(Player.transform.position, transform.position) > 8f)
        {
            return;
        }

        // start captain dialogue flow
        playerController.StopMovement();
        BeginCaptainDialogue();

    }

    // when I click on captain , I should be able to board the boat
    public void BoardShip()
    {
        // transfer control to ship and retarget camera
        playerController.SetControllable(false);
        shipController.SetControllable(true);
        cameraController.SetTarget(shipController.transform);
        cameraController.SetShipCamera();
        
        // player and captain should not be visible while on the boat. 
        Player.SetActive(false);
        gameObject.SetActive(false);

        // play sailing music
        musicController.SetMusic(musicController.GetSailingMusic());

    }

    // leave conversation withoout boarding, re-nable player control
    private void LeaveWithoutBoarding()
    {
        playerController.SetControllable(true);
    }

    private void BeginCaptainDialogue()
    {
        // open dialogue ui and lock player while choice is pending
        if (uiRunner == null)
        {
            Debug.LogError("CaptainController: UIRunner is not assigned.");
            return;
        }

        boardedFromDialogue = false;
        if (playerController != null)
        {
            playerController.SetControllable(false);
        }

        uiRunner.UpdateFace(Face);
        playerController.SetInDialogue(true);
        uiRunner.SetDialogueRunner(runner);
        uiRunner.OptionSelectedAction += OnOptionSelected;
        uiRunner.DialogueEndedAction += OnDialogueEnded;
        uiRunner.BeginDialogue("Captain Jack");
    }

    private void OnOptionSelected(string action)
    {
        // map generic option index to captain-specific game actions
        Debug.Log(action);
        Debug.Log($"Setting flag, current flags: {string.Join(", ", state.EventFlags)}");

        string[] actions = action.Split(',');

        foreach(string currentAction in actions)
        {
            string curAction = currentAction.Trim();

            if (actionBoard == curAction)
            {
                boardedFromDialogue = true;
                BoardShip();
            }

            else if (foundMalakor == curAction)
            {
                this.state.setFlag("foundMalakor");
            }

            // stats selection 
            else if (set_strength_2 == curAction)
            {
                this.state.strengthPowerUp(2);
                Debug.Log(state.Intelligence);
            }

            else if (set_strength_5 == curAction)
            {
                this.state.strengthPowerUp(5);
                Debug.Log(state.Intelligence);
            }

            else if (set_strength_8 == curAction)
            {
                this.state.strengthPowerUp(8);
                Debug.Log(state.Intelligence);
            }
            else if (set_intelligence_2 == curAction)
            {
                this.state.intelligencePowerUp(2);
                Debug.Log(state.Charisma);
            }
            else if (set_intelligence_5 == curAction)
            {
                this.state.intelligencePowerUp(5);
                Debug.Log(state.Charisma);
            }
            else if (set_intelligence_8 == curAction)
            {
                this.state.intelligencePowerUp(8);
                Debug.Log(state.Charisma);
            }
            else if (set_charisma_2 == curAction)
            {
                this.state.charismaPowerUp(2);
                Debug.Log(state.Strength);
            }
            else if (set_charisma_5 == curAction)
            {
                this.state.charismaPowerUp(5);
                Debug.Log(state.Strength);
            }
            else if (set_charisma_8 == curAction)
            {
                this.state.charismaPowerUp(8);
                Debug.Log(state.Strength);
            }

            else if (respec == curAction)
            {
                this.state.resetStats();
            }

            else if (curAction == SkipIntro)
            {
                this.state.setFlag("introOver");
            }
        }
       

    }

    private void OnDialogueEnded()
    {

        uiRunner.OptionSelectedAction -= OnOptionSelected;
        uiRunner.DialogueEndedAction -= OnDialogueEnded;
        playerController.SetInDialogue(false);

        // default fallback when dialogue ends without choosing board option
        if (boardedFromDialogue)
        {
            return;
        }

        LeaveWithoutBoarding();
    }

    public void SetSaveData(SaveData saveData)
    {
        saveData.captain.position = transform.position;
        saveData.captain.rotation = transform.rotation;
        saveData.captain.active = gameObject.activeSelf;
    }

    public void LoadSaveData(SaveData saveData)
    {
        transform.position = saveData.captain.position;
        transform.rotation = saveData.captain.rotation;
        gameObject.SetActive(saveData.captain.active);
    }



}
