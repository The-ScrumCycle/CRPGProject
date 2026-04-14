using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public class HealerBrain : IEnemyBrain
    {
        public IEnumerable<ICombatAction> GenerateCandidateActions(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            Unit retreatTarget = BrainHelpers.FindMostDamagedAlly(enemyUnit, allUnits);

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || unit.IsPlayerControlled || unit == enemyUnit)
                {
                    continue;
                }

                if (unit.Stats.currentHealth >= unit.Stats.maxHealth)
                {
                    continue;
                }

                int distance = grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates);
                if (distance >= 1 && distance <= enemyUnit.Stats.attackRange)
                {
                    yield return new RangedHealAction(enemyUnit, unit);
                }
            }

            if (retreatTarget != null)
            {
                var retreat = BrainHelpers.MoveAwayFromPlayers(enemyUnit, allUnits, grid, resolver, retreatTarget);
                if (retreat != null)
                {
                    yield return retreat;
                }
            }

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;
                if (grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates) == 1)
                {
                    yield return resolver.CreateMeleeAttack(enemyUnit, unit);
                }
            }
        }
    }
}
