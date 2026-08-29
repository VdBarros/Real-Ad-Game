using System;

namespace Game.Presentation.Pure
{
    public readonly struct LandmarkPiece : IEquatable<LandmarkPiece>
    {
        public LandmarkPiece(WorldPart part, Tint tint)
        {
            Part = part;
            Tint = tint;
        }

        public WorldPart Part { get; }

        public Tint Tint { get; }

        public bool Equals(LandmarkPiece other)
        {
            return Part.Equals(other.Part) && Tint.Equals(other.Tint);
        }

        public override bool Equals(object obj)
        {
            return obj is LandmarkPiece other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Part.GetHashCode() * 397) ^ Tint.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Concat(Part.ToString(), " in ", Tint.ToString());
        }
    }
}
