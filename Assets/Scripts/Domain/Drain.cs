using System;

namespace Game.Domain
{
    public readonly struct Drain : IEquatable<Drain>
    {
        public const int Floor = 1;

        public const float RampSeconds = 0.30f;

        public const float Seconds = 2.00f;

        readonly Contact contact;
        readonly int from;
        readonly float elapsed;

        Drain(Contact contact, int from, float elapsed)
        {
            this.contact = contact;
            this.from = from;
            this.elapsed = elapsed;
        }

        public static Drain None
        {
            get { return default(Drain); }
        }

        public static Drain Against(int power)
        {
            RequirePower(power);
            return new Drain(Contact.Held, power, 0f);
        }

        public static int PowerAfter(int power, float seconds)
        {
            RequirePower(power);

            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "A drain only ever runs forwards.");
            }

            var span = power - Floor;
            if (span <= 0 || seconds >= Seconds)
            {
                return Floor;
            }

            var left = span * (1f - Spent(seconds));

            return left <= 0f ? Floor : Floor + (int)Math.Ceiling(left);
        }

        public static float Spent(float seconds)
        {
            if (seconds <= 0f)
            {
                return 0f;
            }

            if (seconds >= Seconds)
            {
                return 1f;
            }

            var rate = 1f / (Seconds - RampSeconds * 0.5f);
            var spent = seconds < RampSeconds
                ? rate * seconds * seconds / (2f * RampSeconds)
                : rate * (seconds - RampSeconds * 0.5f);

            return spent >= 1f ? 1f : spent;
        }

        public bool HasLetGo
        {
            get { return contact == Contact.LetGo; }
        }

        public bool IsHeld
        {
            get { return contact != Contact.None; }
        }

        public int From
        {
            get { return from; }
        }

        public float Elapsed
        {
            get { return elapsed; }
        }

        public int Power
        {
            get { return IsHeld ? PowerAfter(from, elapsed) : 0; }
        }

        public int Lost
        {
            get { return IsHeld ? from - Power : 0; }
        }

        public bool IsEmpty
        {
            get { return IsHeld && Power <= Floor; }
        }

        public bool IsRunning
        {
            get { return contact == Contact.Held && !IsEmpty; }
        }

        public Drain Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A drain only ever runs forwards.");
            }

            if (!IsRunning)
            {
                return this;
            }

            return new Drain(contact, from, elapsed + deltaSeconds);
        }

        public Drain Stopped()
        {
            return contact == Contact.Held ? new Drain(Contact.LetGo, from, elapsed) : this;
        }

        static void RequirePower(int power)
        {
            if (power < Floor)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(power), power, "A drain runs on power the run is already holding.");
            }
        }

        public bool Equals(Drain other)
        {
            return contact == other.contact && from == other.from && elapsed.Equals(other.elapsed);
        }

        public override bool Equals(object obj)
        {
            return obj is Drain other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)contact;
                hash = (hash * 397) ^ from;
                hash = (hash * 397) ^ elapsed.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (!IsHeld)
            {
                return "no drain";
            }

            return string.Concat(
                from.ToString(), " -> ", Power.ToString(), IsRunning ? ", draining" : ", let go");
        }
    }
}
