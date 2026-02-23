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

        // Calculate the distance between two hex cells.
        public static int Distance(HexCoordinates a, HexCoordinates b)
        {
            return (Mathf.Abs(a.q - b.q) + Mathf.Abs(a.r - b.r) + Mathf.Abs(a.S - b.S)) / 2;
        }

        // Get all six neighboring coordinates.
        public static HexCoordinates[] GetNeighborOffsets()
        {
            return new HexCoordinates[]
            {
                new HexCoordinates(1, 0),   // East
                new HexCoordinates(1, -1),  // Northeast
                new HexCoordinates(0, -1),  // Northwest
                new HexCoordinates(-1, 0),  // West
                new HexCoordinates(-1, 1),  // Southwest
                new HexCoordinates(0, 1)    // Southeast
            };
        }

        public HexCoordinates GetNeighbor(int direction)
        {
            var offsets = GetNeighborOffsets();
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
