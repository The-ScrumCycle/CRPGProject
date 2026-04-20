using Game.Combat.Units;
using Game.Combat.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Action for healing a unit at range
    /// If valid, heals exactly by healAmount
    /// </summary>
    public class RangedHealAction : ICombatAction
    {
        public Unit Actor { get; }
        public HexCoordinates TargetPos { get; private set; }
        public int healAmount => Actor.Stats.healPower;

        public RangedHealAction(Unit actor, HexCoordinates targetPos)
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

            int before = targetUnit.Stats.currentHealth;
            targetUnit.Heal(healAmount);
            int after = targetUnit.Stats.currentHealth;
            Debug.Log($"[Heal Execute] {Actor.DisplayName} healed {targetUnit.DisplayName} for {healAmount}. HP: {before} -> {after}");
        }

        public void ApplyDisplacement(HexCoordinates offset)
        {
            TargetPos = new HexCoordinates(TargetPos.q + offset.q, TargetPos.r + offset.r);
        }
    }
} 