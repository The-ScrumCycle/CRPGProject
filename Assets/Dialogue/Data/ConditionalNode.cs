using UnityEngine;
using System.Collections.Generic;
namespace Dialogue.Data
{
    public class ConditionalNode : Node
    {
        [SerializeField] private string eventName;
        public string EventName {get => eventName; internal set => eventName = value;}
        [SerializeField] Node conditionMetNode;

        public Node ConditionMetNode {get => conditionMetNode; internal set => conditionMetNode = value; }
        [SerializeField] Node conditionNotMetNode;

        public Node ConditionNotMetNode {get => conditionNotMetNode; internal set => conditionNotMetNode = value; }
    }

}