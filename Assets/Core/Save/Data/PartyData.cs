using UnityEngine;
using System;
using System.Collections.Generic;


namespace Core.Save
{
    

    [Serializable]
    public class PartyData
    {
        public int partyLevel;
        public int partyExperience;
        public int experienceToNextLevel;
        public List<int> activeFollowers;
    }

}