using System;
using System.Globalization;

namespace Game.Domain
{
    public readonly struct TilePosition : IEquatable<TilePosition>, IComparable<TilePosition>
    {
        public TilePosition(int floor, int x, int y)
        {
            Floor = floor;
            X = x;
            Y = y;
        }

        public int Floor { get; }

        public int X { get; }

        public int Y { get; }

        public int CompareTo(TilePosition other)
        {
            var byFloor = Floor.CompareTo(other.Floor);
            if (byFloor != 0)
            {
                return byFloor;
            }

            var byRow = Y.CompareTo(other.Y);
            if (byRow != 0)
            {
                return byRow;
            }

            return X.CompareTo(other.X);
        }

        public bool Equals(TilePosition other)
        {
            return Floor == other.Floor && X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is TilePosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Floor;
                hash = (hash * 397) ^ X;
                hash = (hash * 397) ^ Y;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "(",
                Floor.ToString(CultureInfo.InvariantCulture),
                ":",
                X.ToString(CultureInfo.InvariantCulture),
                ",",
                Y.ToString(CultureInfo.InvariantCulture),
                ")");
        }

        public static bool operator ==(TilePosition left, TilePosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TilePosition left, TilePosition right)
        {
            return !left.Equals(right);
        }
    }
}
