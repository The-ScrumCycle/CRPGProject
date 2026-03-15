using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core.Save
{

    [Serializable]
    public class StatsData
    {
        public int intelligence;
        public int charisma;
        public int strength;
        public List<string> EventFlags;

    }

}