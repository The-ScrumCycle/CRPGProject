using UnityEngine;
using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Action for moving a unit to a new hex cell.
    /// </summary>
    public class MoveAction : ICombatAction
    {
        public Unit Actor { get; }
        public HexCoordinates Destination { get; }
        public List<HexCoordinates> Path { get; private set; }

        public MoveAction(Unit actor, HexCoordinates destination)
        {
            Actor = actor;
            Destination = destination;
            Path = new List<HexCoordinates>();
        }

        public IEnumerable<HexCoordinates> GetTargetCells()
        {
            yield return Destination;
        }

        public bool IsValid(HexGrid grid)
        {
            // Can't move if destination is same as current
            if (Actor.Coordinates == Destination)
            {
                return false;
            }

            // Check if destination is in range
            int distance = grid.GetDistance(Actor.Coordinates, Destination);
            if (distance > Actor.Stats.movementRange)
            {
                Debug.Log("out of range");
                return false;
            }

            // Check if destination is walkable and unoccupied
            var destCell = grid.GetCell(Destination);
            if (destCell == null || !destCell.CanEnter())
            {
                return false;
            }

            // Find valid path
            Path = grid.FindPath(Actor.Coordinates, Destination, Actor.Stats.movementRange);
            return Path.Count > 0;
        }

        public void Execute(HexGrid grid)
        {
            grid.MoveUnit(Actor, Destination);
        }
    }
}
