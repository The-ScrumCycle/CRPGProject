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

        public ActionIntent(Unit actor, ICombatAction action, Unit targetUnit = null, int predictedDamage = 0)
        {
            Actor = actor;
            Action = action;
            TargetCells = new List<HexCoordinates>(action.GetTargetCells());
            TargetUnit = targetUnit;
            PredictedDamage = predictedDamage;
        }

        public override string ToString()
        {
            string target = TargetUnit != null ? $" -> {TargetUnit.DisplayName}" : "";
            string damage = PredictedDamage > 0 ? $" ({PredictedDamage} dmg)" : "";
            return $"{Actor.DisplayName}: {Action.GetType().Name}{target}{damage}";
        }
    }
}
