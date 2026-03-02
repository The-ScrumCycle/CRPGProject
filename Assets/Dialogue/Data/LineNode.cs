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

        // min intelligence, charisma, strength. Default is 0.
        [SerializeField] private int minimumIntelligence = 0;
        public int MinimumIntelligence { get => minimumIntelligence; internal set => minimumIntelligence = value; } 

        public bool hasEnoughIntelligence(int currIntelligence)
        {
            return currIntelligence >= minimumIntelligence;
        }

        [SerializeField] private int minimumCharisma = 0;
        public int MinimumCharisma { get => minimumCharisma; internal set => minimumCharisma = value; }

        public bool hasEnoughCharisma(int currCharisma)
        {
            return currCharisma >= minimumCharisma;
        }

        [SerializeField] private int minimumStrength = 0;
        public int MinimumStrength { get => minimumStrength; internal set => minimumStrength = value; }

        public bool hasEnoughStrength(int currStrength)
        {
            return currStrength >= minimumStrength;
        }

        // max intelligence, charisma, strength. Default is 11, since our normal max is 10.

        [SerializeField] private int maximumIntelligence = 11;
        public int MaximumIntelligence { get => maximumIntelligence; internal set => maximumIntelligence = value; }
        
        public bool hasTooMuchIntelligence(int currIntelligence)
        {
            return currIntelligence > maximumIntelligence;
        }

        [SerializeField] private int maximumCharisma = 11;
        public int MaximumCharisma { get => maximumCharisma; internal set => maximumCharisma = value; }

        public bool hasTooMuchCharisma(int currCharisma)
        {
            return currCharisma > maximumCharisma;
        }

        [SerializeField] private int maximumStrength = 11;
        public int MaximumStrength { get => maximumStrength; internal set => maximumStrength = value; }

        public bool hasTooMuchStrength(int currStrength)
        {
            return currStrength > maximumStrength;
        }


        // function to check if player meets the requirements 
        public bool meetRequirements(int currIntelligence, int currCharisma, int currStrength)
        {
            return hasEnoughIntelligence(currIntelligence) && !hasTooMuchIntelligence(currIntelligence) &&
                   hasEnoughCharisma(currCharisma) && !hasTooMuchCharisma(currCharisma) && hasEnoughStrength(currStrength) && !hasTooMuchStrength(currStrength);
        }


    }
}