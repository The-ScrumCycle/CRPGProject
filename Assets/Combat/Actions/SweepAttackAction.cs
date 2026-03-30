using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    public class SweepAttackAction : ICombatAction
    {
        public Unit Actor { get; }
        private readonly HexCoordinates _mainTarget;
        private readonly List<HexCoordinates> _sweepCells;

        public SweepAttackAction(Unit actor, HexCoordinates mainTarget, List<HexCoordinates> sweepCells)
        {
            Actor = actor;
            _mainTarget = mainTarget;
            _sweepCells = sweepCells;
        }

        public IEnumerable<HexCoordinates> GetTargetCells() => _sweepCells;

        public bool IsValid(HexGrid grid)
        {
            if (Actor == null || !Actor.IsAlive) return false;
            return grid.GetDistance(Actor.Coordinates, _mainTarget) == 1; // Must be adjacent
        }

        public void Execute(HexGrid grid)
        {
            foreach (var coords in _sweepCells)
            {
                var cell = grid.GetCell(coords);
                if (cell != null && cell.Occupant != null && cell.Occupant.IsAlive)
                {
                    cell.Occupant.TakeDamage(Actor.Stats.attackPower);
                }
            }
        }
    }
}
