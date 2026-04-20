using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.AI
{
    public class SkeletonRangedBrain : IEnemyBrain
    {
        public IEnumerable<ICombatAction> GenerateCandidateActions(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            if (BrainHelpers.ShouldRetreatToHealer(enemyUnit, allUnits, out Unit healer))
            {
                var retreat = BrainHelpers.MoveToward(enemyUnit, healer, grid, resolver);
                if (retreat != null)
                {
                    yield return retreat;
                }
            }

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled)
                {
                    continue;
                }

                int distanceToTarget = grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates);

                if (distanceToTarget <= enemyUnit.Stats.attackRange)
                {
                    if (enemyUnit.AvailableActions.Contains(CombatActionType.SplashAttack))
                    {
                        var splash = resolver.CreateSplashAttack(enemyUnit, grid.GetCell(unit.Coordinates));
                        if (resolver.Validate(splash))
                        {
                            yield return splash;
                        }
                    }

                    if (distanceToTarget > 1)
                    {
                        var ranged = resolver.CreateRangedAttack(enemyUnit, grid.GetCell(unit.Coordinates));
                        if (resolver.Validate(ranged))
                        {
                            yield return ranged;
                        }
                    }
                }

                if (distanceToTarget == 1)
                {
                    yield return resolver.CreateMeleeAttack(enemyUnit, grid.GetCell(unit.Coordinates));
                }

                var reposition = MoveToOptimalRange(enemyUnit, unit, grid, resolver);
                if (reposition != null)
                {
                    yield return reposition;
                }
            }
        }

        private ICombatAction MoveToOptimalRange(Unit mover, Unit target, HexGrid grid, ActionResolver resolver)
        {
            int currentDist = grid.GetDistance(mover.Coordinates, target.Coordinates);
            int bestScore = Mathf.Abs(currentDist - mover.Stats.attackRange);
            HexCoordinates bestCell = mover.Coordinates;

            var validMoves = resolver.GetValidMoveDestinations(mover);

            foreach (var cell in validMoves)
            {
                int distToTarget = grid.GetDistance(cell, target.Coordinates);
                int score = Mathf.Abs(distToTarget - mover.Stats.attackRange);

                if (score < bestScore || (score == bestScore && distToTarget > grid.GetDistance(bestCell, target.Coordinates)))
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            if (bestCell == mover.Coordinates) return null;
            return resolver.CreateMoveAction(mover, bestCell);
        }
    }
}