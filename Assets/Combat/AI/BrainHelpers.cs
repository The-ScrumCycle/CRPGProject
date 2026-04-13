using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public static class BrainHelpers
    {
        private const float RETREAT_THRESHOLD = 0.4f;

        // Find the most damaged non-player ally (excluding self). Returns null if nobody is hurt.
        public static Unit FindMostDamagedAlly(Unit self, IReadOnlyList<Unit> allUnits)
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

            return bestRatio < 1.0f ? best : null;
        }

        // Should this unit retreat to the healer? True if:
        // 1. Below health threshold
        // 2. A living healer ally exists
        // 3. This unit IS the most damaged ally (so only one retreats)
        public static bool ShouldRetreatToHealer(Unit self, IReadOnlyList<Unit> allUnits, out Unit healer)
        {
            healer = null;
            float hpRatio = (float)self.Stats.currentHealth / self.Stats.maxHealth;
            if (hpRatio >= RETREAT_THRESHOLD) return false;

            // Find a living healer
            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || unit.IsPlayerControlled || unit == self) continue;
                if (unit.AIBehavior == AIBehavior.Healer)
                {
                    healer = unit;
                    break;
                }
            }

            if (healer == null) return false;

            // Only retreat if we're the most damaged — prevents bunching
            Unit mostDamaged = FindMostDamagedAlly(healer, allUnits);
            return mostDamaged == self;
        }

        // Move toward a target unit, picking the reachable cell closest to them.
        public static ICombatAction MoveToward(Unit mover, Unit target, HexGrid grid, ActionResolver resolver)
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

        // FIXED: Added "Unit retreatTarget = null" so the internal math has a context for it!
        // Move away from the nearest player, preferring cells closer to retreatTarget.
        public static ICombatAction MoveAwayFromPlayers(Unit mover, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver, Unit retreatTarget = null)
        {
            Unit nearestPlayer = FindNearestPlayer(mover, allUnits, grid);
            if (nearestPlayer == null) return null;

            var validMoves = resolver.GetValidMoveDestinations(mover);
            if (validMoves.Count == 0) return null;

            int currentPlayerDist = grid.GetDistance(mover.Coordinates, nearestPlayer.Coordinates);
            HexCoordinates bestCell = mover.Coordinates;
            int bestPlayerDist = currentPlayerDist;
            int bestRetreatDist = retreatTarget != null ? grid.GetDistance(mover.Coordinates, retreatTarget.Coordinates) : int.MaxValue;

            foreach (var cell in validMoves)
            {
                int playerDist = grid.GetDistance(cell, nearestPlayer.Coordinates);
                int retreatDist = retreatTarget != null ? grid.GetDistance(cell, retreatTarget.Coordinates) : 0;

                // Primary: get farther from player. Secondary: get closer to retreat target.
                if (playerDist > bestPlayerDist ||
                    (playerDist == bestPlayerDist && retreatDist < bestRetreatDist))
                {
                    bestPlayerDist = playerDist;
                    bestRetreatDist = retreatDist;
                    bestCell = cell;
                }
            }

            if (bestCell == mover.Coordinates) return null;
            return resolver.CreateMoveAction(mover, bestCell);
        }

        public static Unit FindNearestPlayer(Unit fromUnit, IReadOnlyList<Unit> allUnits, HexGrid grid)
        {
            Unit nearest = null;
            int nearestDist = int.MaxValue;

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;
                int dist = grid.GetDistance(fromUnit.Coordinates, unit.Coordinates);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = unit;
                }
            }
            return nearest;
        }
    }
}