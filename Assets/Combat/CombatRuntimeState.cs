using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Actions;

namespace Game.Combat
{
    ///<summary>
    /// Class which represents the pure domain state of combat during runtime.
    /// It owns the CombatState, truth of player playing his turn, the Units, enemy AI turn Intent, unit visual layer.
    /// Simply holds data mutation helpers to keep track of combat cleanly.
    ///<summary>
    public class CombatRuntimeState
    {
        public List<Unit> AllUnits { get; } = new List<Unit>();
        public Dictionary<Unit, UnitVisual> UnitVisuals { get; } = new Dictionary<Unit, UnitVisual>();
        public List<ActionIntent> EnemyIntents { get; } = new List<ActionIntent>();
        public CombatState CurrentState { get; set; } = CombatState.Initializing;
        public HashSet<Unit> ActedUnits { get; } = new HashSet<Unit>();

        public void ClearActedUnits()
        {
            ActedUnits.Clear();
        }

        public void RegisterUnit(Unit unit, UnitVisual visual)
        {
            AllUnits.Add(unit);
            UnitVisuals[unit] = visual;
        }

        public void RemoveUnitVisual(Unit unit)
        {
            UnitVisuals.Remove(unit);
        }

        public UnitVisual GetVisual(Unit unit)
        {
            UnitVisuals.TryGetValue(unit, out var visual);
            return visual;
        }

        public void ClearIntents()
        {
            EnemyIntents.Clear();
        }

        public void AddIntent(ActionIntent intent)
        {
            EnemyIntents.Add(intent);
        }

        public IReadOnlyList<ActionIntent> GetIntents()
        {
            return EnemyIntents.AsReadOnly();
        }
    }
}
