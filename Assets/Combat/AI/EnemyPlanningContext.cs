using System.Collections.Generic;
using Game.Combat.Grid;
using Game.Combat.Units;

namespace Game.Combat.AI
{
    /// <summary>
    /// Lightweight shared planning state used while enemies choose intents for the round.
    /// Keeps only the planning state needed for simple coordination:
    /// focus fire, distinct move destinations, and frontline protection.
    /// </summary>
    public class EnemyPlanningContext
    {
        private readonly Dictionary<Unit, int> _plannedDamageByPlayer = new Dictionary<Unit, int>();
        private readonly Dictionary<Unit, HexCoordinates> _plannedPositionsByUnit = new Dictionary<Unit, HexCoordinates>();
        private readonly HashSet<HexCoordinates> _reservedMoveDestinations = new HashSet<HexCoordinates>();
        private readonly HashSet<HexCoordinates> _reservedAttackCells = new HashSet<HexCoordinates>();
        private readonly HashSet<Unit> _frontlineAllies = new HashSet<Unit>();

        public int GetPlannedDamage(Unit unit)
        {
            return unit != null && _plannedDamageByPlayer.TryGetValue(unit, out int damage) ? damage : 0;
        }

        public void AddPlannedDamage(Unit unit, int damage)
        {
            if (unit == null || damage <= 0)
            {
                return;
            }

            _plannedDamageByPlayer[unit] = GetPlannedDamage(unit) + damage;
        }

        public bool IsMoveDestinationReserved(HexCoordinates destination)
        {
            return _reservedMoveDestinations.Contains(destination);
        }

        public void ReserveMoveDestination(HexCoordinates destination)
        {
            _reservedMoveDestinations.Add(destination);
        }

        public void SetPlannedPosition(Unit unit, HexCoordinates destination)
        {
            if (unit != null)
            {
                _plannedPositionsByUnit[unit] = destination;
            }
        }

        public HexCoordinates GetProjectedPosition(Unit unit)
        {
            if (unit != null && _plannedPositionsByUnit.TryGetValue(unit, out HexCoordinates destination))
            {
                return destination;
            }

            return unit != null ? unit.Coordinates : HexCoordinates.Invalid;
        }

        public bool IsAttackCellReserved(HexCoordinates cell)
        {
            return _reservedAttackCells.Contains(cell);
        }

        public void ReserveAttackCell(HexCoordinates cell)
        {
            _reservedAttackCells.Add(cell);
        }

        public void MarkFrontlineAlly(Unit unit)
        {
            if (unit != null)
            {
                _frontlineAllies.Add(unit);
            }
        }

        public bool IsFrontlineAlly(Unit unit)
        {
            return unit != null && _frontlineAllies.Contains(unit);
        }
    }
}
