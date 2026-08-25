using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct Spark : IEquatable<Spark>
    {
        public const float Lift = 0.55f;

        public const float Edge = 0.70f;

        public Spark(float sway, float scale, Tint tint)
        {
            Sway = sway;
            Scale = scale < 0f ? 0f : scale;
            Tint = tint;
        }

        public static Spark None
        {
            get { return default(Spark); }
        }

        public float Sway { get; }

        public float Scale { get; }

        public Tint Tint { get; }

        public bool IsLit
        {
            get { return Scale > 0f; }
        }

        public Spark Sized(float scale)
        {
            return new Spark(Sway, scale, Tint);
        }

        public bool Equals(Spark other)
        {
            return Sway.Equals(other.Sway) && Scale.Equals(other.Scale) && Tint.Equals(other.Tint);
        }

        public override bool Equals(object obj)
        {
            return obj is Spark other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Sway.GetHashCode();
                hash = (hash * 397) ^ Scale.GetHashCode();
                hash = (hash * 397) ^ Tint.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (!IsLit)
            {
                return "dark";
            }

            return string.Concat(
                Tint.ToString(),
                " at ",
                Sway.ToString("0.###", CultureInfo.InvariantCulture),
                " sized ",
                Scale.ToString("0.###", CultureInfo.InvariantCulture));
        }
    }
}
