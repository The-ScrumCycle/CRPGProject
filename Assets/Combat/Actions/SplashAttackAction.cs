using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    public class SplashAttackAction : ICombatAction
    {
        public Unit Actor { get; }
        private readonly HexCoordinates _targetCenter;
        private readonly List<HexCoordinates> _aoeCells;

        public SplashAttackAction(Unit actor, HexCoordinates targetCenter, List<HexCoordinates> aoeCells)
        {
            Actor = actor;
            _targetCenter = targetCenter;
            _aoeCells = aoeCells;
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
            // Bypass dictionary hash lookups and evaluate pure distance
            foreach (var cell in grid.GetAllCells())
            {
                if (cell != null && cell.Occupant != null && cell.Occupant.IsAlive)
                {
                    foreach (var blastCoord in _aoeCells)
                    {
                        if (grid.GetDistance(blastCoord, cell.Occupant.Coordinates) == 0)
                        {
                            cell.Occupant.TakeDamage(Actor.Stats.attackPower);
                            break; // Prevent double-damage if coordinates overlap
                        }
                    }
                }
            }
        } 
    }
}
