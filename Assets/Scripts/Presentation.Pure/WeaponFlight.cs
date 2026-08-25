using System;

namespace Game.Presentation.Pure
{
    public readonly struct WeaponFlight : IEquatable<WeaponFlight>
    {
        public const float Seconds = Promotion.Seconds;

        public const float Arc = 0.9f;

        public const float Spins = 2f;

        readonly WorldPoint from;
        readonly WorldPoint to;
        readonly float left;

        WeaponFlight(WorldPoint from, WorldPoint to, float left)
        {
            this.from = from;
            this.to = to;
            this.left = left;
        }

        public static WeaponFlight None
        {
            get { return default(WeaponFlight); }
        }

        public static WeaponFlight From(WorldPoint site, WorldPoint carrier)
        {
            return new WeaponFlight(site, carrier, Seconds);
        }

        public bool IsSettled
        {
            get { return left <= 0f; }
        }

        public float Travelled
        {
            get { return IsSettled ? 1f : 1f - left / Seconds; }
        }

        public WorldPoint Position
        {
            get
            {
                var t = Travelled;
                return new WorldPoint(
                    from.X + (to.X - from.X) * t,
                    from.Y + (to.Y - from.Y) * t + Arc * 4f * t * (1f - t),
                    from.Z + (to.Z - from.Z) * t);
            }
        }

        public float Spin
        {
            get { return 360f * Spins * Travelled; }
        }

        public WeaponFlight Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A weapon only ever flies forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            return new WeaponFlight(from, to, left - deltaSeconds);
        }

        public bool Equals(WeaponFlight other)
        {
            return from.Equals(other.from) && to.Equals(other.to) && left.Equals(other.left);
        }

        public override bool Equals(object obj)
        {
            return obj is WeaponFlight other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = from.GetHashCode();
                hash = (hash * 397) ^ to.GetHashCode();
                hash = (hash * 397) ^ left.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(from.ToString(), " -> ", to.ToString(), " at ", Position.ToString());
        }
    }
}
