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
        public event Action<List<Node>> OptionNodeAction;



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
        {
            if (currentNode is null)
            {
                DialogueEndAction?.Invoke();
                return;
            }
            else if (currentNode is LineNode lineNode)
            {
                LineNodeAction?.Invoke(lineNode.LineText);
                currentNode = lineNode.NextNode;
                return;
                }
            else if (currentNode is OptionNode optionNode)
            {
                OptionNodeAction?.Invoke(optionNode.Options);
            }
        }

        public void Next()
        {
            if (currentNode is LineNode)
            {
                ProccessNode();
            }
            else
            {
                Debug.LogError("Current node is not a LineNode");
            }
        }

        public void SelectOptions(int optionIndex)
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
        {
            currentNode = null;
            DialogueEndAction?.Invoke();
        }




        
    }
}  
