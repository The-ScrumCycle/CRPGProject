using UnityEngine;
using Game.Combat;
using Game.Core.Party;
using Game.Core.Transitions;
using Game.Combat.Actions;

namespace Game.Combat.Units
{
    // ScriptableObject for configuring unit stats in the editor.
    [CreateAssetMenu(fileName = "UnitStats", menuName = "Combat/Unit Stats Config")]
    public class UnitStatsConfig : ScriptableObject
    {
        [Header("Action Loadout")]
        public System.Collections.Generic.List<CombatActionType> availableActions;

        [Header("Combat Stats")]
        public int maxHealth = 100;
        public int attackPower = 20;

        [Header("Movimentation Stats")]
        public int movementRange = 3;
        public int attackRange = 1;

        [Header("Upgrades Stats")]
        private int upgradeHealth = 15;
        private int upgradePower   = 5;
        private int upgradeMovement = 1;

        // Get stats at current level
        public UnitStats GetStatsForLevel(int level)
        {
            int currentHealth        = maxHealth     + (level - 1) * upgradeHealth;
            int currentAttackPower   = attackPower   + (level - 1) * upgradePower;
            int currentMovementRange = movementRange + (level - 1) * upgradeMovement / 2;

            return new UnitStats(currentHealth, currentAttackPower, currentMovementRange, attackRange);

        }

        public UnitStats ToUnitStats()
        {
            return new UnitStats(maxHealth, attackPower, movementRange, attackRange);
        }
    }
}