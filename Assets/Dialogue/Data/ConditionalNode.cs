using UnityEngine;
using System.Collections.Generic;
namespace Dialogue.Data
{
    public class ConditionalNode : Node
    {
        [SerializeField] private string eventName;
        public string EventName => eventName;
        [SerializeField] Node conditionMetNode;

        public Node ConditionMetNode => conditionMetNode;
        [SerializeField] Node conditionNotMetNode;

        public Node ConditionNotMetNode => conditionNotMetNode;
    }

}