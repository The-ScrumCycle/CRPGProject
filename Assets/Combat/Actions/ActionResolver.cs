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

            ActionVisualType visualType = ActionVisualType.None;
            Unit targetUnit = null;
            int predictedDamage = 0;
            List<HexCoordinates> targetCells = new List<HexCoordinates>();
            HexCoordinates? pushDestination = null;
            Unit secondaryBumpTarget = null;
            bool targetTakesBumpDamage = false;

            if (action is MoveAction move)
            {
                visualType = ActionVisualType.Move;
                targetCells.Add(move.Destination);
            }
            else if (action is MeleeAttackAction melee)
            {
                visualType = ActionVisualType.MeleeAttack;
                targetUnit = _grid.GetCell(melee.TargetPos)?.Occupant;
                predictedDamage = action.Actor.Stats.attackPower;

                if (targetUnit != null)
                {
                    pushDestination = ResolveLinearPush(targetUnit, action.Actor.Coordinates, 1, out secondaryBumpTarget);
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        predictedDamage += 10;
                        targetTakesBumpDamage = true;
                    }
                }
            }
            else if (action is RangedAttackAction ranged)
            {
                visualType = ActionVisualType.RangedAttack;
                targetUnit = _grid.GetCell(ranged.TargetPos)?.Occupant;
                predictedDamage = action.Actor.Stats.attackPower;

                if (targetUnit != null)
                {
                    pushDestination = ResolveLinearPush(targetUnit, action.Actor.Coordinates, 1, out secondaryBumpTarget);
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        predictedDamage += 10;
                        targetTakesBumpDamage = true;
                    }
                }
            }
            else if (action is HeavyMeleeAttackAction heavy)
            {
                visualType = ActionVisualType.MeleeAttack; 
                targetUnit = _grid.GetCell(heavy.TargetPos)?.Occupant;
                predictedDamage = action.Actor.Stats.attackPower;

                if (targetUnit != null)
                {
                    pushDestination = ResolveLinearPush(targetUnit, action.Actor.Coordinates, 3, out secondaryBumpTarget);
                    
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        predictedDamage += 10;
                        targetTakesBumpDamage = true;
                    }
                }
            }
            else if (action is PullAction pull)
            {
                visualType = ActionVisualType.Pull;
                targetUnit = _grid.GetCell(pull.TargetPos)?.Occupant;

                if (targetUnit != null)
                {
                    pushDestination = ResolveLinearPull(targetUnit, action.Actor.Coordinates, 3, out secondaryBumpTarget);
                    
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        predictedDamage += 10;
                        targetTakesBumpDamage = true;
                    }
                }
            }
            else if (action is SplashAttackAction splash)
            {
                visualType = ActionVisualType.RangedAttack;
                targetCells = new List<HexCoordinates>(splash.GetTargetCells());
                predictedDamage = action.Actor.Stats.attackPower;

                foreach (var cellCoord in targetCells)
                {
                    var cell = _grid.GetCell(cellCoord);
                    if (cell?.Occupant != null && cell.Occupant != action.Actor)
                    {
                        targetUnit = cell.Occupant;
                        break;
                    }
                }

                if (targetUnit != null)
                {
                    pushDestination = ResolveLinearPush(targetUnit, action.Actor.Coordinates, 1, out secondaryBumpTarget);
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        targetTakesBumpDamage = true;
                    }
                }
            }
            else if (action is SweepAttackAction sweep)
            {
                visualType = ActionVisualType.MeleeAttack;
                targetCells = new List<HexCoordinates>(sweep.GetTargetCells());
                predictedDamage = action.Actor.Stats.attackPower;

                foreach (var cellCoord in targetCells)
                {
                    var cell = _grid.GetCell(cellCoord);
                    if (cell?.Occupant != null && cell.Occupant != action.Actor)
                    {
                        targetUnit = cell.Occupant;
                        break; 
                    }
                }

                if (targetUnit != null)
                {
                    pushDestination = ResolveLinearPush(targetUnit, action.Actor.Coordinates, 1, out secondaryBumpTarget);
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        targetTakesBumpDamage = true;
                    }
                }
            }
            else if (action is GrappleAction grapple)
            {
                visualType = ActionVisualType.Grapple;
                targetUnit = _grid.GetCell(grapple.TargetPos)?.Occupant;
                predictedDamage = action.Actor.Stats.attackPower;
            }
            else if (action is RangedHealAction heal)
            {
                visualType = ActionVisualType.Heal;
                targetUnit = _grid.GetCell(heal.TargetPos)?.Occupant;
                predictedDamage = -heal.healAmount; 
            }

            if (targetCells.Count == 0 && action.GetTargetCells() != null)
            {
                targetCells.AddRange(action.GetTargetCells());
            }

            return new ActionIntent(
                action.Actor, 
                action, 
                targetUnit, 
                predictedDamage, 
                visualType, 
                null, 
                true, 
                pushDestination, 
                targetTakesBumpDamage, 
                secondaryBumpTarget
            );
        } 

        // Get all valid move destinations for a unit.
        public List<HexCoordinates> GetValidMoveDestinations(Unit unit)
        {
            var reachable = _grid.GetReachableCells(unit.Coordinates, unit.Stats.movementRange);
            var validDestinations = new List<HexCoordinates>();

            var claimedHexes = new HashSet<HexCoordinates>();
            if (CombatManager.Instance != null)
            {
                foreach (var intent in CombatManager.Instance.GetEnemyIntents())
                {
                    if (intent.Action is MoveAction move && intent.Actor != unit)
                    {
                        claimedHexes.Add(move.Destination);
                    }
                }
            }

            foreach (var cell in reachable)
            {
                if (!claimedHexes.Contains(cell.Coordinates))
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

        public ActionIntent GeneratePullIntent(Unit actor, HexCell targetCell)
        {
            var targetUnit = targetCell.Occupant;
            var pullAction = new PullAction(actor, targetCell.Coordinates);

            if (targetUnit != null)
            {
                // Run the physics simulation purely to predict the collision for the UI
                ResolveLinearPull(targetUnit, actor.Coordinates, 3, out Unit predictedBumpTarget);

                int predictedDamage = predictedBumpTarget != null ? 10 : 0; // Calculate bump damage for the preview
                bool takesBumpDamage = predictedBumpTarget != null;

                return new ActionIntent(
                    actor, 
                    pullAction, 
                    targetUnit, 
                    predictedDamage, 
                    ActionVisualType.Pull, 
                    null, // movementPath
                    true, // isValid
                    null, // pushDestination
                    takesBumpDamage, 
                    predictedBumpTarget // Pass the read-only property through the constructor
                );
            }

            // Fallback for hovering over an empty cell
            return new ActionIntent(
                actor, pullAction, null, 0, ActionVisualType.Pull, null, false
            );
        } 

        public ICombatAction CreateMoveAction(Unit actor, HexCoordinates destination) => new MoveAction(actor, destination);
        public ICombatAction CreateSweepAttack(Unit actor, HexCell targetCell) => new SweepAttackAction(actor, targetCell.Coordinates, _grid); 
        public ICombatAction CreateSplashAttack(Unit actor, HexCell targetCell) => new SplashAttackAction(actor, targetCell.Coordinates, _grid);
        public ICombatAction CreateHeavyMeleeAttack(Unit actor, HexCell targetCell) => new HeavyMeleeAttackAction(actor, targetCell.Coordinates);
        public ICombatAction CreatePull(Unit actor, HexCell targetCell) => new PullAction(actor, targetCell.Coordinates);
        
        public ICombatAction CreateMeleeAttack(Unit actor, HexCell targetCell) => new MeleeAttackAction(actor, targetCell.Coordinates);
        public ICombatAction CreateRangedAttack(Unit actor, HexCell targetCell) => new RangedAttackAction(actor, targetCell.Coordinates);
        public ICombatAction CreateRangedHeal(Unit actor, HexCell targetCell) => new RangedHealAction(actor, targetCell.Coordinates);
        public ICombatAction CreateGrappleAction(Unit actor, HexCell targetCell) => new GrappleAction(actor, targetCell.Coordinates);

        // Calculates a multi-hex shove. Returns the final valid hex and outputs any unit that was collided with so we can consider bump damage.
        public HexCoordinates ResolveLinearPush(Unit targetUnit, HexCoordinates attackerPos, int pushDistance, out Unit bumpedUnit)
        {
            bumpedUnit = null;
            HexCoordinates currentPos = targetUnit.Coordinates;
            HexCoordinates prevPos = attackerPos;

            for (int i = 0; i < pushDistance; i++)
            {
                HexCoordinates nextPos = currentPos.GetPushDestination(prevPos);
                HexCell nextCell = _grid.GetCell(nextPos);

                if (nextCell == null || !nextCell.CanEnter()) 
                    break;

                if (nextCell.Occupant != null && nextCell.Occupant != targetUnit)
                {
                    bumpedUnit = nextCell.Occupant;
                    break;
                }

                prevPos = currentPos;
                currentPos = nextPos;
            }

            return currentPos;
        }

        // Calculates a multi-hex pull. Moves the target towards the caster, respecting collision rules for the game (e.g bump stops and causes damage)
        public HexCoordinates ResolveLinearPull(Unit targetUnit, HexCoordinates pullerPos, int pullDistance, out Unit bumpedUnit)
        {
            bumpedUnit = null;
            HexCoordinates currentPos = targetUnit.Coordinates;

            for (int i = 0; i < pullDistance; i++)
            {
                HexCoordinates bestNeighbor = currentPos;
                int minDistance = HexCoordinates.Distance(currentPos, pullerPos);

                foreach (var offset in HexCoordinates.GetNeighborOffsets(currentPos))
                {
                    HexCoordinates neighbor = currentPos + offset;
                    int dist = HexCoordinates.Distance(neighbor, pullerPos);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestNeighbor = neighbor;
                    }
                }

                if (bestNeighbor == currentPos || minDistance == 0) break;

                HexCell nextCell = _grid.GetCell(bestNeighbor);

                if (nextCell == null || !nextCell.CanEnter()) break;
                if (nextCell.Occupant != null && nextCell.Occupant != targetUnit)
                {
                    bumpedUnit = nextCell.Occupant;
                    break;
                }

                currentPos = bestNeighbor;
            }

            return currentPos;
        }
    }
}
