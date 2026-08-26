using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct CameraFlight : IEquatable<CameraFlight>
    {
        public const float Seconds = 2f;

        public const float HoldSeconds = 0.6f;

        public const float Duration = Seconds + HoldSeconds;

        readonly CameraFraming from;
        readonly CameraFraming to;
        readonly float elapsed;

        CameraFlight(CameraFraming from, CameraFraming to, float elapsed)
        {
            this.from = from;
            this.to = to;
            this.elapsed = elapsed;
        }

        public static CameraFlight Over(LevelGraph graph)
        {
            return new CameraFlight(LevelFraming.Opening(graph), LevelFraming.Whole(graph), 0f);
        }

        public CameraFraming Destination
        {
            get { return to; }
        }

        public bool IsSettled
        {
            get { return elapsed >= Duration; }
        }

        public bool IsHolding
        {
            get { return elapsed >= Seconds && elapsed < Duration; }
        }

        public CameraFraming Framing
        {
            get
            {
                if (elapsed >= Seconds)
                {
                    return to;
                }

                return CameraFraming.Between(from, to, EasedInAndOut(elapsed / Seconds));
            }
        }

        public CameraFlight Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A flight only ever runs forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            return new CameraFlight(from, to, elapsed + deltaSeconds);
        }

        public CameraFlight Skipped()
        {
            if (IsSettled)
            {
                return this;
            }

            return new CameraFlight(from, to, Duration);
        }

        static float EasedInAndOut(float t)
        {
            if (t < 0.5f)
            {
                return 2f * t * t;
            }

            var left = 1f - t;
            return 1f - 2f * left * left;
        }

        public bool Equals(CameraFlight other)
        {
            return from.Equals(other.from) && to.Equals(other.to) && elapsed.Equals(other.elapsed);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraFlight other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = from.GetHashCode();
                hash = (hash * 397) ^ to.GetHashCode();
                hash = (hash * 397) ^ elapsed.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (IsSettled)
            {
                return "let go of " + to;
            }

            if (IsHolding)
            {
                return "holding on " + to;
            }

            return string.Concat("revealing ", from.ToString(), " out to ", to.ToString());
        }
    }
}
