using System;
using UnityEngine;

namespace Game.Combat.Grid
{
    /// <summary>
    /// Immutable type representing axial hex coordinates (q, r).
    /// Uses cube coordinate system internally for distance calculations (from my research this seems optimal?)
    /// </summary>
    [System.Serializable] 
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public int q;
        public int r;

        // Cube coordinate S is derived by s = -q - r
        public int S => -q - r;

        public HexCoordinates(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        // Takes a position in offset coordinates (typical grid position) and converts to axial coordinates
        // This makes some algorithms easier
        public static HexCoordinates OffsetToAxial(HexCoordinates offsetPos)
        {
            float parity = offsetPos.r%2;
            return new HexCoordinates(
                (int)(offsetPos.q - (offsetPos.r - parity) / 2.0f),
                offsetPos.r);
        }

        // Reverses OffsetToAxial to map correct geometry back to our visual grid
        public static HexCoordinates AxialToOffset(HexCoordinates axialPos)
        {
            int parity = axialPos.r % 2;
            return new HexCoordinates(
                axialPos.q + (axialPos.r - parity) / 2,
                axialPos.r);
        }

        // Takes two positions in axial coordinates and calculates distance between them
        public static float AxialDistance(HexCoordinates A, HexCoordinates B)
        {
            HexCoordinates inter = A - B;
            return (Mathf.Abs(inter.q) + Mathf.Abs(inter.q + inter.r) + Mathf.Abs(inter.r)) / 2.0f;
        }

        // Calculate the distance between two hex cells.
        public static int Distance(HexCoordinates a, HexCoordinates b)
        {
            return (int)AxialDistance(OffsetToAxial(a), OffsetToAxial(b));
        }

        // Get all six neighboring coordinates.
        public static HexCoordinates[] GetNeighborOffsets(HexCoordinates inPos)
        {
            if (inPos.r % 2 == 0)
            {
                // Even offsets
                return new HexCoordinates[]
                {
                    new HexCoordinates(1, 0), new HexCoordinates(-1, 0),
                    new HexCoordinates(0, 1), new HexCoordinates(0, -1),
                    new HexCoordinates(-1, 1), new HexCoordinates(-1, -1)
                };
            }
            else
            {
                // Odd offsets
                return new HexCoordinates[]
                {
                    new HexCoordinates(1, 0), new HexCoordinates(-1, 0),
                    new HexCoordinates(0, 1), new HexCoordinates(0, -1),
                    new HexCoordinates(1, -1), new HexCoordinates(1, 1)
                };
            }
        }

        public HexCoordinates GetNeighbor(int direction, HexCoordinates inPos)
        {
            var offsets = GetNeighborOffsets(inPos);
            direction = ((direction % 6) + 6) % 6; // Normalize to 0-5
            return this + offsets[direction];
        }

        // Helper func that finds the neighbor hex directly opposite of the attacker's trajectory.
        // Converts to axial coords to safely calculate the dot product, then returns offset for shove direction.
        public HexCoordinates GetPushDestination(HexCoordinates attackerPos)
        {
            HexCoordinates axAttacker = OffsetToAxial(attackerPos);
            HexCoordinates axTarget = OffsetToAxial(this);

            // Trajectory vector from attacker to target
            int dq = axTarget.q - axAttacker.q;
            int dr = axTarget.r - axAttacker.r;
            int ds = axTarget.S - axAttacker.S;

            HexCoordinates bestNeighbor = this;
            int maxDot = -int.MaxValue;

            // Check all valid offset neighbors
            foreach (var offset in GetNeighborOffsets(this))
            {
                var neighbor = this + offset;
                HexCoordinates axNeighbor = OffsetToAxial(neighbor);

                // Trajectory vector from target to neighbor
                int ndq = axNeighbor.q - axTarget.q;
                int ndr = axNeighbor.r - axTarget.r;
                int nds = axNeighbor.S - axTarget.S;

                // Cube coordinate dot product
                int dot = (dq * ndq) + (dr * ndr) + (ds * nds);

                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestNeighbor = neighbor;
                }
            }
            return bestNeighbor;
        }

        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.q + b.q, a.r + b.r);
        }

        public static HexCoordinates operator -(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.q - b.q, a.r - b.r);
        }

        public static bool operator ==(HexCoordinates a, HexCoordinates b)
        {
            return a.q == b.q && a.r == b.r;
        }

        public static bool operator !=(HexCoordinates a, HexCoordinates b)
        {
            return !(a == b);
        }

        public bool Equals(HexCoordinates other)
        {
            return q == other.q && r == other.r;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinates other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(q, r);
        }

        public override string ToString()
        {
            return $"Hex({q}, {r})";
        }

        public static HexCoordinates Invalid = new HexCoordinates(int.MinValue, int.MinValue);
    }
}
