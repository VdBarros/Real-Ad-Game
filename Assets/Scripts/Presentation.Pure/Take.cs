using System;

namespace Game.Presentation.Pure
{
    public readonly struct Take : IEquatable<Take>
    {
        public const float Seconds = 0.30f;

        public const float PedestalEdge = 0.62f;

        public const float PedestalHeight = 0.10f;

        static readonly Tint Stone = new Tint(0.50f, 0.48f, 0.54f);

        readonly float elapsed;
        readonly bool spending;

        Take(float elapsed, bool spending)
        {
            this.elapsed = elapsed;
            this.spending = spending;
        }

        public static Take None
        {
            get { return default(Take); }
        }

        public static Take Spent
        {
            get { return new Take(Seconds, true); }
        }

        public static Take Begun()
        {
            return new Take(0f, true);
        }

        public bool IsSpent
        {
            get { return spending; }
        }

        public bool IsSettled
        {
            get { return !spending || elapsed >= Seconds; }
        }

        public float Edge
        {
            get { return Blend(LevelBlueprintBuilder.PickupScale, PedestalEdge); }
        }

        public float Height
        {
            get { return Blend(LevelBlueprintBuilder.PickupScale, PedestalHeight); }
        }

        public Tint Wash(Tint gem)
        {
            return Tint.Lerp(gem, Stone, Collapsed);
        }

        public Take Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A take only ever runs forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            var moved = elapsed + deltaSeconds;
            return moved >= Seconds ? Spent : new Take(moved, true);
        }

        float Collapsed
        {
            get
            {
                if (!spending)
                {
                    return 0f;
                }

                return IsSettled ? 1f : EaseOut(elapsed / Seconds);
            }
        }

        float Blend(float cube, float pedestal)
        {
            return cube + (pedestal - cube) * Collapsed;
        }

        static float EaseOut(float t)
        {
            var remaining = 1f - t;
            return 1f - remaining * remaining * remaining;
        }

        public bool Equals(Take other)
        {
            return elapsed.Equals(other.elapsed) && spending == other.spending;
        }

        public override bool Equals(object obj)
        {
            return obj is Take other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (elapsed.GetHashCode() * 397) ^ (spending ? 1 : 0);
            }
        }

        public override string ToString()
        {
            if (!spending)
            {
                return "untaken";
            }

            return IsSettled ? "spent" : "being taken";
        }
    }
}
