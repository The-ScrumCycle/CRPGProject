using System.Collections.Generic;
using Game.Combat.Grid;
using Game.Combat.Units;
using UnityEngine;

namespace Game.Combat.Actions
{
    public class PullAction : ICombatAction
    {
        public Unit Actor { get; }
        public HexCoordinates TargetPos { get; }
        
        private const int PULL_DISTANCE = 8;
        private const int BUMP_DAMAGE = 10;

        public PullAction(Unit actor, HexCoordinates targetPos)
        {
            Actor = actor;
            TargetPos = targetPos;
        }

        public bool IsValid(HexGrid grid)
        {
            var targetCell = grid.GetCell(TargetPos);
            
            if (targetCell == null || targetCell.Occupant == null || !targetCell.Occupant.IsAlive) 
                return false;
                
            // Validate that the target is within the allowed pull range
            return grid.GetDistance(Actor.Coordinates, TargetPos) <= PULL_DISTANCE;
        }

        public void Execute(HexGrid grid, UnitVisual visual)
        {
            var targetCell = grid.GetCell(TargetPos);
            var targetUnit = targetCell?.Occupant;

            if (targetUnit != null && targetUnit.IsAlive)
            {
                // 1. Physics: Calculate a multi-hex pull towards the caster
                ActionResolver resolver = CombatManager.Instance.GetActionResolver();
                HexCoordinates finalPos = resolver.ResolveLinearPull(targetUnit, Actor.Coordinates, PULL_DISTANCE, out Unit bumpedUnit);

                // 2. Move the unit physically on the grid
                if (finalPos != targetUnit.Coordinates)
                {
                    grid.MoveUnit(targetUnit, finalPos);
                }

                // 3. UNIVERSAL RULE: Apply Bump Damage if a collision occurred
                if (bumpedUnit != null && bumpedUnit.IsAlive)
                {
                    targetUnit.TakeDamage(BUMP_DAMAGE);
                    bumpedUnit.TakeDamage(BUMP_DAMAGE);
                    Debug.Log($"[Physics] {targetUnit.DisplayName} collided with {bumpedUnit.DisplayName} during pull for {BUMP_DAMAGE} damage!");
                }
            }
        }

        public IEnumerable<HexCoordinates> GetTargetCells() => new List<HexCoordinates> { TargetPos };
    }
}