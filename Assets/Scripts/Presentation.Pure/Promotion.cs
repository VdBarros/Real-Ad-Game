using System;

namespace Game.Presentation.Pure
{
    public readonly struct Promotion : IEquatable<Promotion>
    {
        public const float Seconds = 0.25f;

        public const float Tension = 2.2f;

        readonly float fromScale;
        readonly float elapsed;

        Promotion(float fromScale, PlayerLook target, float elapsed)
        {
            this.fromScale = fromScale;
            this.elapsed = elapsed;
            Target = target;
        }

        public static Promotion Settled(PlayerLook look)
        {
            return new Promotion(look.Scale, look, Seconds);
        }

        public PlayerLook Target { get; }

        public bool IsSettled
        {
            get { return elapsed >= Seconds; }
        }

        public float Scale
        {
            get
            {
                if (IsSettled)
                {
                    return Target.Scale;
                }

                return fromScale + (Target.Scale - fromScale) * Overshoot(elapsed / Seconds);
            }
        }

        public Promotion Toward(PlayerLook look)
        {
            if (look.Equals(Target))
            {
                return this;
            }

            return new Promotion(Scale, look, 0f);
        }

        public Promotion Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A promotion only ever runs forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            return new Promotion(fromScale, Target, elapsed + deltaSeconds);
        }

        static float Overshoot(float t)
        {
            var past = t - 1f;
            return 1f + (Tension + 1f) * past * past * past + Tension * past * past;
        }

        public bool Equals(Promotion other)
        {
            return fromScale.Equals(other.fromScale)
                && elapsed.Equals(other.elapsed)
                && Target.Equals(other.Target);
        }

        public override bool Equals(object obj)
        {
            return obj is Promotion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = fromScale.GetHashCode();
                hash = (hash * 397) ^ elapsed.GetHashCode();
                hash = (hash * 397) ^ Target.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(IsSettled ? "settled on " : "growing into ", Target.ToString());
        }
    }
}
