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

        [SerializeField] private int minimumIntelligence = 0;

        public int MinimumIntelligence => minimumIntelligence;

        public bool hasEnoughIntelligence(int currIntelligence)
        {
            return currIntelligence >= minimumIntelligence;
        }

    }
}