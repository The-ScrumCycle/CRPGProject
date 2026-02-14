using UnityEngine;
using Dialogue.Data;
using System.Collections.Generic;
using System;

namespace Dialogue.Core
{
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] private DialogueGraph dialogueGraph;

        public DialogueGraph DialogueGraph {get => dialogueGraph; set => dialogueGraph = value;}
        private Node currentNode;

        //actions will be used to trigger the UI
        public event Action<string> LineNodeAction;
        public event Action<List<string>> OptionNodeAction;

        private List<LineNode> playerCurrentOptions = new(); //list of nodes player can say due to intelligence

        public event Action DialogueEndAction;
        public event Action DialogueStartAction;

        private GameState gameState;

        public void BeginDialogue()
        {
            if (dialogueGraph == null){
                Debug.LogError("dialogue graph is not assigned");
                return;
            }
            else if (dialogueGraph.StartNode == null)
            {
                Debug.LogError("dialogue graph has no start node");
                return;
            }
            gameState = GameState.Instance;
            currentNode = dialogueGraph.StartNode;
            DialogueStartAction?.Invoke();
            ProccessNode();
        }

        public void ProccessNode()
        /*
        Idea is: we pass the string through invoking the Actions --> the ui will pick up the string and display it
        When we invoke options, it will return a list of nodes --> ui should have a helper, that will convert nodes to strings, or I will implement a 
        helper function for it in the backend
        */
        {
            if (currentNode is null)
            {
                DialogueEndAction?.Invoke();
                return;
            }
            else if (currentNode is LineNode ln)
            {
                LineNodeAction?.Invoke(ln.LineText);
                return;
            }
            else if (currentNode is OptionNode optionNode)
            {
                //list of strings to send to frontend
                playerCurrentOptions = new();
                List<string> usableText = new();
                foreach (var node in optionNode.Options)
                {
                    if(node is LineNode lineNode)
                    {
                        if (lineNode.hasEnoughIntelligence(gameState.Intelligence)){
                            //check if player has min intelligence for all options
                            playerCurrentOptions.Add(lineNode);
                            usableText.Add(lineNode.LineText);
                        }
                    }
                    else
                    {
                        Debug.LogError("Current node is not an OptionNode");
                        return;
                    }
                }
                OptionNodeAction?.Invoke(usableText);
                return;
            }
            else if (currentNode is ConditionalNode conditionalNode)
            //check if certain event has happened to display conditional dialogue
            {
                bool met = gameState.hasFlag(conditionalNode.EventName);
                Node result =  met ? conditionalNode.ConditionMetNode : conditionalNode.ConditionNotMetNode;
                //LineNodeAction?.Invoke(result.LineText);
                currentNode = result;
                ProccessNode();
            }
        }

        public void Next()
        /*
        use this when the user presses "continue" --> will then cycle to next node and send to UI
        */
        {
            if (currentNode is LineNode lineNode)
            {
                currentNode = lineNode.NextNode;
                ProccessNode();
            }
            else
            {
                Debug.LogError("Current node is not a LineNode");
            }
        }

        public void SelectOptions(int optionIndex)
        /*
        user will choose a dialogue option --> from 0 to x, frontend will call this function with the index of the options selected
        backend will process and then process the next node, sending back to frontend
        */
        {
            if (!(currentNode is OptionNode))
            {
                Debug.LogError("Current node is not an OptionNode");
                return;
            }
            List<LineNode> options = playerCurrentOptions;
            if (optionIndex < 0 || optionIndex >= options.Count)
            {
                Debug.LogError("Invalid option index selected");
                return;
            }
            currentNode = options[optionIndex];
            playerCurrentOptions = new();
            ProccessNode();
        }

        public void EndDialogue()
        /*
        once dialogue is finished
        */
        {
            currentNode = null;
            DialogueEndAction?.Invoke();
        }




        
    }
}  
