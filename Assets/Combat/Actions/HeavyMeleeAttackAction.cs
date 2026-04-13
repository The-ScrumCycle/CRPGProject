using System.Collections.Generic;
using Game.Combat.Grid;
using Game.Combat.Units;
using UnityEngine;

// Warrior class special attack that deals heavy damage and shoves enemies farther than normal attacks
namespace Game.Combat.Actions
{
    public class HeavyMeleeAttackAction : ICombatAction
    {
        public Unit Actor { get; }
        public HexCoordinates TargetPos { get; }
        
        private const int KNOCKBACK_DISTANCE = 3;
        private const int BUMP_DAMAGE = 10;

        public HeavyMeleeAttackAction(Unit actor, HexCoordinates targetPos)
        {
            Actor = actor;
            TargetPos = targetPos;
        }

        public bool IsValid(HexGrid grid)
        {
            var targetCell = grid.GetCell(TargetPos);
            
            if (targetCell == null || targetCell.Occupant == null || !targetCell.Occupant.IsAlive) 
                return false;
                
            // Heavy Melee requires standard adjacent melee range (distance 1)
            return grid.GetDistance(Actor.Coordinates, TargetPos) <= 1;
        }

        public void Execute(HexGrid grid)
        {
            var targetCell = grid.GetCell(TargetPos);
            var targetUnit = targetCell?.Occupant;

            if (targetUnit != null && targetUnit.IsAlive)
            {
                // 1. Initial hit
                targetUnit.TakeDamage(Actor.Stats.attackPower);

                // 2. Physics: Calculate the 3-hex shove
                ActionResolver resolver = CombatManager.Instance.GetActionResolver();
                HexCoordinates finalPos = resolver.ResolveLinearPush(targetUnit, Actor.Coordinates, KNOCKBACK_DISTANCE, out Unit bumpedUnit);

                // 3. Move the unit physically on the grid
                if (finalPos != targetUnit.Coordinates)
                {
                    grid.MoveUnit(targetUnit, finalPos);
                }

                // 4. Apply Bump Damage to BOTH units if a collision occurred!
                if (bumpedUnit != null && bumpedUnit.IsAlive)
                {
                    targetUnit.TakeDamage(BUMP_DAMAGE);
                    bumpedUnit.TakeDamage(BUMP_DAMAGE);
                    Debug.Log($"[Physics] {targetUnit.DisplayName} was shoved into {bumpedUnit.DisplayName} for {BUMP_DAMAGE} bump damage!");
                }
            }
        }

        public IEnumerable<HexCoordinates> GetTargetCells() => new List<HexCoordinates> { TargetPos };
    }
}
