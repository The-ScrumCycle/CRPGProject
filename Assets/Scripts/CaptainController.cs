using UnityEngine;
using Dialogue.Core;
using Dialogue.Data;

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

    [Header("Dialogue Choice Mapping")]
    // option index that means board the ship
    [SerializeField] private int boardBoatOptionIndex = 0;
    // option index that means stay on land
    [SerializeField] private int stayOffBoatOptionIndex = 1;

    // tracks whether this dialogue ended because player chose to board
    private bool boardedFromDialogue;

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
        DialogueGraph graph = DialogueGraphLoader.LoadGraph("captain");
        DialogueRunner runner = gameObject.AddComponent<DialogueRunner>();
        runner.DialogueGraph = graph;
        uiRunner.DialogueRunner = runner;

        uiRunner.OptionSelectedAction += OnOptionSelected;
        uiRunner.DialogueEndedAction += OnDialogueEnded;
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
            Debug.LogError("CaptainController: Player reference is missing.");
            return;
        }

        if (Vector3.Distance(Player.transform.position, transform.position) > 10f)
        {
            return;
        }

        // start captain dialogue flow
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

        uiRunner.BeginDialogue("captain");
    }

    private void OnOptionSelected(int optionIndex)
    {
        // map generic option index to captain-specific game actions
        if (uiRunner == null || !uiRunner.IsDialogueActive)
        {
            return;
        }

        if (optionIndex == boardBoatOptionIndex)
        {
            boardedFromDialogue = true;
            BoardShip();
            uiRunner.EndDialogue();
            return;
        }

        if (optionIndex == stayOffBoatOptionIndex)
        {
            LeaveShip();
            uiRunner.EndDialogue();
        }
    }

    private void OnDialogueEnded()
    {
        // default fallback when dialogue ends without choosing board option
        if (boardedFromDialogue)
        {
            return;
        }

        LeaveShip();
    }



}
