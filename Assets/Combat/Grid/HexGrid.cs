using System.Collections.Generic;
using UnityEngine;
using Game.Combat.Units;

namespace Game.Combat.Grid
{
    /// <summary>
    /// Logical hex grid managing cells, basic grid to grid pathfinding, and spatial queries inside the hex grid
    /// Does NOT handle rendering for this see HexGridRenderer.
    /// </summary>
    public class HexGrid
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Dictionary<HexCoordinates, HexCell> _cells;

        public int Width => _width;
        public int Height => _height;

        public HexGrid(int width, int height)
        {
            _width = width;
            _height = height;
            _cells = new Dictionary<HexCoordinates, HexCell>();

            InitializeCells();
        }

        private void InitializeCells()
        {
            for (int r = 0; r < _height; r++)
            {
                for (int q = 0; q < _width; q++)
                {
                    var coords = new HexCoordinates(q, r);
                    _cells[coords] = new HexCell(coords);
                }
            }
        }

        // Get a cell at the specified coordinates.
        public HexCell GetCell(HexCoordinates coords)
        {
            _cells.TryGetValue(coords, out HexCell cell);
            return cell;
        }

        // Get a cell at the specified q, r coordinates.
        public HexCell GetCell(int q, int r)
        {
            return GetCell(new HexCoordinates(q, r));
        }

        // Check if coordinates are within grid bounds.
        public bool IsInBounds(HexCoordinates coords)
        {
            return coords.q >= 0 && coords.q < _width &&
                   coords.r >= 0 && coords.r < _height;
        }

        // Get all valid neighboring cells.
        public List<HexCell> GetNeighbors(HexCoordinates coords)
        {
            var neighbors = new List<HexCell>();
            var offsets = HexCoordinates.GetNeighborOffsets();

            foreach (var offset in offsets)
            {
                var neighborCoords = coords + offset;
                var cell = GetCell(neighborCoords);
                if (cell != null)
                {
                    neighbors.Add(cell);
                }
            }

            return neighbors;
        }

        // Get all cells within a specified range.
        public List<HexCell> GetCellsInRange(HexCoordinates center, int range)
        {
            var result = new List<HexCell>();

            for (int dq = -range; dq <= range; dq++)
            {
                for (int dr = Mathf.Max(-range, -dq - range); dr <= Mathf.Min(range, -dq + range); dr++)
                {
                    var coords = new HexCoordinates(center.q + dq, center.r + dr);
                    var cell = GetCell(coords);
                    if (cell != null)
                    {
                        result.Add(cell);
                    }
                }
            }

            return result;
        }

        // Get distance between two coordinates.
        public int GetDistance(HexCoordinates a, HexCoordinates b)
        {
            return HexCoordinates.Distance(a, b);
        }

        // Find path from start to end using BFS.
        // NOTE: Returns empty list if no path exists e.g essential for heuristic handling for AI
        public List<HexCoordinates> FindPath(HexCoordinates start, HexCoordinates end, int maxRange = int.MaxValue)
        {
            var endCell = GetCell(end);
            if (endCell == null || !endCell.CanEnter())
            {
                return new List<HexCoordinates>();
            }

            var frontier = new Queue<HexCoordinates>();
            var cameFrom = new Dictionary<HexCoordinates, HexCoordinates>();
            var costSoFar = new Dictionary<HexCoordinates, int>();

            frontier.Enqueue(start);
            cameFrom[start] = start;
            costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == end)
                {
                    break;
                }

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!neighbor.CanEnter() && neighbor.Coordinates != end)
                    {
                        continue;
                    }

                    int newCost = costSoFar[current] + neighbor.MovementCost;

                    if (newCost > maxRange)
                    {
                        continue;
                    }

                    if (!costSoFar.ContainsKey(neighbor.Coordinates) || newCost < costSoFar[neighbor.Coordinates])
                    {
                        costSoFar[neighbor.Coordinates] = newCost;
                        frontier.Enqueue(neighbor.Coordinates);
                        cameFrom[neighbor.Coordinates] = current;
                    }
                }
            }

            // Reconstruct path
            if (!cameFrom.ContainsKey(end))
            {
                return new List<HexCoordinates>();
            }

            var path = new List<HexCoordinates>();
            var step = end;
            while (step != start)
            {
                path.Add(step);
                step = cameFrom[step];
            }
            path.Reverse();

            return path;
        }

        // Get all walkable cells within movement range.
        public List<HexCell> GetReachableCells(HexCoordinates start, int movementRange)
        {
            var reachable = new List<HexCell>();
            var frontier = new Queue<HexCoordinates>();
            var visited = new Dictionary<HexCoordinates, int>();

            frontier.Enqueue(start);
            visited[start] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                var currentCost = visited[current];

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!neighbor.IsWalkable)
                    {
                        continue;
                    }

                    int newCost = currentCost + neighbor.MovementCost;

                    if (newCost > movementRange)
                    {
                        continue;
                    }

                    if (!visited.ContainsKey(neighbor.Coordinates))
                    {
                        visited[neighbor.Coordinates] = newCost;
                        frontier.Enqueue(neighbor.Coordinates);

                        if (neighbor.CanEnter())
                        {
                            reachable.Add(neighbor);
                        }
                    }
                }
            }

            return reachable;
        }

        // Place a unit at the specified coordinates.
        public bool PlaceUnit(Unit unit, HexCoordinates coords)
        {
            var cell = GetCell(coords);
            if (cell == null || !cell.CanEnter())
            {
                return false;
            }

            // Remove from previous cell if any
            if (unit.CurrentCell != null)
            {
                unit.CurrentCell.ClearOccupant();
            }

            cell.SetOccupant(unit);
            unit.SetPosition(coords, cell);
            return true;
        }

        // Move a unit from one cell to another.
        public bool MoveUnit(Unit unit, HexCoordinates destination)
        {
            var destCell = GetCell(destination);
            if (destCell == null || !destCell.CanEnter())
            {
                return false;
            }

            unit.CurrentCell?.ClearOccupant();
            destCell.SetOccupant(unit);
            unit.SetPosition(destination, destCell);
            return true;
        }

        // Get all cells for iteration
        public IEnumerable<HexCell> GetAllCells()
        {
            return _cells.Values;
        }
    }
}
