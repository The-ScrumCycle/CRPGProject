using UnityEngine;
using Game.Core.Party;
using Game.Core.Transitions;


namespace Game.Combat.Units
{
    /// <summary>
    /// Factory for creating combat units.
    /// Handles prefab instantiation and unit configuration for the unity side of things
    /// </summary>
    public class UnitFactory : MonoBehaviour
    {
        [Header("Player Configuration")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private UnitStatsConfig HeroStats;
        [SerializeField] private UnitStatsConfig JohnStats;


        [Header("Enemy Configuration")]
        [SerializeField] private UnitStatsConfig HydraStats;
        [SerializeField] private UnitStatsConfig CloseRangeSkletonStats;
        [SerializeField] private GameObject fallbackEnemyPrefab;
        [SerializeField] private UnitStatsConfig defaultEnemyStats;

        private int _unitIdCounter = 0;

        // Create a player unit.
        public (Unit unit, UnitVisual visual) CreatePlayerUnit()
        {

            UnitStats stats;

            // if party manager does not exist, return default stats
            if (PartyManager.Instance != null)
            {
                stats = HeroStats != null
                ? HeroStats.GetStatsForLevel(PartyManager.Instance.GetPartyLevel())
                : new UnitStats(100, 20, 3, 1);
            }
            else
            {
                stats = new UnitStats(100, 20, 3, 1);
            }

            var unit = new Unit(
                    id: $"player_{_unitIdCounter++}",
                    displayName: "Captain",
                    role: UnitRole.Player,
                    stats: stats
                );

            GameObject prefabInstance = Instantiate(playerPrefab);
            var visual = prefabInstance.AddComponent<UnitVisual>();

            return (unit, visual);
        }

        // Create an enemy unit based on transition data.
        public (Unit unit, UnitVisual visual) CreateEnemyUnit(CombatTransitionData transitionData)
        {
            string enemyTag = transitionData?.enemyTag ?? "Enemy";

            // Get appropriate prefab
            GameObject prefab = null;
            if (TagToPrefab.Instance != null)
            {
                Debug.Log(enemyTag);
                prefab = TagToPrefab.Instance.GetPrefabForTag(enemyTag);
            }
            if (prefab == null)
            {
                prefab = fallbackEnemyPrefab;
                Debug.LogWarning($"[UnitFactory] No prefab found for tag '{enemyTag}', using fallback");
            }

            // Determine AI behavior based on tag or configuration
            AIBehavior behavior = DetermineAIBehavior(enemyTag);

            UnitStatsConfig ennemyStats = GetEnemyStatsConfig(enemyTag);
            UnitStats stats;
            // if transitionData does not exist, return default stats
            if (transitionData != null)
            {

                Debug.Log("ennemy level is " + transitionData.ennemyLevel);
                stats = ennemyStats != null
               ? ennemyStats.GetStatsForLevel(transitionData.ennemyLevel)
               : new UnitStats(50, 15, 2, 1);
            }
            else
            {
                stats = new UnitStats(50, 15, 3, 1);
            }

            var unit = new Unit(
                id: $"enemy_{_unitIdCounter++}",
                displayName: transitionData?.enemyName ?? "Enemy",
                role: UnitRole.Enemy,
                stats: stats,
                aiBehavior: behavior
            );

            GameObject prefabInstance = Instantiate(prefab);
            var visual = prefabInstance.AddComponent<UnitVisual>();

            return (unit, visual);
        }

        // get correct UnitStatsConfig for ennemy 
        private UnitStatsConfig GetEnemyStatsConfig(string enemyTag)
        {
            switch (enemyTag)
            {
                case "Hydra":
                    return HydraStats;
                case "CloseRangeSkeleton":
                    return CloseRangeSkletonStats;
                case "troll":
                    return defaultEnemyStats;
                default:
                    Debug.LogWarning($"[UnitFactory] No stats config found for tag '{enemyTag}', using default stats");
                    return defaultEnemyStats; 
            }
        }



        private AIBehavior DetermineAIBehavior(string enemyTag)
        {
            // Simple logic that we can expand based on tag or configuration
            switch (enemyTag)
            {
                case "ranged":
                case "cannon":
                case "mage":
                    return AIBehavior.Defensive;
                default:
                    return AIBehavior.Aggressive;
            }
        }
    }

  
}
