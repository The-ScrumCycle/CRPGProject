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
        private const int BUMP_DAMAGE = 10;

        public SweepAttackAction(Unit actor, HexCoordinates mainTarget, HexGrid grid)
        {
            Actor = actor;
            _mainTarget = mainTarget;
            _grid = grid;
            RecalculateSweep();
        }

        private void RecalculateSweep()
        {
            _sweepCells.Clear();
            if (_grid == null) return;

            List<HexCoordinates> layer1 = new List<HexCoordinates>();

            // LAYER 1: The inner 3-hex arc
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
            foreach (var cell in _grid.GetAllCells())
            {
                if (_grid.GetDistance(Actor.Coordinates, cell.Coordinates) == 2)
                {
                    foreach (var innerCell in layer1)
                    {
                        if (_grid.GetDistance(innerCell, cell.Coordinates) == 1)
                        {
                            if (!_sweepCells.Contains(cell.Coordinates))
                                _sweepCells.Add(cell.Coordinates);
                            break; 
                        }
                    }
                }
            }
        }

        public IEnumerable<HexCoordinates> GetTargetCells() => _sweepCells;

        public bool IsValid(HexGrid grid)
        {
            if (Actor == null || !Actor.IsAlive) return false;
            return grid.GetDistance(Actor.Coordinates, _mainTarget) == 1; 
        }

        public void Execute(HexGrid grid)
        {
            ActionResolver resolver = CombatManager.Instance.GetActionResolver();

            foreach (var cell in grid.GetAllCells())
            {
                if (cell != null && cell.Occupant != null && cell.Occupant.IsAlive)
                {
                    if (_sweepCells.Contains(cell.Occupant.Coordinates))
                    {
                        var targetUnit = cell.Occupant;
                        
                        // 1. Initial Damage
                        targetUnit.TakeDamage(Actor.Stats.attackPower);

                        // 2. Physics: 1-hex push directly away from the attacker
                        HexCoordinates finalPos = resolver.ResolveLinearPush(targetUnit, Actor.Coordinates, 1, out Unit bumpedUnit);

                        if (finalPos != targetUnit.Coordinates)
                        {
                            grid.MoveUnit(targetUnit, finalPos);
                            
                            // Broadcast displacement so the shoved enemy's intents shift
                            HexCoordinates offset = new HexCoordinates(finalPos.q - targetUnit.Coordinates.q, finalPos.r - targetUnit.Coordinates.r);
                            CombatManager.Instance.ShiftUnitIntent(targetUnit, offset);
                        }

                        // 3. Collision Damage
                        if (bumpedUnit != null && bumpedUnit.IsAlive)
                        {
                            targetUnit.TakeDamage(BUMP_DAMAGE);
                            bumpedUnit.TakeDamage(BUMP_DAMAGE);
                        }
                    }
                }
            }
        } 

        public void ApplyDisplacement(HexCoordinates offset)
        {
            _mainTarget = new HexCoordinates(_mainTarget.q + offset.q, _mainTarget.r + offset.r);
            RecalculateSweep();
        }
    }
}