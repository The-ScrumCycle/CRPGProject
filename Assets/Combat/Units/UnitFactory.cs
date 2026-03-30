using UnityEngine;
using Game.Core.Party;
using Game.Core.Transitions;
using Game.Combat.Actions;


namespace Game.Combat.Units
{
    /// <summary>
    /// Factory for creating combat units.
    /// Handles prefab instantiation and unit configuration for the unity side of things
    /// </summary>
    public class UnitFactory : MonoBehaviour
    {
        [Header("Player Configuration")]
        [SerializeField] private GameObject Character; //fallback prefab
        // Player units stats
        [SerializeField] private UnitStatsConfig CaptainStats;
        [SerializeField] private UnitStatsConfig WarriorStats;
        [SerializeField] private UnitStatsConfig ClericStats;


        [Header("Enemy Configuration")]
        [SerializeField] private UnitStatsConfig HydraStats;
        [SerializeField] private UnitStatsConfig CloseRangeSkletonStats;
        [SerializeField] private UnitStatsConfig MalakorStats;
        [SerializeField] private UnitStatsConfig RangedSkeletonStats;
        [SerializeField] private UnitStatsConfig HealerStats;
        [SerializeField] private GameObject fallbackEnemyPrefab;
        [SerializeField] private UnitStatsConfig defaultEnemyStats; 

        private int _unitIdCounter = 0;

        // Create player units
        public (Unit unit, UnitVisual visual) CreatePlayerUnit(string unitId)
        {
            UnitStatsConfig config = CaptainStats; // Default to Captain
            
            // Map the FollowerID string to stats
            if (unitId == "Warrior") config = WarriorStats; 
            else if (unitId == "Cleric") config = ClericStats; 

            UnitStats stats;
            if (PartyManager.Instance != null)
            {
                stats = config != null ? config.GetStatsForLevel(PartyManager.Instance.GetPartyLevel()) : new UnitStats(100, 20, 3, 1);
            }
            else
            {
                stats = config != null ? config.GetStatsForLevel(1) : new UnitStats(100, 20, 3, 1);
            }

            var unit = new Unit(
                    id: $"player_{_unitIdCounter++}",
                    displayName: unitId,
                    role: UnitRole.Player,
                    stats: stats,
                    aiBehavior: AIBehavior.Aggressive, // Players don't use this, but we pass it for the constructor signature
                    availableActions: config != null ? config.availableActions : new System.Collections.Generic.List<CombatActionType> { CombatActionType.MeleeAttack, CombatActionType.RangedAttack }
                ); 

            // Attempt to grab a specific prefab from your TagToPrefab dictionary, otherwise fallback to the Captain
            GameObject prefabToSpawn = Character;
            if (TagToPrefab.Instance != null)
            {
                GameObject specificPrefab = TagToPrefab.Instance.GetPrefabForTag(unitId);
                if (specificPrefab != null) prefabToSpawn = specificPrefab;
            }

            GameObject prefabInstance = Instantiate(prefabToSpawn);
            var visual = prefabInstance.AddComponent<UnitVisual>();

            return (unit, visual);
        }

        // Create an enemy unit based on transition data.
        public (Unit unit, UnitVisual visual) CreateEnemyUnit(string enemyTag, int level, string fallbackName)
        {
            GameObject prefab = null;
            if (TagToPrefab.Instance != null)
            {
                prefab = TagToPrefab.Instance.GetPrefabForTag(enemyTag);
            }
            if (prefab == null)
            {
                prefab = fallbackEnemyPrefab;
                Debug.LogWarning($"[UnitFactory] No prefab found for tag '{enemyTag}', using fallback");
            }

            AIBehavior behavior = DetermineAIBehavior(enemyTag);
            UnitStatsConfig ennemyStats = GetEnemyStatsConfig(enemyTag);
            
            UnitStats stats = ennemyStats != null ? ennemyStats.GetStatsForLevel(level) : new UnitStats(50, 15, 2, 1);

            var unit = new Unit(
                id: $"enemy_{_unitIdCounter++}",
                displayName: fallbackName,
                role: UnitRole.Enemy,
                stats: stats,
                aiBehavior: behavior,
                availableActions: ennemyStats != null ? ennemyStats.availableActions : new System.Collections.Generic.List<CombatActionType> { CombatActionType.MeleeAttack }
            ); 

            if (behavior == AIBehavior.Healer)
            {
                Debug.Log($"[Healer Spawn] tag={enemyTag}, statsConfig={(ennemyStats != null ? ennemyStats.name : "null")}, healPower={stats.healPower}, attackRange={stats.attackRange}, maxHealth={stats.maxHealth}");
            }

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
                case "skeleton_melee":
                case "CloseRangeSkeleton":
                    return CloseRangeSkletonStats;
                case "skeleton_ranged":
                    return RangedSkeletonStats;
                case "healer":
                    return HealerStats != null ? HealerStats : defaultEnemyStats;
                case "troll":
                    return defaultEnemyStats;
                case "Malakor":
                    return MalakorStats;
                default:
                    Debug.LogWarning($"[UnitFactory] No stats config found for tag '{enemyTag}', using default stats");
                    return defaultEnemyStats;
            }
        }

        private AIBehavior DetermineAIBehavior(string enemyTag)
        {
            // Enemy behavior tags, this has to match the tags we put on prefabs in unity
            switch (enemyTag.ToLower())
            {
                case "skeleton_ranged":
                    return AIBehavior.SkeletonRanged;
                case "skeleton_melee":
                    return AIBehavior.SkeletonMelee;
                case "healer":
                    return AIBehavior.Healer;
                case "hydra":
                    return AIBehavior.HydraGrappler;
                default:
                    return AIBehavior.Aggressive;
            }
        }
    }
}
