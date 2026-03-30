  using UnityEngine;                                                                                                                                                                 
  using Dialogue.Core;                                                                                                                                                               
                                                                                                                                                                                     
  public class LoaderTest : MonoBehaviour                                                                                                                                            
  {
      void Start()
      {
          var graph = DialogueGraphLoader.LoadGraph("captain");
          DialogueGraphLoader.PrintGraph(graph);
      }
  }