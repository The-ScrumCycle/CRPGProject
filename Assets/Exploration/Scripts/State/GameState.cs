using UnityEngine;
using System.Collections.Generic;
using Core.Save;

namespace State
{
    public class GameState : MonoBehaviour, ISaveable
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

        public void Start()
        {
            SaveManager.Instance.Register(this);
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

        public bool isFlagsEmpty()
        {
            return EventFlags.Count == 0;
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

        public void SetSaveData(SaveData saveData)
        {
            saveData.stats.intelligence = this.intelligence;
            saveData.stats.charisma = this.charisma;
            saveData.stats.strength = this.strength;
            saveData.stats.EventFlags = new List<string>(this.EventFlags);
        }
        public void LoadSaveData(SaveData saveData)
        {
            intelligence = saveData.stats.intelligence;
            charisma = saveData.stats.charisma;
            strength = saveData.stats.strength;
            EventFlags = new HashSet<string>(saveData.stats.EventFlags);
        }
        
        public int GetIntelligence()
        {
            return intelligence;
        }

        public int GetCharisma()
        {
            return charisma;
        }

        public int GetStrength()
        {
            return strength;
        }

    }
}