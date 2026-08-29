using System;

namespace Game.Presentation.Pure
{
    public readonly struct TrailLook : IEquatable<TrailLook>
    {
        public TrailLook(Tint tint, float size)
        {
            if (size <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "A dot always has a size.");
            }

            Tint = tint;
            Size = size;
        }

        public Tint Tint { get; }

        public float Size { get; }

        public bool Equals(TrailLook other)
        {
            return Tint.Equals(other.Tint) && Size.Equals(other.Size);
        }

        public override bool Equals(object obj)
        {
            return obj is TrailLook other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Tint.GetHashCode() * 397) ^ Size.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Concat(Tint.ToString(), " dots of ", Size.ToString("0.###"));
        }
    }
}
