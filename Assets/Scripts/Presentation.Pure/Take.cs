using System;

namespace Game.Presentation.Pure
{
    public readonly struct Take : IEquatable<Take>
    {
        public const float Seconds = 0.30f;

        public const float LidAngle = 104f;

        public const float LidShare = 0.55f;

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

        public bool IsGone
        {
            get { return Opacity <= 0f; }
        }

        public float LidSwing
        {
            get { return EaseOut(Part(0f, LidShare)) * LidAngle; }
        }

        public float Opacity
        {
            get { return 1f - EaseOut(Part(LidShare, 1f)); }
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

        float Played
        {
            get
            {
                if (!spending)
                {
                    return 0f;
                }

                return IsSettled ? 1f : elapsed / Seconds;
            }
        }

        float Part(float from, float to)
        {
            var run = Played - from;

            if (run <= 0f)
            {
                return 0f;
            }

            var span = to - from;

            return run >= span ? 1f : run / span;
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

            return IsSettled ? "gone" : "opening";
        }
    }
}
