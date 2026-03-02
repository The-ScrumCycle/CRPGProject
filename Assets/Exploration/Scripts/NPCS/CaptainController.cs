using UnityEngine;
using Dialogue.Core;
using Dialogue.Data;
using State;

public class CaptainController : MonoBehaviour
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

    // tracks whether this dialogue ended because player chose to board
    private bool boardedFromDialogue;

    private GameState state;
    public Sprite Face;

    private string actionBoard = "board";
    private string actionLeave = "leave";
    private string increaseIntelligence = "intelligence";
    private string insultedCaptain = "insult";
    private string SkipIntro = "introOver";
    private string apology = "apology";

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
        uiRunner.InitializeDialogue(from:gameObject, characterName:"captain");
        uiRunner.OptionSelectedAction += OnOptionSelected;
        uiRunner.DialogueEndedAction += OnDialogueEnded;
    }

    private void Start()
    {
        state = GameState.Instance;
        SearchForPlayer();

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

    // update is called once per frame
    void Update()
    {
        
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
    }

    // when I click on Docks, I should be able to leave the boat
    public void LeaveShip()
    {
        // transfer control back to player and retarget camera
        shipController.SetControllable(false);
        playerController.SetControllable(true);
        cameraController.SetTarget(playerController.transform);
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
        uiRunner.BeginDialogue("captain");
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
            else if (actionLeave == curAction)
            {
                LeaveWithoutBoarding();
            }
            else if (increaseIntelligence == curAction)
            {
                //test to see intelligence words
                Debug.Log("CaptainController");
                this.state.intelligencePowerUp(5);
                Debug.Log(state.Intelligence);
            }
            else if (curAction == insultedCaptain)
            {
                this.state.setFlag("insult");
            }
            else if (curAction == apology)
            {
                this.state.removeFlag("insult");
            }
            else if (curAction == SkipIntro)
            {
                this.state.setFlag("introOver");
            }
        }
       

    }

    private void OnDialogueEnded()
    {
        // default fallback when dialogue ends without choosing board option
        if (boardedFromDialogue)
        {
            return;
        }

        LeaveWithoutBoarding();
    }



}
