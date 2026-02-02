using UnityEngine;
using Dialogue.Data;
using System.Collections.Generic;
using System;

namespace Dialogue.Core
{
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] private DialogueGraph dialogueGraph;
        private Node currentNode;

        //actions will be used to trigger the UI
        public event Action<string> LineNodeAction;
        public event Action<List<string>> OptionNodeAction;



        public event Action DialogueEndAction;
        public event Action DialogueStartAction;

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
            currentNode = dialogueGraph.StartNode;
            DialogueStartAction?.Invoke();
            //proccessNode
            
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
            else if (currentNode is LineNode lineNode)
            {
                LineNodeAction?.Invoke(lineNode.LineText);
                return;
                }
            else if (currentNode is OptionNode optionNode)
            {
                OptionNodeAction?.Invoke(optionNode.getOptionsText());
                return;
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
        user will choose a dialogue option --> from 0 to x, frontend will calll this function with the index of the options selected
        backend will process and then process the next node, sending back to frontend
        */
        {
            if (!(currentNode is OptionNode))
            {
                Debug.LogError("Current node is not an OptionNode");
                return;
            }
            List<Node> options = ((OptionNode)currentNode).Options;
            if (optionIndex < 0 || optionIndex >= options.Count)
            {
                Debug.LogError("Invalid option index selected");
                return;
            }
            currentNode = options[optionIndex];
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
