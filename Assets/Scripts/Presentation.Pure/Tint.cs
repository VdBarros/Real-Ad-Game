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

        public float Luminance
        {
            get
            {
                return (float)(0.2126 * Linear(Red) + 0.7152 * Linear(Green) + 0.0722 * Linear(Blue));
            }
        }

        public float Chroma
        {
            get { return Highest - Lowest; }
        }

        public float Hue
        {
            get
            {
                var span = Chroma;

                if (span <= 0f)
                {
                    return 0f;
                }

                float sixth;

                if (Highest == Red)
                {
                    sixth = (Green - Blue) / span;
                }
                else if (Highest == Green)
                {
                    sixth = (Blue - Red) / span + 2f;
                }
                else
                {
                    sixth = (Red - Green) / span + 4f;
                }

                var degrees = sixth * 60f;

                return degrees < 0f ? degrees + 360f : degrees;
            }
        }

        public static float Contrast(Tint one, Tint other)
        {
            var here = one.Luminance;
            var there = other.Luminance;
            var high = here > there ? here : there;
            var low = here > there ? there : here;

            return (high + 0.05f) / (low + 0.05f);
        }

        public static float HueApart(Tint one, Tint other)
        {
            var apart = one.Hue - other.Hue;

            if (apart < 0f)
            {
                apart = -apart;
            }

            return apart > 180f ? 360f - apart : apart;
        }

        float Highest
        {
            get
            {
                var high = Red > Green ? Red : Green;

                return high > Blue ? high : Blue;
            }
        }

        float Lowest
        {
            get
            {
                var low = Red < Green ? Red : Green;

                return low < Blue ? low : Blue;
            }
        }

        static double Linear(float channel)
        {
            return channel <= 0.04045f ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

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
