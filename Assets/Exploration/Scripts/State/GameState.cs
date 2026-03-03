using UnityEngine;
using System.Collections.Generic;

namespace State
    {
    public class GameState : MonoBehaviour
    {
        public static GameState Instance {get; private set;}
        public HashSet<string> EventFlags {get; set;} = new();

        [SerializeField] private int intelligence;
        [SerializeField] private int charisma;
        [SerializeField] private int strength;
        public int Intelligence => intelligence;
        public int Charisma => charisma;
        public int Strength => strength;


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

        public void charismaPowerUp(int incr)
        {
            charisma += incr;
        }

        public void strengthPowerUp(int incr)
        {
            strength += incr;
        }

        public void resetStats()
        {
            intelligence = 0;
            charisma = 0;
            strength = 0;
        }

        public void setFlag(string flag)
        {
            EventFlags.Add(flag);
        }

        public void removeFlag(string flag)
        {
            EventFlags.Remove(flag);
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

        public bool hasCharisma(int requiredCharisma)
        {
            return charisma >= requiredCharisma;
        }

        public bool hasStrength(int requiredStrength)
        {
            return strength >= requiredStrength;
        }

    }
}