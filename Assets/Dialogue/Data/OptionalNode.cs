using UnityEngine;
using System.Collections.Generic;
namespace Dialogue.Data
{
    public class OptionNode : Node
    {
        [SerializeField] private List<Node> options = new List<Node>();

        public List<Node> Options { get => options; internal set => options = value; }

        public List<string> getOptionsText()
        /*
        helper function for UI, returns a list of string to display for the options
        
        */
        {
            List<string> optionsText = new List<string>();
            foreach (var option in options)
            {
                if (option is LineNode lineNode)
                {
                    optionsText.Add(lineNode.LineText);
                }
            }
            return optionsText;
        }

    }
}