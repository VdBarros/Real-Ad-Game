using System;
using System.Globalization;

namespace Game.Domain
{
    public readonly struct TilePosition : IEquatable<TilePosition>, IComparable<TilePosition>
    {
        public TilePosition(int elevation, int x, int y)
        {
            Elevation = elevation;
            X = x;
            Y = y;
        }

        public int Elevation { get; }

        public int X { get; }

        public int Y { get; }

        public int CompareTo(TilePosition other)
        {
            var byElevation = Elevation.CompareTo(other.Elevation);
            if (byElevation != 0)
            {
                return byElevation;
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
            return Elevation == other.Elevation && X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is TilePosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Elevation;
                hash = (hash * 397) ^ X;
                hash = (hash * 397) ^ Y;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "(",
                Elevation.ToString(CultureInfo.InvariantCulture),
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
