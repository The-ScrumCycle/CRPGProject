using UnityEngine;
using Dialogue.Core;
using Dialogue.Data;
using State;

public class NPCDialogue : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] GameObject player;
    [SerializeField] PlayerController playerController;
    [SerializeField] private UIRunner uiRunner;

    [Header("NPC/Follower Settings")]
    public Sprite Face;
    public string characterDialogueID;
    private FollowerController followerController;


    private UnityEngine.AI.NavMeshAgent agent;
    private GameState state;


    [Header("Actions")]
    private string actionLeave = "leave";
    private string increaseIntelligence = "intelligence";
    private string insultedCaptain = "insult";
    private string apology = "apology";

    private void Awake()
    {
        // If character is a follower, get follower controller and agent
        followerController = GetComponent<FollowerController>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // find the ui runner in scene if not assigned in inspector
        if (uiRunner == null)
        {
            uiRunner = FindObjectOfType<UIRunner>();
        }

        if (uiRunner == null)
        {
            Debug.LogError("No UIRunner found in scene");
            return;
        }

        // load npc dialogue graph and create a runner component
        uiRunner.InitializeDialogue(from: gameObject, characterName: characterDialogueID);
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
        if (uiRunner == null)
        {
            return;
        }

        uiRunner.OptionSelectedAction -= OnOptionSelected;
        uiRunner.DialogueEndedAction -= OnDialogueEnded;
    }

    // set up player object and player controller reference
    bool SearchForPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("player not found");
                return false;
            }
        }

        if (playerController == null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
              {
                  Debug.LogError("PlayerController component not found on player");
                  return false;
            }
        }

        return true;
    }



    void OnMouseDown()
    {

        Debug.Log("Clicked on NPC");

        if (uiRunner != null && uiRunner.IsDialogueActive)
        {
            return;
        }

        if(SearchForPlayer() && Vector3.Distance(player.transform.position, transform.position) < 8f)
        {
            // start captain dialogue flow
            BeginNPCDialogue();

        }
    }


    private void BeginNPCDialogue()
    {
        // open dialogue ui and lock player while choice is pending
        if (uiRunner == null)
        {
            Debug.LogError("UIRunner is not assigned.");
            return;
        }

        if (playerController != null)
        {
            playerController.SetControllable(false);
            playerController.StopMovement();
        }

        if (agent != null)
        {
            agent.isStopped = true;
        }

        uiRunner.UpdateFace(Face);
        uiRunner.BeginDialogue(characterDialogueID);
    }


    private void OnOptionSelected(string action)
    {
        Debug.Log(action);
        Debug.Log($"Setting flag, current flags: {string.Join(", ", state.EventFlags)}");

      
        if (actionLeave == action)
        {
            return;
        }
        else if (increaseIntelligence == action)
        {
            //test to see intelligence words
            Debug.Log("CaptainController");
            this.state.intelligencePowerUp(5);
            Debug.Log(state.Intelligence);
        }
        else if (action == insultedCaptain)
        {
            this.state.setFlag("insult");
        }
        else if (action == apology)
        {
            this.state.removeFlag("insult");
        }

    }

    private void OnDialogueEnded()
    {
        // if it was a follower, reactivate movement
        if (agent != null)
        {
            agent.isStopped = false;
        }


    }

}
