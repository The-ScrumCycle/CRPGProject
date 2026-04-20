using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    public class SplashAttackAction : ICombatAction
    {
        public Unit Actor { get; }
        public HexCoordinates TargetCenter => _targetCenter;
        private HexCoordinates _targetCenter;
        private readonly HexGrid _grid;
        private readonly List<HexCoordinates> _aoeCells = new List<HexCoordinates>();

        public SplashAttackAction(Unit actor, HexCoordinates targetCenter, HexGrid grid)
        {
            Actor = actor;
            _targetCenter = targetCenter;
            _grid = grid;
            RecalculateAoE();
        }

        // Dynamically builds a perfect 7-hex shape around central aim point
        private void RecalculateAoE()
        {
            _aoeCells.Clear();
            if (_grid == null) return;

            foreach (var cell in _grid.GetAllCells())
            {
                if (_grid.GetDistance(_targetCenter, cell.Coordinates) <= 1)
                {
                    _aoeCells.Add(cell.Coordinates);
                }
            }
        }

        public IEnumerable<HexCoordinates> GetTargetCells() => _aoeCells;

        public bool IsValid(HexGrid grid)
        {
            if (Actor == null || !Actor.IsAlive) return false;
            int dist = grid.GetDistance(Actor.Coordinates, _targetCenter);
            return dist <= Actor.Stats.attackRange && dist > 0;
        }

        public void Execute(HexGrid grid)
        {
            foreach (var cell in grid.GetAllCells())
            {
                if (cell != null && cell.Occupant != null && cell.Occupant.IsAlive)
                {
                    if (_aoeCells.Contains(cell.Occupant.Coordinates))
                    {
                        cell.Occupant.TakeDamage(Actor.Stats.attackPower);
                    }
                }
            }
        }

        public void ApplyDisplacement(HexCoordinates offset)
        {
            // Shift the center, then let the grid draw the new shape
            _targetCenter = new HexCoordinates(_targetCenter.q + offset.q, _targetCenter.r + offset.r);
            RecalculateAoE();
        }
    }
}