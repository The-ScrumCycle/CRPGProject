using System.Collections.Generic;
using UnityEngine;
using Game.Combat.Grid;
using Game.Combat.Units;
using Game.Combat.Actions;

namespace Game.Combat
{
    ///<summary>
    /// Class that controls all AI decision making logic e.g where we implement AI heuristics for decision making
    /// Responsible for generating enemy AI action intents and creating enemy AI turn plans.
    ///<summary>
    public class EnemyDecisionService
    {
        private readonly HexGrid _grid;
        private readonly ActionResolver _actionResolver;

        public EnemyDecisionService(HexGrid grid, ActionResolver actionResolver)
        {
            _grid = grid;
            _actionResolver = actionResolver;
        }

        public ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits)
        {
            if (enemyUnit == null || !enemyUnit.IsAlive)
            {
                return null;
            }

            Unit targetUnit = FindNearestPlayerUnit(enemyUnit, allUnits);

            if (targetUnit == null)
            {
                return null;
            }

            int distanceToTarget = _grid.GetDistance(enemyUnit.Coordinates, targetUnit.Coordinates);

            ICombatAction action = null;

            switch (enemyUnit.AIBehavior)
            {
                case AIBehavior.Aggressive:
                    if (distanceToTarget == 1)
                    {
                        action = _actionResolver.CreateMeleeAttack(enemyUnit, targetUnit);
                    }
                    else
                    {
                        action = CreateMoveTowardTarget(enemyUnit, targetUnit);
                    }
                    break;

                case AIBehavior.Defensive:
                    if (distanceToTarget <= enemyUnit.Stats.attackRange && distanceToTarget > 1)
                    {
                        action = _actionResolver.CreateRangedAttack(enemyUnit, targetUnit);
                    }
                    else if (distanceToTarget == 1)
                    {
                        action = CreateMoveAwayFromTarget(enemyUnit, targetUnit);
                        if (action == null || !_actionResolver.Validate(action))
                        {
                            action = _actionResolver.CreateMeleeAttack(enemyUnit, targetUnit);
                        }
                    }
                    else
                    {
                        action = CreateMoveTowardTarget(enemyUnit, targetUnit);
                    }
                    break;

                default:
                    action = CreateMoveTowardTarget(enemyUnit, targetUnit);
                    break;
            }

            return action;
        }

        public void GenerateAllIntents(CombatRuntimeState state)
        {
            state.ClearIntents();

            foreach (var unit in state.AllUnits)
            {
                if (!unit.IsAlive || unit.IsPlayerControlled) continue;

                var intent = GenerateIntentForEnemy(unit, state.AllUnits);
                if (intent != null)
                {
                    state.AddIntent(intent);
                }
            }
        }

        public ActionIntent GenerateIntentForEnemy(Unit enemy, IReadOnlyList<Unit> allUnits)
        {
            var action = DecideAction(enemy, allUnits);
            if (action == null) return null;

            return _actionResolver.Preview(action);
        }

        public MoveAction CreateMoveTowardTarget(Unit mover, Unit target)
        {
            var reachableCells = _grid.GetReachableCells(mover.Coordinates, mover.Stats.movementRange);
            HexCoordinates bestDestination = mover.Coordinates;
            int bestDistance = _grid.GetDistance(mover.Coordinates, target.Coordinates);

            foreach (var cell in reachableCells)
            {
                if (!cell.CanEnter()) continue;

                int distance = _grid.GetDistance(cell.Coordinates, target.Coordinates);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestDestination = cell.Coordinates;
                }
            }

            if (bestDestination != mover.Coordinates)
            {
                return _actionResolver.CreateMoveAction(mover, bestDestination);
            }

            return null;
        }

        public MoveAction CreateMoveAwayFromTarget(Unit mover, Unit target)
        {
            var reachableCells = _grid.GetReachableCells(mover.Coordinates, mover.Stats.movementRange);
            HexCoordinates bestDestination = mover.Coordinates;
            int bestDistance = _grid.GetDistance(mover.Coordinates, target.Coordinates);

            foreach (var cell in reachableCells)
            {
                if (!cell.CanEnter()) continue;

                int distance = _grid.GetDistance(cell.Coordinates, target.Coordinates);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestDestination = cell.Coordinates;
                }
            }

            if (bestDestination != mover.Coordinates)
            {
                return _actionResolver.CreateMoveAction(mover, bestDestination);
            }

            return null;
        }

        public Unit FindNearestPlayerUnit(Unit fromUnit, IReadOnlyList<Unit> allUnits)
        {
            Unit nearest = null;
            int nearestDistance = int.MaxValue;

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;

                int distance = _grid.GetDistance(fromUnit.Coordinates, unit.Coordinates);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = unit;
                }
            }

            return nearest;
        }
    }
}
