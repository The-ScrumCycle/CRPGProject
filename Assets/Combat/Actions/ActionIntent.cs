using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Represents the enemy ai's planned action before execution.
    /// Used for previewing enemy actions (our main reference is Into the Breach)
    /// </summary>
    public class ActionIntent
    {
        public Unit Actor { get; }
        public ICombatAction Action { get; }
        public IReadOnlyList<HexCoordinates> TargetCells { get; private set; }
        public int PredictedDamage { get; }
        public Unit TargetUnit { get; private set; }
        public ActionVisualType VisualType { get; }
        public List<HexCoordinates> MovementPath { get; }
        public bool IsValid { get; }
        public HexCoordinates? PushDestination { get; }
        public bool TargetTakesBumpDamage { get; }
        public Unit SecondaryBumpTarget { get; }

        public ActionIntent(
        Unit actor,
        ICombatAction action,
        Unit targetUnit,
        int predictedDamage,
        ActionVisualType visualType,
        List<HexCoordinates> movementPath,
        bool isValid,
        HexCoordinates? pushDestination = null,
        bool targetTakesBumpDamage = false,
        Unit secondaryBumpTarget = null)
        {
            Actor = actor;
            Action = action;
            var rawCells = action.GetTargetCells();
            TargetCells = rawCells != null ? new List<HexCoordinates>(rawCells) : new List<HexCoordinates>();
            TargetUnit = targetUnit;
            PredictedDamage = predictedDamage;
            VisualType = visualType;
            MovementPath = movementPath ?? new List<HexCoordinates>();
            IsValid = isValid;
            
            PushDestination = pushDestination;
            TargetTakesBumpDamage = targetTakesBumpDamage;
            SecondaryBumpTarget = secondaryBumpTarget;
        } 

        // Function to calculate intent's offset i.e "Aim" if enemy AI Unit is shoved around
        public void Shift(HexCoordinates offset, HexGrid grid)
        {
            // 1. Shift the underlying action's mathematical targeting
            Action.ApplyDisplacement(offset);

            // 2. Re-evaluate the target cells for the UI red danger zones
            TargetCells = new List<HexCoordinates>(Action.GetTargetCells());

            // 3. Update the primary target unit if someone new is standing in the target zone
            TargetUnit = null;
            if (TargetCells.Count > 0)
            {
                TargetUnit = grid.GetCell(TargetCells[0])?.Occupant;
            }
        }

        public override string ToString()
        {
            string target = TargetUnit != null ? $" -> {TargetUnit.DisplayName}" : "";
            string damage = PredictedDamage > 0 ? $" ({PredictedDamage} dmg)" : "";
            return $"{Actor.DisplayName}: {Action.GetType().Name}{target}{damage}";
        }
    }
}
