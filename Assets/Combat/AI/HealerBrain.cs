using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public class HealerBrain : IEnemyBrain
    {
        public ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            Unit damagedAlly = BrainHelpers.FindMostDamagedAlly(enemyUnit, allUnits);

            if (damagedAlly != null)
            {
                int distance = grid.GetDistance(enemyUnit.Coordinates, damagedAlly.Coordinates);

                // Priority 1: Heal if adjacent/in range
                if (distance >= 1 && distance <= enemyUnit.Stats.attackRange)
                {
                    var healAction = resolver.CreateRangedHeal(enemyUnit, grid.GetCell(damagedAlly.Coordinates));
                    if (resolver.Validate(healAction)) return healAction;
                }

                // Priority 2: Retreat away from players (the injured ally will come to us)
                var retreat = BrainHelpers.MoveAwayFromPlayers(enemyUnit, allUnits, grid, resolver);
                if (retreat != null) return retreat;
            }

            // Nobody hurt — desperation melee if adjacent
            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;
                if (grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates) == 1)
                    return resolver.CreateMeleeAttack(enemyUnit, grid.GetCell(unit.Coordinates));
            }

            // Nobody hurt, nobody adjacent — idle
            return null;
        }
    }
}