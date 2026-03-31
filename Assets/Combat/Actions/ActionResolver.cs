using Game.Combat.Units;
using Game.Combat.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Validates and executes combat actions.
    /// Manages all truth for action resolution
    /// </summary>
    public class ActionResolver
    {
        private readonly HexGrid _grid;

        public ActionResolver(HexGrid grid)
        {
            _grid = grid;
        }

        // Validate an action without executing it.
        public bool Validate(ICombatAction action)
        {
            if (action == null || action.Actor == null)
            {
                return false;
            }

            if (!action.Actor.IsAlive)
            {
                return false;
            }

            return action.IsValid(_grid);
        }

        // Execute an action if valid.
        // Returns true if action was executed.
        public bool Execute(ICombatAction action)
        {
            if (!Validate(action))
            {
                Debug.LogWarning($"[ActionResolver] Invalid action: {action?.GetType().Name}");
                return false;
            }

            action.Execute(_grid);
            Debug.Log($"[ActionResolver] Executed: {action.Actor.DisplayName} -> {action.GetType().Name}");
            return true;
        }

        // Generate a preview (ActionIntent) for an action.
        public ActionIntent Preview(ICombatAction action)
        {
            if (action == null || action.Actor == null) return null;
            bool isValid = Validate(action);
            if (!isValid) return null;

            Unit targetUnit = null;
            int predictedDamage = 0;
            ActionVisualType visualType = ActionVisualType.None;
            List<HexCoordinates> movementPath = null;
            
            HexCoordinates? pushDestination = null;
            bool targetTakesBumpDamage = false;
            Unit secondaryBumpTarget = null;
            const int BUMP_DAMAGE = 10;

            if (action is MoveAction move)
            {
                visualType = ActionVisualType.Move;
                movementPath = new List<HexCoordinates>(move.Path);
            }
            else if (action is MeleeAttackAction melee || action is RangedAttackAction ranged)
            {
                visualType = action is MeleeAttackAction ? ActionVisualType.MeleeAttack : ActionVisualType.RangedAttack;
                targetUnit = action is MeleeAttackAction ? ((MeleeAttackAction)action).Target : ((RangedAttackAction)action).Target;
                predictedDamage = action is MeleeAttackAction ? ((MeleeAttackAction)action).Damage : ((RangedAttackAction)action).Damage;

                // Push Calculation (Pure preview, no grid mutation)
                HexCoordinates bestPush = targetUnit.Coordinates.GetPushDestination(action.Actor.Coordinates); 

                pushDestination = bestPush;
                var destCell = _grid.GetCell(bestPush);

                if (destCell == null || !destCell.IsWalkable)
                {
                    predictedDamage += BUMP_DAMAGE;
                    targetTakesBumpDamage = true;
                    pushDestination = null; // Stays in place
                }
                else if (destCell.IsOccupied)
                {
                    predictedDamage += BUMP_DAMAGE;
                    targetTakesBumpDamage = true;
                    secondaryBumpTarget = destCell.Occupant;
                    pushDestination = null; // Stays in place
                }
            }
            else if (action is GrappleAction grapple)
            {
                visualType = ActionVisualType.Grapple;
                targetUnit = grapple.Target;
                predictedDamage = 0;
            }
            else if (action is RangedHealAction heal)
            {
                visualType = ActionVisualType.Heal;
                targetUnit = heal.Target;
                predictedDamage = -heal.healAmount; // Negative damage acts as healing for the UI
            }
            // --- Enemy AOE damage attacks ---
            else if (action is SweepAttackAction sweep)
            {
                visualType = ActionVisualType.MeleeAttack;
                predictedDamage = action.Actor.Stats.attackPower;
                
                // Grab the first valid target to satisfy the base ActionIntent constructor
                foreach (var cellCoord in sweep.GetTargetCells())
                {
                    var cell = _grid.GetCell(cellCoord);
                    if (cell?.Occupant != null && cell.Occupant != action.Actor)
                    {
                        targetUnit = cell.Occupant;
                        break;
                    }
                }
            }
            else if (action is SplashAttackAction splash)
            {
                visualType = ActionVisualType.RangedAttack;
                predictedDamage = action.Actor.Stats.attackPower;

                // Grab the first valid target to satisfy the base ActionIntent constructor
                foreach (var cellCoord in splash.GetTargetCells())
                {
                    var cell = _grid.GetCell(cellCoord);
                    if (cell?.Occupant != null && cell.Occupant != action.Actor)
                    {
                        targetUnit = cell.Occupant;
                        break;
                    }
                }
            }

            return new ActionIntent(
                action.Actor, action, targetUnit, predictedDamage, visualType,
                movementPath, isValid, pushDestination, targetTakesBumpDamage, secondaryBumpTarget
            );
        } 

        // Get all valid move destinations for a unit.
        public List<HexCoordinates> GetValidMoveDestinations(Unit unit)
        {
            var validDestinations = new List<HexCoordinates>();

            if (unit == null || !unit.IsAlive)
            {
                return validDestinations;
            }

            var reachableCells = _grid.GetReachableCells(unit.Coordinates, unit.Stats.movementRange);

            foreach (var cell in reachableCells)
            {
                if (cell.CanEnter())
                {
                    validDestinations.Add(cell.Coordinates);
                }
            }

            return validDestinations;
        }

        // Get all valid attack targets for a unit (melee).
        public List<Unit> GetValidMeleeTargets(Unit attacker)
        {
            var validTargets = new List<Unit>();

            if (attacker == null || !attacker.IsAlive)
            {
                return validTargets;
            }

            var neighbors = _grid.GetNeighbors(attacker.Coordinates);

            foreach (var cell in neighbors)
            {
                if (cell.Occupant != null &&
                    cell.Occupant.IsAlive &&
                    cell.Occupant.Role != attacker.Role)
                {
                    validTargets.Add(cell.Occupant);
                }
            }

            return validTargets;
        }

        // Get all valid attack targets for a unit (ranged).
        public List<Unit> GetValidRangedTargets(Unit attacker)
        {
            var validTargets = new List<Unit>();

            if (attacker == null || !attacker.IsAlive)
            {
                return validTargets;
            }

            var cellsInRange = _grid.GetCellsInRange(attacker.Coordinates, attacker.Stats.attackRange);

            foreach (var cell in cellsInRange)
            {
                if (cell.Occupant != null &&
                    cell.Occupant.IsAlive &&
                    cell.Occupant.Role != attacker.Role &&
                    cell.Occupant != attacker)
                {
                    validTargets.Add(cell.Occupant);
                }
            }

            return validTargets;
        }

        // Create a MoveAction for a unit to a destination.
        public MoveAction CreateMoveAction(Unit actor, HexCoordinates destination)
        {
            return new MoveAction(actor, destination);
        }

        // Create a MeleeAttackAction for a unit against a target.
        public MeleeAttackAction CreateMeleeAttack(Unit actor, Unit target)
        {
            return new MeleeAttackAction(actor, target);
        }

        // Create a RangedAttackAction for a unit against a target.
        public RangedAttackAction CreateRangedAttack(Unit actor, Unit target)
        {
            return new RangedAttackAction(actor, target);
        }

        public GrappleAction CreateGrapple(Unit actor, Unit target)
        {
            return new GrappleAction(actor, target);
        }

        public RangedHealAction CreateRangedHeal(Unit actor, Unit target)
        {
            return new RangedHealAction(actor, target);
        }

        // Builds a 7-hex area ranged splash attack
        public ICombatAction CreateSplashAttack(Unit actor, HexCell targetCell)
        {
            var aoe = new List<HexCoordinates>();
            
            // A splash hits the center (dist 0) and immediate adjacent hexes (dist 1)
            foreach (var cell in _grid.GetAllCells())
            {
                if (_grid.GetDistance(targetCell.Coordinates, cell.Coordinates) <= 1)
                {
                    aoe.Add(cell.Coordinates);
                }
            }
            
            return new SplashAttackAction(actor, targetCell.Coordinates, aoe);
        } 

        // Builds a 3-hex frontal melee attack
        public ICombatAction CreateSweepAttack(Unit actor, HexCell targetCell)
        {
            var sweep = new List<HexCoordinates> { targetCell.Coordinates };
            
            // Any cell that is exactly distance 1 from BOTH the Actor AND the Target
            foreach (var cell in _grid.GetAllCells())
            {
                if (_grid.GetDistance(actor.Coordinates, cell.Coordinates) == 1 &&
                    _grid.GetDistance(targetCell.Coordinates, cell.Coordinates) == 1)
                {
                    sweep.Add(cell.Coordinates);
                }
            }
            
            return new SweepAttackAction(actor, targetCell.Coordinates, sweep);
        } 
    }
}
