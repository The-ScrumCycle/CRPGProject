using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.AI
{
    public class SkeletonRangedBrain : IEnemyBrain
    {
        public ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            // Priority 0: Retreat to healer if badly wounded and we're the most damaged
            if (BrainHelpers.ShouldRetreatToHealer(enemyUnit, allUnits, out Unit healer))
                return BrainHelpers.MoveToward(enemyUnit, healer, grid, resolver);

            Unit target = FindNearestPlayer(enemyUnit, allUnits, grid);
            if (target == null) return null;

            int distanceToTarget = grid.GetDistance(enemyUnit.Coordinates, target.Coordinates);

            // Priority 1: Already at optimal position — ranged attack if in range
            if (distanceToTarget <= enemyUnit.Stats.attackRange)
            {
                if (enemyUnit.AvailableActions.Contains(CombatActionType.SplashAttack))
                {
                    var splash = resolver.CreateSplashAttack(enemyUnit, grid.GetCell(target.Coordinates));
                    if (resolver.Validate(splash)) return splash; 
                }
                
                if (distanceToTarget > 1) // Standard Ranged needs distance
                {
                    var ranged = resolver.CreateRangedAttack(enemyUnit, grid.GetCell(target.Coordinates));
                    if (resolver.Validate(ranged)) return ranged;
                }
                else
                {
                    // Fallback to standard Melee
                    return resolver.CreateMeleeAttack(enemyUnit, grid.GetCell(target.Coordinates));
                }
            }

            // Priority 2: Move to ideal firing distance
            int bestScore = Mathf.Abs(distanceToTarget - enemyUnit.Stats.attackRange);
            HexCoordinates bestCell = enemyUnit.Coordinates;

            var validMoves = resolver.GetValidMoveDestinations(enemyUnit);

            foreach (var cell in validMoves)
            {
                int distToTarget = grid.GetDistance(cell, target.Coordinates);
                int score = Mathf.Abs(distToTarget - enemyUnit.Stats.attackRange);

                // Prefer strictly better score, or same score but farther (stay at max range)
                if (score < bestScore || (score == bestScore && distToTarget > grid.GetDistance(bestCell, target.Coordinates)))
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            if (bestCell == enemyUnit.Coordinates) return null; // Already optimal
            return resolver.CreateMoveAction(enemyUnit, bestCell);
        }

        private Unit FindNearestPlayer(Unit fromUnit, IReadOnlyList<Unit> allUnits, HexGrid grid)
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