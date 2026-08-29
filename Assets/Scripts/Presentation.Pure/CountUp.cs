using System;

namespace Game.Presentation.Pure
{
    public readonly struct CountUp : IEquatable<CountUp>
    {
        public const float Seconds = 0.35f;

        readonly int from;
        readonly int to;
        readonly float elapsed;
        readonly float fromDigits;

        CountUp(int from, int to, float elapsed, float fromDigits)
        {
            this.from = from;
            this.to = to;
            this.elapsed = elapsed;
            this.fromDigits = fromDigits;
        }

        public static CountUp Settled(int value)
        {
            return new CountUp(value, value, Seconds, BadgeText.Digits(value));
        }

        public int From
        {
            get { return from; }
        }

        public int Target
        {
            get { return to; }
        }

        public bool IsSettled
        {
            get { return elapsed >= Seconds || from == to; }
        }

        public float Remaining
        {
            get { return IsSettled ? 0f : Seconds - elapsed; }
        }

        public int Display
        {
            get
            {
                if (IsSettled)
                {
                    return to;
                }

                var eased = EaseOut(elapsed / Seconds);
                return from + (int)Math.Round((to - from) * (double)eased, MidpointRounding.AwayFromZero);
            }
        }

        public float Digits
        {
            get
            {
                var landing = BadgeText.Digits(to);
                if (IsSettled)
                {
                    return landing;
                }

                return fromDigits + (landing - fromDigits) * EaseOut(elapsed / Seconds);
            }
        }

        public CountUp Toward(int target)
        {
            if (target == to)
            {
                return this;
            }

            return new CountUp(Display, target, 0f, Digits);
        }

        public CountUp Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A count-up only ever runs forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            return new CountUp(from, to, elapsed + deltaSeconds, fromDigits);
        }

        static float EaseOut(float t)
        {
            var remaining = 1f - t;
            return 1f - remaining * remaining * remaining;
        }

        public bool Equals(CountUp other)
        {
            return from == other.from
                && to == other.to
                && elapsed.Equals(other.elapsed)
                && fromDigits.Equals(other.fromDigits);
        }

        public override bool Equals(object obj)
        {
            return obj is CountUp other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = from;
                hash = (hash * 397) ^ to;
                hash = (hash * 397) ^ elapsed.GetHashCode();
                hash = (hash * 397) ^ fromDigits.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(from.ToString(), " -> ", to.ToString(), " showing ", Display.ToString());
        }
    }
}
