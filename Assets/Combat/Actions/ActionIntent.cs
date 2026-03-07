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
        public List<HexCoordinates> TargetCells { get; }
        public int PredictedDamage { get; }
        public Unit TargetUnit { get; }
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
            TargetCells = new List<HexCoordinates>(action.GetTargetCells());
            TargetUnit = targetUnit;
            PredictedDamage = predictedDamage;
            VisualType = visualType;
            MovementPath = movementPath ?? new List<HexCoordinates>();
            IsValid = isValid;
            
            PushDestination = pushDestination;
            TargetTakesBumpDamage = targetTakesBumpDamage;
            SecondaryBumpTarget = secondaryBumpTarget;
        } 

        public override string ToString()
        {
            string target = TargetUnit != null ? $" -> {TargetUnit.DisplayName}" : "";
            string damage = PredictedDamage > 0 ? $" ({PredictedDamage} dmg)" : "";
            return $"{Actor.DisplayName}: {Action.GetType().Name}{target}{damage}";
        }
    }
}
