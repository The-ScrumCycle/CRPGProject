using System.Collections.Generic;
using Game.Combat.Units;

namespace Game.Combat.Turn
{
    /// <summary>
    /// Manages turn order and progression.
    /// RESPONSIBILITY: Time control ONLY
    /// Does NOT execute actions or apply/manage the game rules.
    /// </summary>
    public class TurnSystem
    {
        private readonly List<Unit> _turnOrder;
        private int _currentIndex;
        private int _roundNumber;

        public Unit CurrentUnit => _turnOrder.Count > 0 ? _turnOrder[_currentIndex] : null;
        public int CurrentIndex => _currentIndex;
        public int RoundNumber => _roundNumber;
        public int UnitCount => _turnOrder.Count;
        public IReadOnlyList<Unit> TurnOrder => _turnOrder.AsReadOnly();

        public TurnSystem()
        {
            _turnOrder = new List<Unit>();
            _currentIndex = 0;
            _roundNumber = 1;
        }

        // Initialize turn order with units.
        // Player units go first, then enemies.
        public void Initialize(IEnumerable<Unit> units)
        {
            _turnOrder.Clear();

            var playerUnits = new List<Unit>();
            var enemyUnits = new List<Unit>();

            foreach (var unit in units)
            {
                if (unit.IsPlayerControlled)
                {
                    playerUnits.Add(unit);
                }
                else
                {
                    enemyUnits.Add(unit);
                }
            }

            _turnOrder.AddRange(playerUnits);
            _turnOrder.AddRange(enemyUnits);

            _currentIndex = 0;
            _roundNumber = 1;
        }

        // Get the current unit whose turn it is.
        public Unit GetCurrentUnit()
        {
            return CurrentUnit;
        }

        // Advance to the next unit's turn.
        public void AdvanceTurn()
        {
            if (_turnOrder.Count == 0) return;

            // Skip dead units
            int attempts = 0;
            do
            {
                _currentIndex++;
                if (_currentIndex >= _turnOrder.Count)
                {
                    _currentIndex = 0;
                    _roundNumber++;
                }
                attempts++;
            }
            while (!CurrentUnit.IsAlive && attempts < _turnOrder.Count);
        }

        // Check if the current round is complete.
        public bool IsRoundComplete()
        {
            return _currentIndex == 0 && _roundNumber > 1;
        }

        // Remove a unit from the turn order e.g when killed
        public void RemoveUnit(Unit unit)
        {
            int index = _turnOrder.IndexOf(unit);
            if (index < 0) return;

            _turnOrder.RemoveAt(index);

            // Adjust current index if necessary
            if (index < _currentIndex)
            {
                _currentIndex--;
            }
            else if (index == _currentIndex && _currentIndex >= _turnOrder.Count)
            {
                _currentIndex = 0;
            }
        }

        // Check if there are any player units alive.
        public bool HasAlivePlayerUnits()
        {
            foreach (var unit in _turnOrder)
            {
                if (unit.IsPlayerControlled && unit.IsAlive)
                {
                    return true;
                }
            }
            return false;
        }

        // Check if there are any enemy units alive.
        public bool HasAliveEnemyUnits()
        {
            foreach (var unit in _turnOrder)
            {
                if (!unit.IsPlayerControlled && unit.IsAlive)
                {
                    return true;
                }
            }
            return false;
        }

        // set the player's current unit through grid clicking/UI swaping
        public void SetCurrentUnit(Unit unit)
        {
            int index = _turnOrder.IndexOf(unit);
            if (index >= 0)
            {
                _currentIndex = index;
            }
        }
    }
}
