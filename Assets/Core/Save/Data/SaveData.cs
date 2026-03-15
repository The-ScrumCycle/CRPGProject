using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core.Save
{

    [Serializable]
    public class SaveData
    {
        public PlayerData player;//done
        public StatsData stats;//done
        public PartyData party;//done
        public EnemyData enemy;//done
        public string saveDateTime;
        public byte[] screenshotData;

        public SaveData()
        {
            player = new PlayerData();
            stats = new StatsData();
            party = new PartyData();
            enemy = new EnemyData();
        }
    }


}