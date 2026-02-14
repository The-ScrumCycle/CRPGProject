using UnityEngine;
using System.Collections.Generic;
public class GameState : MonoBehaviour
{
    public static GameState Instance {get; private set;}
    public HashSet<string> EventFlags {get; set;} = new();

    [SerializeField] int intelligence {get; set;}
    public int Intelligence => intelligence;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void intelligencePowerUp(int incr)
    {
        intelligence += incr;
    }

    public void setFlag(string flag)
    {
        EventFlags.add(flag);
    }

    public bool hasFlag(string flag)
    //this will be used on ConditionalNodes for npcs
    {
        return EventFlags.Contains(flag);
    }

    public bool hasIntelligence(int requiredIntelligence)
    //this is used if player has enough intelligence to say a linenode
    {
        return intelligence >= requiredIntelligence;
    }

}