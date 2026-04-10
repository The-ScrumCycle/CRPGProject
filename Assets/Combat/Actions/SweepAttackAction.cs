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

        public void Execute(HexGrid grid, UnitVisual visual)
        {
            foreach (var cell in grid.GetAllCells())
            {
                if (cell != null && cell.Occupant != null && cell.Occupant.IsAlive)
                {
                    foreach (var blastCoord in _sweepCells)
                    {
                        if (grid.GetDistance(blastCoord, cell.Occupant.Coordinates) == 0)
                        {
                            cell.Occupant.TakeDamage(Actor.Stats.attackPower);
                            break;
                        }
                    }
                }
            }
        } 
    }
}
