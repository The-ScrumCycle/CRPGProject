using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;
using UnityEngine;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Action for grappling an adjacent unit
    /// If valid, grappled unit is unable to move
    /// </summary>
    public class GrappleAction : ICombatAction
    {
        public Unit Actor { get; }
        public HexCoordinates TargetPos { get; private set; }

        public GrappleAction(Unit actor, HexCoordinates targetPos)
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
            return distance >= 1 && distance <= 4; // Example grapple range
        }

        public void Execute(HexGrid grid)
        {
            var targetUnit = grid.GetCell(TargetPos)?.Occupant;
            if (targetUnit != null && targetUnit.IsAlive)
            {
                // Implement grapple pull/stun logic here
                targetUnit.TakeDamage(Actor.Stats.attackPower);
                Debug.Log($"[Grapple] {Actor.DisplayName} grappled {targetUnit.DisplayName}!");
            }
        }

        public void ApplyDisplacement(HexCoordinates offset)
        {
            TargetPos = new HexCoordinates(TargetPos.q + offset.q, TargetPos.r + offset.r);
        }
    }
}