using UnityEngine;
using System.Collections.Generic;
namespace Dialogue.Data
{
    public class LineNode : Node
    {
        [SerializeField] private string lineText;
        public string LineText => lineText;

        [SerializeField] private Node nextNode;
        public Node NextNode => nextNode;

        [SerializeField] private int minimumIntelligence { get; set; } = 0;

        public bool hasEnoughIntelligence(int playerIntelligence)
        //min amount of intelligence needed
        {
            
            return playerIntelligence >= minimumIntelligence;
        }
    }
}