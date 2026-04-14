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
        public static MoveAction MoveToward(Unit mover, Unit target, HexGrid grid, ActionResolver resolver)
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

        // Move away from the nearest player, preferring cells closer to retreatTarget.
        public static MoveAction MoveAwayFromPlayers(Unit mover, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver, Unit retreatTarget = null)
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

        // Move into healing range of an ally while still preferring safer cells.
        public static MoveAction MoveToHealingPosition(Unit mover, Unit healTarget, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            if (mover == null || healTarget == null || !healTarget.IsAlive)
            {
                return null;
            }

            var validMoves = resolver.GetValidMoveDestinations(mover);
            if (validMoves.Count == 0)
            {
                return null;
            }

            Unit nearestPlayer = FindNearestPlayer(mover, allUnits, grid);
            HexCoordinates currentCell = mover.Coordinates;
            HexCoordinates bestCell = currentCell;
            int bestSupportDistance = grid.GetDistance(currentCell, healTarget.Coordinates);
            bool bestInHealRange = bestSupportDistance >= 1 && bestSupportDistance <= mover.Stats.attackRange;
            int bestThreatDistance = nearestPlayer != null ? grid.GetDistance(currentCell, nearestPlayer.Coordinates) : int.MinValue;

            foreach (var cell in validMoves)
            {
                int supportDistance = grid.GetDistance(cell, healTarget.Coordinates);
                bool inHealRange = supportDistance >= 1 && supportDistance <= mover.Stats.attackRange;
                int threatDistance = nearestPlayer != null ? grid.GetDistance(cell, nearestPlayer.Coordinates) : int.MinValue;

                if (!bestInHealRange && inHealRange)
                {
                    bestCell = cell;
                    bestSupportDistance = supportDistance;
                    bestInHealRange = true;
                    bestThreatDistance = threatDistance;
                    continue;
                }

                if (bestInHealRange == inHealRange)
                {
                    if (inHealRange)
                    {
                        if (threatDistance > bestThreatDistance ||
                            (threatDistance == bestThreatDistance && supportDistance < bestSupportDistance))
                        {
                            bestCell = cell;
                            bestSupportDistance = supportDistance;
                            bestThreatDistance = threatDistance;
                        }
                    }
                    else if (supportDistance < bestSupportDistance ||
                             (supportDistance == bestSupportDistance && threatDistance > bestThreatDistance))
                    {
                        bestCell = cell;
                        bestSupportDistance = supportDistance;
                        bestThreatDistance = threatDistance;
                    }
                }
            }

            if (bestCell == currentCell)
            {
                return null;
            }

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
