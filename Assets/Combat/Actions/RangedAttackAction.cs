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
        public int Damage => Actor.Stats.attackPower;

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
            var targetCell = grid.GetCell(TargetPos);
            if (targetCell == null) return false;

            int distance = grid.GetDistance(Actor.Coordinates, TargetPos);
            return distance >= 1 && distance <= Actor.Stats.attackRange;
        }

        public void Execute(HexGrid grid)
        {
            var targetUnit = grid.GetCell(TargetPos)?.Occupant;
            if (targetUnit == null || !targetUnit.IsAlive) return;

            const int BUMP_DAMAGE = 10;
            int totalDamage = Damage;

            HexCoordinates oldPos = targetUnit.Coordinates;
            HexCoordinates pushDest = oldPos.GetPushDestination(Actor.Coordinates); 
            var destCell = grid.GetCell(pushDest); 

            if (destCell == null || !destCell.IsWalkable)
            {
                totalDamage += BUMP_DAMAGE;
            }
            else if (destCell.IsOccupied)
            {
                totalDamage += BUMP_DAMAGE;
                destCell.Occupant.TakeDamage(BUMP_DAMAGE);
            }
            else
            {
                grid.MoveUnit(targetUnit, pushDest);
                
                // BROADCAST INTENT SHIFT
                HexCoordinates offset = new HexCoordinates(pushDest.q - oldPos.q, pushDest.r - oldPos.r);
                CombatManager.Instance.ShiftUnitIntent(targetUnit, offset);
            }

            targetUnit.TakeDamage(totalDamage);
        } 

        public void ApplyDisplacement(HexCoordinates offset)
        {
            TargetPos = new HexCoordinates(TargetPos.q + offset.q, TargetPos.r + offset.r);
        }
    }
}