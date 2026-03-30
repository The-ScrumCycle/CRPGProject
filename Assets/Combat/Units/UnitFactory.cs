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
        [SerializeField] private UnitStatsConfig playerStats;

        [Header("Enemy Configuration")]
        [SerializeField] private GameObject fallbackEnemyPrefab;
        [SerializeField] private UnitStatsConfig defaultEnemyStats;

        [Header("References")]
        [SerializeField] private TagToPrefab tagToPrefab;

        private int _unitIdCounter = 0;

        // Create a player unit.
        public (Unit unit, UnitVisual visual) CreatePlayerUnit()
        {
            var stats = playerStats != null
                ? playerStats.ToUnitStats()
                : new UnitStats(100, 20, 3, 1);

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
            if (tagToPrefab != null)
            {
                prefab = tagToPrefab.GetPrefabForTag(enemyTag);
            }
            if (prefab == null)
            {
                prefab = fallbackEnemyPrefab;
                Debug.LogWarning($"[UnitFactory] No prefab found for tag '{enemyTag}', using fallback");
            }

            // Determine AI behavior based on tag or configuration
            AIBehavior behavior = DetermineAIBehavior(enemyTag);

            var stats = defaultEnemyStats != null
                ? defaultEnemyStats.ToUnitStats()
                : new UnitStats(50, 15, 2, 1);

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

        private AIBehavior DetermineAIBehavior(string enemyTag)
        {

            switch (enemyTag.ToLower())
            {
                // Enemy behavior tags, this has to match the tags we put on prefabs in unity
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

    // ScriptableObject for configuring unit stats in the editor.
    [CreateAssetMenu(fileName = "UnitStats", menuName = "Combat/Unit Stats Config")]
    public class UnitStatsConfig : ScriptableObject
    {
        public int maxHealth = 100;
        public int attackPower = 20;
        public int movementRange = 3;
        public int attackRange = 1;

        public UnitStats ToUnitStats()
        {
            return new UnitStats(maxHealth, attackPower, movementRange, attackRange);
        }
    }
}
