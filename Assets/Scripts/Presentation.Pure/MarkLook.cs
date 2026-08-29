using System;

namespace Game.Presentation.Pure
{
    public readonly struct MarkLook : IEquatable<MarkLook>
    {
        public MarkLook(Tint tint, float weight, float scale, float opacity)
        {
            if (weight < 0f || weight > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weight), weight, "A mark washes a badge by none of it, all of it, or something between.");
            }

            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "A badge always has a size.");
            }

            if (opacity <= 0f || opacity > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(opacity), opacity, "A mark fades a badge, and a badge faded away entirely says nothing.");
            }

            Tint = tint;
            Weight = weight;
            Scale = scale;
            Opacity = opacity;
        }

        public Tint Tint { get; }

        public float Weight { get; }

        public float Scale { get; }

        public float Opacity { get; }

        public float Brightness
        {
            get { return ((Tint.Red + Tint.Green + Tint.Blue) * Weight + (1f - Weight)) * Opacity; }
        }

        public bool Equals(MarkLook other)
        {
            return Tint.Equals(other.Tint)
                && Weight.Equals(other.Weight)
                && Scale.Equals(other.Scale)
                && Opacity.Equals(other.Opacity);
        }

        public override bool Equals(object obj)
        {
            return obj is MarkLook other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Tint.GetHashCode();
                hash = (hash * 397) ^ Weight.GetHashCode();
                hash = (hash * 397) ^ Scale.GetHashCode();
                hash = (hash * 397) ^ Opacity.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                Tint.ToString(),
                " at ",
                Weight.ToString("0.##"),
                ", sized ",
                Scale.ToString("0.##"),
                ", ",
                Opacity.ToString("0.##"),
                " opaque");
        }
    }
}
