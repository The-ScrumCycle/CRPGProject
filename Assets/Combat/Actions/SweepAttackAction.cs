using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    public class SweepAttackAction : ICombatAction
    {
        public Unit Actor { get; }
        private HexCoordinates _mainTarget;
        private readonly HexGrid _grid;
        private readonly List<HexCoordinates> _sweepCells = new List<HexCoordinates>();

        public SweepAttackAction(Unit actor, HexCoordinates mainTarget, HexGrid grid)
        {
            Actor = actor;
            _mainTarget = mainTarget;
            _grid = grid;
            RecalculateSweep();
        }

        // Dynamically rebuilds an 8-hex (2-depth) frontal cone!
        private void RecalculateSweep()
        {
            _sweepCells.Clear();
            if (_grid == null) return;

            List<HexCoordinates> layer1 = new List<HexCoordinates>();

            // LAYER 1: The inner 3-hex arc
            // Any cell that is exactly distance 1 from the Actor AND distance <= 1 from the Target
            foreach (var cell in _grid.GetAllCells())
            {
                if (_grid.GetDistance(Actor.Coordinates, cell.Coordinates) == 1 &&
                    _grid.GetDistance(_mainTarget, cell.Coordinates) <= 1) 
                {
                    layer1.Add(cell.Coordinates);
                    _sweepCells.Add(cell.Coordinates);
                }
            }

            // LAYER 2: The outer 5-hex arc
            // Any cell that is distance 2 from the Actor, but touches the inner arc
            foreach (var cell in _grid.GetAllCells())
            {
                if (_grid.GetDistance(Actor.Coordinates, cell.Coordinates) == 2)
                {
                    foreach (var innerCell in layer1)
                    {
                        if (_grid.GetDistance(innerCell, cell.Coordinates) == 1)
                        {
                            if (!_sweepCells.Contains(cell.Coordinates))
                            {
                                _sweepCells.Add(cell.Coordinates);
                            }
                            break; // Move to the next grid cell once added to avoid duplicates
                        }
                    }
                }
            }
        }

        public IEnumerable<HexCoordinates> GetTargetCells() => _sweepCells;

        public bool IsValid(HexGrid grid)
        {
            if (Actor == null || !Actor.IsAlive) return false;
            return grid.GetDistance(Actor.Coordinates, _mainTarget) == 1; // Anchor must be adjacent
        }

        public void Execute(HexGrid grid)
        {
            foreach (var cell in grid.GetAllCells())
            {
                if (cell != null && cell.Occupant != null && cell.Occupant.IsAlive)
                {
                    foreach (var sweepCoord in _sweepCells)
                    {
                        if (grid.GetDistance(sweepCoord, cell.Occupant.Coordinates) == 0)
                        {
                            cell.Occupant.TakeDamage(Actor.Stats.attackPower);
                            break;
                        }
                    }
                }
            }
        } 

        public void ApplyDisplacement(HexCoordinates offset)
        {
            // Shift the center, then dynamically redraw the 8-hex cone
            _mainTarget = new HexCoordinates(_mainTarget.q + offset.q, _mainTarget.r + offset.r);
            RecalculateSweep();
        }
    }
}