using System.Collections.Generic;
using System;
using UnityEngine;
namespace Dialogue.Data
{
    public class DialogueGraph
    {
        public Node StartNode { get; set; }

        private Dictionary<Node, Dictionary<Node, int>> Dialogue { get; set; } = new();

        public DialogueGraph(Node startNode)
        {
            StartNode = startNode;
        }

        public void AddEdge(Node from, Node to, int weight=0)
        {
            if (!Dialogue.ContainsKey(from))
            {
                Dialogue[from] = new Dictionary<Node, int>();
            }
            Dialogue[from][to] = weight;
        }

        public Dictionary<Node, int> GetNeighbors(Node node)
        {
            if (Dialogue.ContainsKey(node))
            {
                return Dialogue[node];
            }
            return new Dictionary<Node, int>();
        }
        public Node getNode(int id)
        {
            foreach (var node in Dialogue.Keys)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }
            return null;
        }

        public Node getNode(Node node)
        {
            if (Dialogue.ContainsKey(node))
            {
                return node;
            }
            return null;
        }

    }
}