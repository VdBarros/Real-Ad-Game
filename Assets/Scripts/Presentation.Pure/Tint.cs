using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct Tint : IEquatable<Tint>
    {
        public Tint(float red, float green, float blue)
        {
            Red = Clamp(red);
            Green = Clamp(green);
            Blue = Clamp(blue);
        }

        public float Red { get; }

        public float Green { get; }

        public float Blue { get; }

        public static Tint Lerp(Tint from, Tint to, float amount)
        {
            return new Tint(
                from.Red + (to.Red - from.Red) * amount,
                from.Green + (to.Green - from.Green) * amount,
                from.Blue + (to.Blue - from.Blue) * amount);
        }

        static float Clamp(float channel)
        {
            if (channel < 0f)
            {
                return 0f;
            }

            return channel > 1f ? 1f : channel;
        }

        public bool Equals(Tint other)
        {
            return Red.Equals(other.Red) && Green.Equals(other.Green) && Blue.Equals(other.Blue);
        }

        public override bool Equals(object obj)
        {
            return obj is Tint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Red.GetHashCode();
                hash = (hash * 397) ^ Green.GetHashCode();
                hash = (hash * 397) ^ Blue.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "rgb(",
                Red.ToString("0.###", CultureInfo.InvariantCulture),
                ", ",
                Green.ToString("0.###", CultureInfo.InvariantCulture),
                ", ",
                Blue.ToString("0.###", CultureInfo.InvariantCulture),
                ")");
        }
    }
}
