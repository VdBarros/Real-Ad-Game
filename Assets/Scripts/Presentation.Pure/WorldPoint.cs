using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct WorldPoint : IEquatable<WorldPoint>
    {
        public WorldPoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public static float Dot(WorldPoint first, WorldPoint second)
        {
            return first.X * second.X + first.Y * second.Y + first.Z * second.Z;
        }

        public bool Equals(WorldPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ Z.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "(",
                X.ToString("0.###", CultureInfo.InvariantCulture),
                ", ",
                Y.ToString("0.###", CultureInfo.InvariantCulture),
                ", ",
                Z.ToString("0.###", CultureInfo.InvariantCulture),
                ")");
        }

        public static bool operator ==(WorldPoint left, WorldPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WorldPoint left, WorldPoint right)
        {
            return !left.Equals(right);
        }
    }
}
