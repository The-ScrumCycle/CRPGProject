using System.Collections.Generic;
using unityEngine;
namespace Dialogue.Data
{
    public class Node : ScriptableObject
    {
        [SerializeField] private int Id { get; set; }

        public Node(int id)
        {
            Id = id;
        }
    }

    public class LineNode : Node
    {
        [SerializeField] private string LineText { get; set; }

        [SerializeField] private Node NextNode { get; set; }

        [SerializeField] private int minimumIntelligence { get; set; } = 0;

        public LineNode(int id, string lineText, Node nextNode, int minimumIntelligence) : base(id)
        {   
            LineText = lineText;
            NextNode = nextNode;
            minimumIntelligence = minimumIntelligence;
        }

        public bool hasEnoughIntelligence(int playerIntelligence)
        //min amount of intelligence needed
        {
            
            return playerIntelligence >= minimumIntelligence;
        }
    }

    public class OptionNode : Node
    {
        [SerializeField] private List<Node> Options { get; set; } = new List<Node>();

        public OptionNode(int id, List<Node> options) : base(id)
        {
            Options = options;
        }
    }
}