using System;
using System.Collections.Generic;
using Dialogue.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dialogue.Core
{
    public class UIRunner : MonoBehaviour
    {
        [Header("Backend")]
        // backend dialogue source that emits line and option events
        [SerializeField] private DialogueRunner dialogueRunner;
        public DialogueRunner DialogueRunner
        {
            get => dialogueRunner;
            set => SetDialogueRunner(value);
        }

        [Header("Dialogue UI")]
        // root dialogue panel to show and hide
        [SerializeField] private GameObject dialogueUIRoot;
        // text field for the current dialogue line
        [SerializeField] private TMP_Text dialogueBodyText;
        // text field for the speaker label
        [SerializeField] private TMP_Text speakerNameText;
        // continue button for linear line progression
        [SerializeField] private Button continueButton;
        // option buttons that get populated from option nodes
        [SerializeField] private Button[] optionButtons;

        // cached reference to camera controller for input blocking during dialogue
        [Header("Input Blocking")]
        [SerializeField] private CameraController cameraController;

        // cached button actions so listeners can be removed safely
        private UnityAction[] optionButtonActions;

        // true while a dialogue session is currently active
        private List<DialogueOptions> currPlayerOptions;
        public bool IsDialogueActive { get; private set; }

        // lets npc controllers react to user choice without owning ui plumbing
        public event Action<string> OptionSelectedAction;
        // lets npc controllers react when dialogue fully closes
        public event Action DialogueEndedAction;

        private void Awake()
        {
            // bind ui events and backend events once at startup
            BindUIListeners();
            SubscribeDialogueEvents();
            HideDialogueUI();
        }

        private void OnDestroy()
        {
            // unbind everything to prevent duplicate callbacks after reloads
            UnbindUIListeners();
            UnsubscribeDialogueEvents();
        }

        private void Update()
        {
            // allow user to cancel dialogue with escape to avoid hard lock
            if (!IsDialogueActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndDialogue();
            }
        }

        public void SetDialogueRunner(DialogueRunner runner)
        {
            if (ReferenceEquals(dialogueRunner, runner))
            {
                return;
            }

            // rebind event subscriptions when the runner instance changes at runtime
            UnsubscribeDialogueEvents();
            dialogueRunner = runner;
            SubscribeDialogueEvents();
        }

        public void BeginDialogue(string speakerName)
        {
            // guard against missing backend wiring
            if (dialogueRunner == null)
            {
                Debug.LogError("UIRunner: DialogueRunner is not assigned.");
                return;
            }
            if (dialogueRunner.DialogueGraph == null || dialogueRunner.DialogueGraph.StartNode == null)
            {
                Debug.LogError("UIRunner: dialogue graph is not ready on DialogueRunner.");
                return;
            }

            // open ui and start dialogue processing from the runner
            IsDialogueActive = true;
            SetSpeakerName(speakerName);
            ShowDialogueUI();
            dialogueRunner.BeginDialogue();
        }

        public void EndDialogue()
        {
            // ignore close requests when nothing is open
            if (!IsDialogueActive)
            {
                return;
            }

            // use backend end flow so DialogueEndAction still fires consistently
            if (dialogueRunner != null)
            {
                dialogueRunner.EndDialogue();
                return;
            }

            // fallback close path when backend reference is missing
            CloseDialogueUI();
        }

        public void InitializeDialogue(GameObject from, string characterName)
        {
            Debug.Log("Init dialogue");
            DialogueGraph graph = DialogueGraphLoader.LoadGraph(characterName);
            if(graph == null)
            {
                Debug.LogError("graph did not load correctly");
            }
            DialogueRunner runner = from.AddComponent<DialogueRunner>();
            runner.DialogueGraph = graph;
            SetDialogueRunner(runner);
        }

        private void BindUIListeners()
        {
            // continue button advances current line node
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinuePressed);
            }

            if (optionButtons == null)
            {
                return;
            }

            optionButtonActions = new UnityAction[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null)
                {
                    continue;
                }

                // each button sends its own option index back to the runner
                int optionIndex = i;
                optionButtonActions[i] = () => OnOptionPressed(optionIndex);
                optionButtons[i].onClick.AddListener(optionButtonActions[i]);
            }
        }
        private void UnbindUIListeners()
        {
            // remove continue callback
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinuePressed);
            }

            if (optionButtons == null || optionButtonActions == null)
            {
                return;
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null || optionButtonActions[i] == null)
                {
                    continue;
                }

                // remove per-button option callback
                optionButtons[i].onClick.RemoveListener(optionButtonActions[i]);
            }
        }

        private void SubscribeDialogueEvents()
        {
            // connect backend events to ui update handlers
            if (dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.LineNodeAction += OnDialogueLine;
            dialogueRunner.OptionNodeAction += OnDialogueOptions;
            dialogueRunner.DialogueEndAction += OnDialogueEnded;
        }

        private void UnsubscribeDialogueEvents()
        {
            // detach backend events on destroy
            if (dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.LineNodeAction -= OnDialogueLine;
            dialogueRunner.OptionNodeAction -= OnDialogueOptions;
            dialogueRunner.DialogueEndAction -= OnDialogueEnded;
        }

        private void OnContinuePressed()
        {
            // continue only when dialogue is active and backend exists
            Debug.Log("Continue button pressed!");
            if (!IsDialogueActive || dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.Next();
        }

        private void OnOptionPressed(int optionIndex)
        {
            // send selected option to backend and notify npc controller layer
            if (!IsDialogueActive || dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.SelectOptions(optionIndex);
            string action = this.currPlayerOptions[optionIndex].action;
            OptionSelectedAction?.Invoke(action);
            this.currPlayerOptions = new List<DialogueOptions>();
        }

        private void OnDialogueLine(string line)
        {
            // display line text and switch ui to continue mode
            if (!IsDialogueActive)
            {
                return;
            }

            if (dialogueBodyText != null)
            {
                dialogueBodyText.text = line;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }

            SetOptionButtons(new List<DialogueOptions>());
        }

        private void OnDialogueOptions(List<DialogueOptions> options)
        {
            this.currPlayerOptions = options;
            // display selectable options and hide continue button
            if (!IsDialogueActive)
            {
                return;
            }
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning("UIRunner: no dialogue options are available, ending dialogue.");
                EndDialogue();
                return;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
            
            SetOptionButtons(options);
        }

        private void OnDialogueEnded()
        {
            // backend signaled end of dialogue session
            if (!IsDialogueActive)
            {
                return;
            }

            CloseDialogueUI();
        }

        private void CloseDialogueUI()
        {
            // close ui state and broadcast end event to external listeners
            IsDialogueActive = false;
            HideDialogueUI();
            DialogueEndedAction?.Invoke();
        }

        private void ShowDialogueUI()
        {
            if (dialogueUIRoot != null)
            {
                dialogueUIRoot.SetActive(true);
            }
            // block camera input when dialogue opens to prevent unwanted movement during conversations
            if (cameraController != null)
            {
                cameraController.SetInputBlocked(true);
            }
        }

        private void HideDialogueUI()
        {
            // hide root and clear interactive controls
            if (dialogueUIRoot != null)
            {
                dialogueUIRoot.SetActive(false);
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            SetOptionButtons(new List<DialogueOptions>());

            // unblock camera input when dialogue closes
            if (cameraController != null){
                cameraController.SetInputBlocked(false);
            }
        }

        private void SetSpeakerName(string speakerName)
        {
            // update speaker label for current npc
            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
            }
        }

        private void SetOptionButtons(List<DialogueOptions> options)
        {
            // show only available options and write option text for each visible button
            if (optionButtons == null)
            {
                return;
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null)
                {
                    continue;
                }

                bool show = options != null && i < options.Count;
                optionButtons[i].gameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                TMP_Text optionText = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
                if (optionText != null)
                {
                    optionText.text = options[i].text;
                }
            }
        }
    }
}
