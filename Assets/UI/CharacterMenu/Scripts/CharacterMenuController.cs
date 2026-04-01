using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // used for objective flag/text mappings
using State;

public class CharacterMenuController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject characterMenuRoot; // main character menu root

    [Header("Objective Text")]
    [SerializeField] private TMP_Text objectiveBodyText; // body text field under the main objective header
    [TextArea(2, 4)]
    [SerializeField] private string defaultObjectiveText; // fallback text shown when no matching objective flag is active

    [Header("Layout Rebuild")]
    [SerializeField] private RectTransform contentRoot; // main content container with vertical layout group
    [SerializeField] private RectTransform objectiveContainer; // objective section container
    [SerializeField] private RectTransform statsContainer; // stats section container

    [Header("Stats Text")]
    [SerializeField] private TMP_Text strengthValueText;
    [SerializeField] private TMP_Text charismaValueText;
    [SerializeField] private TMP_Text intelligenceValueText;
    [SerializeField] private TMP_Text LevelValueText;

    public static bool IsMenuOpen { get; private set; }
    public static int LastEscapeConsumedFrame { get; private set; } = -1; // tracks the frame where character menu consumed escape so escape menu does not also open

    private Coroutine refreshCoroutine;

    private GameState state;
    private PartyManager partyManager;

    private void Start()
    {
        if (characterMenuRoot != null)
        {
            characterMenuRoot.SetActive(false);
        }

        IsMenuOpen = false;
        SetObjectiveText(defaultObjectiveText); // initialize objective body text when scene loads

        state = GameState.Instance;
        partyManager = PartyManager.Instance;

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (IsMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
        else if (IsMenuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            LastEscapeConsumedFrame = Time.frameCount; // mark this escape press as consumed so the escape menu does not open in the same frame
            CloseMenu();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) // press 1 key to test objective text update
        {
            UpdateObjective();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // press 2 key to test fallback to default objective text
        {
            SetObjectiveText(defaultObjectiveText);
        }

    }

    // character menu open/close
    public void OpenMenu()
    {
        if (characterMenuRoot == null) return;

        characterMenuRoot.SetActive(true);
        IsMenuOpen = true; // track character menu state so other menus can check it

        UpdateObjective(); // update objective text based on current game state when menu opens

        UpdateStats(); // update stats text based on current game state when menu opens

        // stop any old layout refresh coroutine before starting a new one
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }

        // force layout rebuild over multiple frames so nested layout groups/TMP text load correctly on first open
        refreshCoroutine = StartCoroutine(RefreshLayoutOverFrames());
    }

    public void CloseMenu()
    {
        if (characterMenuRoot == null) return;

        // stop layout refresh if menu is closed mid-refresh
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }

        characterMenuRoot.SetActive(false);
        IsMenuOpen = false; // clear character menu state when menu closes
    }

    public void ToggleMenu()
    {
        if (IsMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    // objective text update helpers
    public void SetObjectiveText(string objectiveText)
    {
        if (objectiveBodyText == null) return; // do nothing if objective text field is not assigned in inspector

        objectiveBodyText.text = string.IsNullOrWhiteSpace(objectiveText) ? defaultObjectiveText : objectiveText; // fall back to default text if passed objective is empty
    }


    private IEnumerator RefreshLayoutOverFrames()
    {
        // let unity activate the object tree first
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (objectiveContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(objectiveContainer);
        }

        if (statsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(statsContainer);
        }

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        RectTransform rootRect = characterMenuRoot.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        Canvas.ForceUpdateCanvases();

        // sometimes TMP/layout needs one more frame
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (objectiveContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(objectiveContainer);
        }

        if (statsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(statsContainer);
        }

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        if (rootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        Canvas.ForceUpdateCanvases();

        refreshCoroutine = null;
    }

    // Carlos's functions : 
    private void UpdateObjective()
    {
        if (state == null) state = GameState.Instance;
        if (state == null) { SetObjectiveText(defaultObjectiveText); return; }

        if (state.isFlagsEmpty())
        {
            SetObjectiveText("Speak with the Captain.");
        }
        else if (state.hasFlag("foundMalakor"))
        {
            SetObjectiveText("Malakor is too powerful to face now. Explore the other islands and destroy both Heart Crystals.");
        }
        else if (state.hasFlag("johnIntroOver"))
        {
            SetObjectiveText("Explore the village and the forest to find a way to reach the castle. Slay Malakor");
        }
        else if (state.hasFlag("introOver"))
        {
            SetObjectiveText("Speak with the old man in front of the mansion.");
        }
        else
        {
            SetObjectiveText(defaultObjectiveText);
        }   

    }

    private void UpdateStats()
    {
        if (state == null) state = GameState.Instance;
        if (state == null) return;
        strengthValueText.text = state.GetStrength().ToString();
        charismaValueText.text = state.GetCharisma().ToString();
        intelligenceValueText.text = state.GetIntelligence().ToString();
        LevelValueText.text = partyManager.GetPartyLevel().ToString();
    }


    }