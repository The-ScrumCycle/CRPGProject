using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // used for objective flag/text mappings

public class CharacterMenuController : MonoBehaviour
{
    [System.Serializable]
    private class ObjectiveEntry
    {
        [SerializeField] private string flagName; // flag name that maps to this objective text
        [TextArea(2, 4)]
        [SerializeField] private string objectiveText; // objective text shown when this flag is active

        public string FlagName => flagName; // exposes the serialized flag name for lookup
        public string ObjectiveText => objectiveText; // exposes the serialized objective text for display
    }

    [Header("Roots")]
    [SerializeField] private GameObject characterMenuRoot; // main character menu root

    [Header("Objective Text")]
    [SerializeField] private TMP_Text objectiveBodyText; // body text field under the main objective header
    [TextArea(2, 4)]
    [SerializeField] private string defaultObjectiveText; // fallback text shown when no matching objective flag is active
    [SerializeField] private List<ObjectiveEntry> objectiveEntries = new(); // ordered list of objective flag -> text mappings

    [Header("Layout Rebuild")]
    [SerializeField] private RectTransform contentRoot; // main content container with vertical layout group
    [SerializeField] private RectTransform objectiveContainer; // objective section container
    [SerializeField] private RectTransform statsContainer; // stats section container

    public static bool IsMenuOpen { get; private set; }
    public static int LastEscapeConsumedFrame { get; private set; } = -1; // tracks the frame where character menu consumed escape so escape menu does not also open

    private Coroutine refreshCoroutine;

    private void Start()
    {
        if (characterMenuRoot != null)
        {
            characterMenuRoot.SetActive(false);
        }

        IsMenuOpen = false;
        SetObjectiveText(defaultObjectiveText); // initialize objective body text when scene loads
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
            UpdateObjectiveFromFlag("ExampleFlag");
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

    public void UpdateObjectiveFromFlag(string activeFlag)
    {
        if (string.IsNullOrWhiteSpace(activeFlag))
        {
            SetObjectiveText(defaultObjectiveText); // fall back if no active flag was provided
            return;
        }

        foreach (ObjectiveEntry entry in objectiveEntries)
        {
            if (entry.FlagName == activeFlag)
            {
                SetObjectiveText(entry.ObjectiveText); // update objective text when a matching flag is found
                return;
            }
        }

        SetObjectiveText(defaultObjectiveText); // fall back if active flag does not match any configured objective
    }

    public void UpdateObjectiveFromFlags(IEnumerable<string> activeFlags)
    {
        if (activeFlags == null)
        {
            SetObjectiveText(defaultObjectiveText); // fall back if no active flags collection was provided
            return;
        }

        foreach (ObjectiveEntry entry in objectiveEntries)
        {
            foreach (string activeFlag in activeFlags)
            {
                if (entry.FlagName == activeFlag)
                {
                    SetObjectiveText(entry.ObjectiveText); // use the first configured objective whose flag is currently active
                    return;
                }
            }
        }

        SetObjectiveText(defaultObjectiveText); // fall back if none of the active flags match a configured objective
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
}