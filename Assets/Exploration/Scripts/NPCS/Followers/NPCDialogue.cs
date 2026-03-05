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
    [SerializeField] private DialogueRunner runner;

    [Header("Actions")]
    private string actionLeave = "leave";
  
    // john actions
    private string johnInParty    = "johnInParty";
    private string johnNotInParty = "johnNotInParty";
    private string JohnIntroOver  = "johnIntroOver";
    private string johnWait       = "johnWait";
    private string johnFollow     = "johnFollow";


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
        runner = gameObject.AddComponent<DialogueRunner>();
        runner.DialogueGraph = DialogueGraphLoader.LoadGraph(characterDialogueID);
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

        playerController.SetInDialogue(true);
        uiRunner.UpdateFace(Face);
        uiRunner.SetDialogueRunner(runner);
        uiRunner.OptionSelectedAction += OnOptionSelected;
        uiRunner.DialogueEndedAction += OnDialogueEnded;
        uiRunner.BeginDialogue(characterDialogueID);
    }


    private void OnOptionSelected(string action)
    {
        // map generic option index to captain-specific game actions
        Debug.Log(action);
        Debug.Log($"Setting flag, current flags: {string.Join(", ", state.EventFlags)}");

        if (string.IsNullOrEmpty(action))
        {
            Debug.LogWarning("Received empty action from dialogue option.");
            return;
        }

        string[] actions = action.Split(',');

        foreach (string currentAction in actions)
        {
            string curAction = currentAction.Trim();

            if (actionLeave == curAction)
            {

            }

            // john actions
            else if (curAction == johnInParty)
            {
                this.state.setFlag("johnInParty");
                PartyManager.Instance.AddFollowerActive(FollowerID.Warrior);
            }
            else if (curAction == johnNotInParty)
            {
                this.state.removeFlag("johnInParty");
                PartyManager.Instance.RemoveFollowerActive(FollowerID.Warrior);
            }

            else if (curAction == JohnIntroOver)
            {
                this.state.setFlag("johnIntroOver");
            } 

            else if (curAction == johnWait)
            {
                this.state.setFlag("johnWait");
                followerController.WaitHere();
            }
            else if (curAction == johnFollow)
            {
                this.state.removeFlag("johnWait");
                followerController.FollowMe();
            }



        }


    }

    private void OnDialogueEnded()
    {
        // if it was a follower, reactivate movement
        if (agent != null)
        {
            agent.isStopped = false;
        }

        playerController.SetControllable(true);
        playerController.SetInDialogue(false);
        uiRunner.OptionSelectedAction -= OnOptionSelected;
        uiRunner.DialogueEndedAction -= OnDialogueEnded;


    }

}
