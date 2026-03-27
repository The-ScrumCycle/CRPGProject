using UnityEngine;
using Dialogue.Core;
using Game.Core;
using Dialogue.Data;
using State;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Game.Combat;


public class NPCDialogue : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] GameObject player;
    [SerializeField] PlayerController playerController;
    [SerializeField] private UIRunner uiRunner;
    [SerializeField] private DialogueRunner runner;

    [Header("NPC/Follower Settings")]
    public Sprite Face;
    public Sprite John;
    public Sprite Clarissa;
    public Sprite Malakor;
    public string characterDialogueID;
    private FollowerController followerController;
    private MusicController musicController;

    [Header("Auto Dialogue Triggers Settings")]
    public bool triggerOnApproach = false;
    public string triggerFlag = ""; // if set, flag has to be true for dialogue to trigger
    public string dontTriggerFlag = ""; // if set, flag has to be false for dialogue to trigger
    public bool hasTriggered = false;

    [Header("Actions")]
    private string actionLeave = "leave";


    private UnityEngine.AI.NavMeshAgent agent;
    private GameState state;
    GameStateManager gameStateManager;
    private NavMeshAgent playerAgent;
    private CameraController camera;

    // Main boss actions
    private string foundMalakor = "foundMalakor";
    private string startBossFight = "startBossFight";
    private string runAway = "runAway";
    private string malakorSpeaking = "malakorSpeaking";
    private string ranAway = "ranAway";
    private string defeatedMalakor = "defeatedMalakor";

    // john actions
    private string johnInParty = "johnInParty";
    private string johnNotInParty = "johnNotInParty";
    private string JohnIntroOver = "johnIntroOver";
    private string johnWait = "johnWait";
    private string johnFollow = "johnFollow";
    private string johnVillageExplored = "johnVillageExplored";
    private string johnSpeaking = "johnSpeaking";

    // clarissa actions
    private string clarissaInParty = "clarissaInParty";
    private string clarissaNotInParty = "clarissaNotInParty";
    private string clarissaIntroOver = "clarissaIntroOver";
    private string clarissaWait = "clarissaWait";
    private string clarissaFollow = "clarissaFollow";
    private string ogreBossDestroyed = "ogreBossDestroyed";
    private string clarissaSpeaking = "clarissaSpeaking";

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
        playerAgent = player.GetComponent<NavMeshAgent>();
        camera = FindObjectOfType<CameraController>();
        gameStateManager = GameStateManager.Instance;

        musicController = MusicController.Instance;
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
            playerAgent = player.GetComponent<NavMeshAgent>(); // FIX
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

        if (SearchForPlayer() && Vector3.Distance(player.transform.position, transform.position) < 8f)
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

            // John actions
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

            else if (curAction == johnVillageExplored)
            {
                this.state.setFlag("johnVillageExplored");
            }
            else if (curAction == johnSpeaking)
            {
                uiRunner.UpdateFace(John);
            }


            // Clarissa actions
            else if (curAction == clarissaInParty)
            {
                this.state.setFlag("clarissaInParty");
                PartyManager.Instance.AddFollowerActive(FollowerID.Cleric);
            }
            else if (curAction == clarissaNotInParty)
            {
                this.state.removeFlag("clarissaInParty");
                PartyManager.Instance.RemoveFollowerActive(FollowerID.Cleric);
            }
            else if (curAction == clarissaIntroOver)
            {
                this.state.setFlag("clarissaIntroOver");
            }
            else if (curAction == clarissaWait)
            {
                this.state.setFlag("clarissaWait");
                followerController.WaitHere();
            }
            else if (curAction == clarissaFollow)
            {
                this.state.removeFlag("clarissaWait");
                followerController.FollowMe();
            }
            else if (curAction == ogreBossDestroyed)
            {
                this.state.setFlag("ogreBossDestroyed");
            }

            else if (curAction == clarissaSpeaking)
            {
                uiRunner.UpdateFace(Clarissa);
            }

            // Main boss actions
            else if (curAction == foundMalakor)
            {
                this.state.setFlag("foundMalakor");
            }

            else if (curAction == startBossFight)
            {
                this.state.setFlag("startBossFight");
                GameStateManager.Instance.TransitionToCombat(gameObject);
            }
            else if (curAction == runAway)
            {
                this.state.setFlag("runAway");
                GameObject[] monsters = GameObject.FindGameObjectsWithTag("Shortcut");
                foreach (GameObject monster in monsters)
                {
                    EnemyID enemyID = monster.GetComponent<EnemyID>();
                    if (enemyID != null)
                    {
                        EnnemiesState.Instance.SetDeadEnnemy(enemyID.getEnemyID());
                        EnnemiesState.Instance.killEnnemies();
                    }
                    else
                    {
                        Debug.LogWarning($"GameObject {monster.name} tagged as 'Shortcut' does not have an enemyID component.");
                    }
                }
            }

            else if (curAction == malakorSpeaking)
            {
                uiRunner.UpdateFace(Malakor);
            }

            else if (curAction == defeatedMalakor)
            {
                this.state.setFlag("defeatedMalakor");
                musicController.SetMusic(musicController.GetEndingMusic());
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

        if (state.hasFlag(runAway)) 
        {
            this.state.removeFlag(runAway); 
            this.state.setFlag(ranAway);

            if (playerAgent == null) playerAgent = player.GetComponent<NavMeshAgent>(); 

            if (playerAgent != null) 
            {
                Vector3 targetPosition = new Vector3(38, 0, -247);
                NavMeshHit hit;

                if (NavMesh.SamplePosition(targetPosition, out hit, 15.0f, NavMesh.AllAreas))
                {
                    playerAgent.Warp(hit.position); 
                }
            }

            if (camera != null) camera.GoToPlayer();
        }

    }


    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnApproach || hasTriggered) return;

        if (other.gameObject.CompareTag("Player"))
        {
            bool canStart = string.IsNullOrEmpty(triggerFlag) || state.hasFlag(triggerFlag);
            bool shouldNotStop = string.IsNullOrEmpty(dontTriggerFlag) || !state.hasFlag(dontTriggerFlag);

            if (canStart && shouldNotStop)
            {
                BeginNPCDialogue();
                hasTriggered = true;
            }
        }

    }

}