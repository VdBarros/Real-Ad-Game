using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct ScreenPoint : IEquatable<ScreenPoint>
    {
        public ScreenPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public static float Distance(ScreenPoint from, ScreenPoint to)
        {
            var across = to.X - from.X;
            var up = to.Y - from.Y;
            return (float)Math.Sqrt(across * across + up * up);
        }

        public bool Equals(ScreenPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is ScreenPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "(",
                X.ToString("0.#", CultureInfo.InvariantCulture),
                ", ",
                Y.ToString("0.#", CultureInfo.InvariantCulture),
                ") px");
        }

        public static bool operator ==(ScreenPoint left, ScreenPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ScreenPoint left, ScreenPoint right)
        {
            return !left.Equals(right);
        }
    }
}
