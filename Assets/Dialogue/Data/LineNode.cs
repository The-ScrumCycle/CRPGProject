using UnityEngine;
using System.Collections.Generic;
namespace Dialogue.Data
{
    public class LineNode : Node
    {
        [SerializeField] private string LineText { get; set; }

        [SerializeField] private Node NextNode { get; set; }

        [SerializeField] private int minimumIntelligence { get; set; } = 0;

        public bool hasEnoughIntelligence(int playerIntelligence)
        //min amount of intelligence needed
        {
            
            return playerIntelligence >= minimumIntelligence;
        }
    }
}