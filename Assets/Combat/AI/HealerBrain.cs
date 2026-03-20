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
            // Find the most damaged ally (enemy unit with lowest HP ratio, not self)
            Unit damagedAlly = FindMostDamagedAlly(enemyUnit, allUnits);
            if (damagedAlly == null) return null; // Everyone is at full HP — skip turn

            int distance = grid.GetDistance(enemyUnit.Coordinates, damagedAlly.Coordinates);

            // Priority 1: Heal if within attack range
            if (distance >= 1 && distance <= enemyUnit.Stats.attackRange)
                return new RangedHealAction(enemyUnit, damagedAlly);

            // Priority 2: Move toward most damaged ally
            return MoveToward(enemyUnit, damagedAlly, grid, resolver);
        }

        private Unit FindMostDamagedAlly(Unit self, IReadOnlyList<Unit> allUnits)
        {
            Unit best = null;
            float bestRatio = 1.0f;

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || unit.IsPlayerControlled || unit == self) continue;
                float ratio = (float)unit.Stats.currentHealth / unit.Stats.maxHealth;
                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    best = unit;
                }
            }

            // Only return if actually damaged
            return bestRatio < 1.0f ? best : null;
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

            if (bestDist >= grid.GetDistance(mover.Coordinates, target.Coordinates))
                return null;

            return resolver.CreateMoveAction(mover, bestCell);
        }
    }
}
