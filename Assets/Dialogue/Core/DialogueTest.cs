using UnityEngine;
using Dialogue.Data;
using System.Collections.Generic;

namespace Dialogue.Core
{
    public class DialogueTest : MonoBehaviour
    {
        private DialogueRunner runner;

        void Start()
        {
            if (GameState.Instance == null)                                                                                                                             
            {                                                                                                                                                           
                var gsObj = new GameObject("GameState");                                                                                                                
                gsObj.AddComponent<GameState>();                                                                                                                        
            }   
            runner = gameObject.AddComponent<DialogueRunner>();

            // Subscribe to events
            runner.DialogueStartAction += () => Debug.Log("=== DIALOGUE STARTED ===");
            runner.LineNodeAction += (text) => Debug.Log($"LINE: {text}");
            runner.OptionNodeAction += (options) => {
                Debug.Log("OPTIONS:");
                for (int i = 0; i < options.Count; i++)
                    Debug.Log($"  [{i}] {options[i]}");
            };
            runner.DialogueEndAction += () => Debug.Log("=== DIALOGUE ENDED ===");

            // Run all tests
            TestBasicDialogue();
            TestOptionNodeWithIntelligence();
            TestSelectOptions();
            TestConditionalNode_FlagMet();
            TestConditionalNode_FlagNotMet();
            TestEndDialogue();
        }

        void TestBasicDialogue()
        {
            Debug.Log("--- Test: Basic Dialogue Flow ---");

            var line3 = ScriptableObject.CreateInstance<LineNode>();
            var line2 = ScriptableObject.CreateInstance<LineNode>();
            var line1 = ScriptableObject.CreateInstance<LineNode>();

            SetPrivateField(line3, "lineText", "Safe travels!");
            SetPrivateField(line3, "nextNode", null);

            SetPrivateField(line2, "lineText", "Welcome to the village.");
            SetPrivateField(line2, "nextNode", line3);

            SetPrivateField(line1, "lineText", "Hello traveler!");
            SetPrivateField(line1, "nextNode", line2);

            var graph = new DialogueGraph(line1);
            SetPrivateField(runner, "dialogueGraph", graph);

            runner.BeginDialogue();  // "Hello traveler!"
            runner.Next();           // "Welcome to the village."
            runner.Next();           // "Safe travels!"
            runner.Next();           // Should end dialogue
        }

        void TestOptionNodeWithIntelligence()
        {
            Debug.Log("--- Test: OptionNode With Intelligence Filter ---");

            // Set player intelligence to 5
            var gs = GameState.Instance;
            SetBackingField(gs, "intelligence", 5);

            var optA = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(optA, "lineText", "Easy option (req 0)");
            SetPrivateField(optA, "minimumIntelligence", 0);

            var optB = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(optB, "lineText", "Hard option (req 10)");
            SetPrivateField(optB, "minimumIntelligence", 10);

            var optC = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(optC, "lineText", "Medium option (req 5)");
            SetPrivateField(optC, "minimumIntelligence", 5);

            var optionNode = ScriptableObject.CreateInstance<OptionNode>();
            SetPrivateField(optionNode, "options", new List<Node> { optA, optB, optC });

            var graph = new DialogueGraph(optionNode);
            SetPrivateField(runner, "dialogueGraph", graph);

            // Should show Easy and Medium, filter out Hard
            runner.BeginDialogue();
        }

        void TestSelectOptions()
        {
            Debug.Log("--- Test: SelectOptions ---");

            var gs = GameState.Instance;
            SetBackingField(gs, "intelligence", 5);

            var nextLine = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(nextLine, "lineText", "You chose to negotiate.");
            SetPrivateField(nextLine, "nextNode", null);

            var optA = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(optA, "lineText", "Fight");
            SetPrivateField(optA, "minimumIntelligence", 0);
            SetPrivateField(optA, "nextNode", null);

            var optB = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(optB, "lineText", "Negotiate");
            SetPrivateField(optB, "minimumIntelligence", 0);
            SetPrivateField(optB, "nextNode", nextLine);

            var optionNode = ScriptableObject.CreateInstance<OptionNode>();
            SetPrivateField(optionNode, "options", new List<Node> { optA, optB });

            var graph = new DialogueGraph(optionNode);
            SetPrivateField(runner, "dialogueGraph", graph);

            runner.BeginDialogue();       // Shows options
            runner.SelectOptions(1);      // Pick "Negotiate" -> LINE: Negotiate
            runner.Next();                // LINE: You chose to negotiate.
            runner.Next();                // nextNode is null -> ends dialogue
        }

        void TestConditionalNode_FlagMet()
        {
            Debug.Log("--- Test: ConditionalNode (Flag Met) ---");

            var gs = GameState.Instance;
            gs.setFlag("village_saved");

            var metLine = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(metLine, "lineText", "Village already saved!");
            SetPrivateField(metLine, "nextNode", null);

            var notMetLine = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(notMetLine, "lineText", "Village needs help.");
            SetPrivateField(notMetLine, "nextNode", null);

            var condNode = ScriptableObject.CreateInstance<ConditionalNode>();
            SetPrivateField(condNode, "eventName", "village_saved");
            SetPrivateField(condNode, "conditionMetNode", metLine);
            SetPrivateField(condNode, "conditionNotMetNode", notMetLine);

            var graph = new DialogueGraph(condNode);
            SetPrivateField(runner, "dialogueGraph", graph);

            // Should route to metLine since flag is set
            runner.BeginDialogue();
            runner.Next(); // end
        }

        void TestConditionalNode_FlagNotMet()
        {
            Debug.Log("--- Test: ConditionalNode (Flag Not Met) ---");

            var metLine = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(metLine, "lineText", "You defeated the dragon!");
            SetPrivateField(metLine, "nextNode", null);

            var notMetLine = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(notMetLine, "lineText", "The dragon still terrorizes us.");
            SetPrivateField(notMetLine, "nextNode", null);

            var condNode = ScriptableObject.CreateInstance<ConditionalNode>();
            SetPrivateField(condNode, "eventName", "dragon_defeated");
            SetPrivateField(condNode, "conditionMetNode", metLine);
            SetPrivateField(condNode, "conditionNotMetNode", notMetLine);

            var graph = new DialogueGraph(condNode);
            SetPrivateField(runner, "dialogueGraph", graph);

            // "dragon_defeated" flag is NOT set, should route to notMetLine
            runner.BeginDialogue();
            runner.Next(); // end
        }

        void TestEndDialogue()
        {
            Debug.Log("--- Test: EndDialogue ---");

            var line = ScriptableObject.CreateInstance<LineNode>();
            SetPrivateField(line, "lineText", "Some dialogue.");
            SetPrivateField(line, "nextNode", null);

            var graph = new DialogueGraph(line);
            SetPrivateField(runner, "dialogueGraph", graph);

            runner.BeginDialogue();
            runner.EndDialogue(); // Should fire DialogueEndAction
        }

        void Update()
        {
            // Manual testing: Space to advance, 1/2/3 for options
            if (Input.GetKeyDown(KeyCode.Space))
                runner.Next();
            if (Input.GetKeyDown(KeyCode.Alpha1))
                runner.SelectOptions(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                runner.SelectOptions(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                runner.SelectOptions(2);
        }

        // Helper to set private [SerializeField] fields via reflection
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogError($"Could not find field '{fieldName}' on {obj.GetType().Name}");
        }

        // Helper for auto-property backing fields (e.g. GameState.intelligence)
        private void SetBackingField(object obj, string propertyName, object value)
        {
            var backingName = $"<{propertyName}>k__BackingField";
            var field = obj.GetType().GetField(backingName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }
            SetPrivateField(obj, propertyName, value);
        }
    }
}