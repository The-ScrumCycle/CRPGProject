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
            else if (action is HeavyMeleeAttackAction heavy)
            {
                visualType = ActionVisualType.MeleeAttack; 
                targetUnit = _grid.GetCell(heavy.TargetPos)?.Occupant;
                predictedDamage = action.Actor.Stats.attackPower;

                if (targetUnit != null)
                {
                    // Predict the 3-hex shove
                    pushDestination = ResolveLinearPush(targetUnit, action.Actor.Coordinates, 3, out secondaryBumpTarget);
                    
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        predictedDamage += BUMP_DAMAGE;
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
                    // Predict the 3-hex pull
                    pushDestination = ResolveLinearPull(targetUnit, action.Actor.Coordinates, 3, out secondaryBumpTarget);
                    
                    if (secondaryBumpTarget != null || !_grid.GetCell(pushDestination.Value).IsWalkable)
                    {
                        predictedDamage += BUMP_DAMAGE;
                        targetTakesBumpDamage = true;
                    }
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
            var reachable = _grid.GetReachableCells(unit.Coordinates, unit.Stats.movementRange);
            var validDestinations = new List<HexCoordinates>();

            // Look at the intents already locked in for this round. 
            // Treat their destinations as occupied cells to avoid AIs wanting to move to same hex.
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

        public ICombatAction CreateHeavyMeleeAttack(Unit actor, HexCell targetCell)
        {
            return new HeavyMeleeAttackAction(actor, targetCell.Coordinates);
        }

        public ICombatAction CreatePull(Unit actor, HexCell targetCell)
        {
            return new PullAction(actor, targetCell.Coordinates);
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

                // Collision: Hit a wall or out of bounds
                if (nextCell == null || !nextCell.CanEnter()) 
                    break;

                // Collision: Hit another unit
                if (nextCell.Occupant != null && nextCell.Occupant != targetUnit)
                {
                    bumpedUnit = nextCell.Occupant;
                    break;
                }

                // Space is clear, advance the trajectory
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
                // Find the neighbor of currentPos that is mathematically closest to the puller
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

                // If we are already adjacent to the puller, stop pulling
                if (bestNeighbor == currentPos || minDistance == 0) break;

                HexCell nextCell = _grid.GetCell(bestNeighbor);

                // Collision checks
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
