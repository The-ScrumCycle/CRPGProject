using System;
using UnityEngine;

namespace Game.Combat.Grid
{
    /// <summary>
    /// Immutable type representing axial hex coordinates (q, r).
    /// Uses cube coordinate system internally for distance calculations (from my research this seems optimal?)
    /// </summary>
    [Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public readonly int q;
        public readonly int r;

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

        public static readonly HexCoordinates Invalid = new HexCoordinates(int.MinValue, int.MinValue);
    }
}
