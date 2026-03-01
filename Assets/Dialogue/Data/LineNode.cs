using UnityEngine;
using System.Collections.Generic;
namespace Dialogue.Data
{
    public class LineNode : Node
    {
        [SerializeField] private SpeakerType speaker;
        public SpeakerType Speaker { get => speaker; internal set => speaker = value; } 

        [SerializeField] private string action;
        public string Action {get => action; internal set => action = value; }

        [SerializeField] private string lineText;
        public string LineText { get => lineText; internal set => lineText = value; } 

        [SerializeField] private Node nextNode;
        public Node NextNode { get => nextNode; internal set => nextNode = value; }        
        
        [SerializeField] private int minimumIntelligence = 0;
        public int MinimumIntelligence { get => minimumIntelligence; internal set => minimumIntelligence = value; } 

        public bool hasEnoughIntelligence(int currIntelligence)
        {
            return currIntelligence >= minimumIntelligence;
        }

    }
}