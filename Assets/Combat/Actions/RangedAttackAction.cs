using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Action for ranged attacking a unit within attack range.
    /// If valid, attack hits and deals exact damage.
    /// </summary>
    public class RangedAttackAction : ICombatAction
    {
        public Unit Actor { get; }
        public HexCoordinates TargetPos { get; private set; } 

        public RangedAttackAction(Unit actor, HexCoordinates targetPos)
        {
            Actor = actor;
            TargetPos = targetPos;
        }

        public IEnumerable<HexCoordinates> GetTargetCells()
        {
            yield return TargetPos;
        }

        public bool IsValid(HexGrid grid)
        {
            if (Actor == null || !Actor.IsAlive) return false;
            
            int dist = grid.GetDistance(Actor.Coordinates, TargetPos);
            
            if (dist < 1 || dist > Actor.Stats.attackRange) return false;

            var targetCell = grid.GetCell(TargetPos);
            if (targetCell == null || targetCell.Occupant == null || !targetCell.Occupant.IsAlive) return false;

            return true;
        }

        public void Execute(HexGrid grid)
        {
            var targetCell = grid.GetCell(TargetPos);
            if (targetCell != null && targetCell.Occupant != null)
            {
                Unit targetUnit = targetCell.Occupant;
                targetUnit.TakeDamage(Actor.Stats.attackPower);

                // --- 1-HEX KNOCKBACK PHYSICS ---
                HexCoordinates pushDest = TargetPos;
                int maxDist = grid.GetDistance(Actor.Coordinates, TargetPos);
                
                // Check all 6 standard hex directions to find the one moving directly away
                HexCoordinates[] directions = new HexCoordinates[] {
                    new HexCoordinates(1, 0), new HexCoordinates(1, -1), new HexCoordinates(0, -1),
                    new HexCoordinates(-1, 0), new HexCoordinates(-1, 1), new HexCoordinates(0, 1)
                };

                foreach (var dir in directions)
                {
                    HexCoordinates neighbor = TargetPos + dir;
                    int dist = grid.GetDistance(Actor.Coordinates, neighbor);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        pushDest = neighbor;
                    }
                }

                // Apply the Shove
                if (pushDest != TargetPos)
                {
                    var destCell = grid.GetCell(pushDest);
                    if (destCell != null)
                    {
                        if (destCell.CanEnter())
                        {
                            HexCoordinates offset = pushDest - TargetPos;
                            grid.MoveUnit(targetUnit, pushDest);
                            
                            // Shift their telegraphed attack aim if they were charging one
                            if (CombatManager.Instance != null)
                            {
                                CombatManager.Instance.ShiftUnitIntent(targetUnit, offset);
                            }
                        }
                        else if (destCell.Occupant != null && destCell.Occupant != targetUnit)
                        {
                            // Bump Damage
                            targetUnit.TakeDamage(10);
                            destCell.Occupant.TakeDamage(10);
                        }
                    }
                }
            }
        }

        public void ApplyDisplacement(HexCoordinates offset)
        {
            TargetPos = new HexCoordinates(TargetPos.q + offset.q, TargetPos.r + offset.r);
        }
    }
} 