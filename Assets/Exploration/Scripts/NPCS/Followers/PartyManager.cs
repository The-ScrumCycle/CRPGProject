using UnityEngine;
using System.Collections.Generic;


public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }
    private List<FollowerID> ActiveFollowers = new List<FollowerID>();

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
        if (ActiveFollowers.Contains(followerID)) return;
        ActiveFollowers.Add(followerID);
    }
    public void RemoveFollowerActive(FollowerID followerID)
    {
        ActiveFollowers.Remove(followerID);
    }

    public bool IsFollowerActive(FollowerID followerID)
    {
        return ActiveFollowers.Contains(followerID);
    }


}
