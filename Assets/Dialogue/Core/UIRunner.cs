using System;
using System.Collections.Generic;
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

        public DialogueRunner DialogueRunner {get => dialogueRunner; set => dialogueRunner = value;}

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

        // cached button actions so listeners can be removed safely
        private UnityAction[] optionButtonActions;

        // true while a dialogue session is currently active
        public bool IsDialogueActive { get; private set; }

        // lets npc controllers react to user choice without owning ui plumbing
        public event Action<int> OptionSelectedAction;
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

        public void BeginDialogue(string speakerName)
        {
            // guard against missing backend wiring
            if (dialogueRunner == null)
            {
                Debug.LogError("UIRunner: DialogueRunner is not assigned.");
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
            OptionSelectedAction?.Invoke(optionIndex);
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

            SetOptionButtons(new List<string>());
        }

        private void OnDialogueOptions(List<string> options)
        {
            // display selectable options and hide continue button
            if (!IsDialogueActive)
            {
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

            SetOptionButtons(new List<string>());
        }

        private void SetSpeakerName(string speakerName)
        {
            // update speaker label for current npc
            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
            }
        }

        private void SetOptionButtons(List<string> options)
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
                    optionText.text = options[i];
                }
            }
        }
    }
}
