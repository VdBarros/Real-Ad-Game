using System;

namespace Game.Presentation.Pure
{
    public readonly struct TrailDot : IEquatable<TrailDot>
    {
        public TrailDot(WorldPoint position, float step)
        {
            Position = position;
            Step = step;
        }

        public WorldPoint Position { get; }

        public float Step { get; }

        public bool Equals(TrailDot other)
        {
            return Position.Equals(other.Position) && Step.Equals(other.Step);
        }

        public override bool Equals(object obj)
        {
            return obj is TrailDot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ Step.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "dot at " + Position;
        }
    }
}
