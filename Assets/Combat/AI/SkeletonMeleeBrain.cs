using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public class SkeletonMeleeBrain : IEnemyBrain
    {
        public ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            // Priority 0: Retreat to healer if badly wounded and we're the most damaged
            if (BrainHelpers.ShouldRetreatToHealer(enemyUnit, allUnits, out Unit healer))
                return BrainHelpers.MoveToward(enemyUnit, healer, grid, resolver);

            // Priority 1: Attack any adjacent player unit immediately
            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;
                if (grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates) == 1)
                    return resolver.CreateMeleeAttack(enemyUnit, unit);
            }

            // Priority 2: Chase the lowest-HP player unit
            Unit huntTarget = FindLowestHpPlayer(allUnits);
            if (huntTarget == null) return null;

            return MoveToward(enemyUnit, huntTarget, grid, resolver);
        }

        private Unit FindLowestHpPlayer(IReadOnlyList<Unit> allUnits)
        {
            Unit lowest = null;
            int lowestHp = int.MaxValue;

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;
                if (unit.Stats.currentHealth < lowestHp)
                {
                    lowestHp = unit.Stats.currentHealth;
                    lowest = unit;
                }
            }
            return lowest;
        }

        private MoveAction MoveToward(Unit mover, Unit target, HexGrid grid, ActionResolver resolver)
        {
            var validMoves = resolver.GetValidMoveDestinations(mover);
            if (validMoves.Count == 0) return null;

            HexCoordinates bestCell = validMoves[0];
            int bestDist = grid.GetDistance(validMoves[0], target.Coordinates);

            for (int i = 1; i < validMoves.Count; i++)
            {
                int dist = grid.GetDistance(validMoves[i], target.Coordinates);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCell = validMoves[i];
                }
            }

            // Only move if it actually gets us closer
            if (bestDist >= grid.GetDistance(mover.Coordinates, target.Coordinates))
                return null;

            return resolver.CreateMoveAction(mover, bestCell);
        }
    }
}
