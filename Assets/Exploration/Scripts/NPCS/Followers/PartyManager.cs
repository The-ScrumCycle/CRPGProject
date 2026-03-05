using UnityEngine;
using System.Collections.Generic;


public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }
    private List<FollowerID> activeFollowers = new List<FollowerID>();
    [SerializeField] int partyLevel = 1;
    [SerializeField] int partyExperience = 0;
    [SerializeField] int experienceToNextLevel = 100;
    [SerializeField] int experienceGrowthRate = 50; 

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

    }


    // adding and removing followers from active list
    public void AddFollowerActive(FollowerID followerID)
    {
        if (activeFollowers.Contains(followerID)) return;
        activeFollowers.Add(followerID);
    }
    public void RemoveFollowerActive(FollowerID followerID)
    {
        activeFollowers.Remove(followerID);
    }

    public bool IsFollowerActive(FollowerID followerID)
    {
        return activeFollowers.Contains(followerID);
    }

    public int GetPartyLevel()
    {
        return partyLevel;
    }

    private void LevelUp()
    {
        if(partyExperience >= experienceToNextLevel)
        {
            partyExperience -= experienceToNextLevel;
            partyLevel++;
            experienceToNextLevel += experienceGrowthRate;
        }

        if (partyExperience >= experienceToNextLevel) LevelUp();
    }

    public void GainExperience(int amount)
    {
        partyExperience += amount;
        LevelUp();
    }



}
