using Game.Combat.Units;

namespace Game.Combat.Grid
{
    /// <summary>
    /// Represents a single cell in the hex grid.
    /// Tracks walkability, movement cost, and current occupant.
    /// </summary>
    public class HexCell
    {
        public HexCoordinates Coordinates { get; }
        public bool IsWalkable { get; set; }
        public int MovementCost { get; set; }
        public Unit Occupant { get; private set; }

        public bool IsOccupied => Occupant != null;

        public HexCell(HexCoordinates coordinates, bool isWalkable = true, int movementCost = 1)
        {
            Coordinates = coordinates;
            IsWalkable = isWalkable;
            MovementCost = movementCost;
            Occupant = null;
        }

        // Check if a unit can enter this cell.
        public bool CanEnter()
        {
            return IsWalkable && !IsOccupied;
        }

        // Place a unit in this cell.
        public bool SetOccupant(Unit unit)
        {
            if (IsOccupied && unit != null)
            {
                return false;
            }
            Occupant = unit;
            return true;
        }

        // Remove the current occupant.
        public void ClearOccupant()
        {
            Occupant = null;
        }
    }
}
