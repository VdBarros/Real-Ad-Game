using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct Fight : IEquatable<Fight>
    {
        public const float BlowSeconds = 0.09f;

        public const float ShoveSeconds = 0.08f;

        public const float DissolveSeconds = 0.34f;

        public const float ClashSeconds = 0.24f;

        public const float StaggerSeconds = 0.44f;

        public const float StepTiles = 0.50f;

        public const float ClashTiles = 0.26f;

        public const float KnockbackTiles = 0.92f;

        static readonly float[] recoveries = { 0f, 0f, DissolveSeconds, ClashSeconds, StaggerSeconds };

        static readonly float[] shoves = { 0f, 0f, -StepTiles, -ClashTiles, -KnockbackTiles };

        static readonly float[] recoils = { 0f, 0f, 0f, ClashTiles, 0f };

        static readonly bool[] dissolves = { false, false, true, false, false };

        static readonly Spark[] sparks =
        {
            Spark.None,
            Spark.None,
            new Spark(0.25f, 0.95f, new Tint(1f, 0.95f, 0.72f)),
            new Spark(0f, 0.72f, new Tint(0.78f, 0.87f, 0.98f)),
            new Spark(-0.45f, 0.86f, new Tint(0.92f, 0.18f, 0.14f))
        };

        readonly ActionOutcome outcome;
        readonly float elapsed;

        Fight(ActionOutcome outcome, float elapsed)
        {
            this.outcome = outcome;
            this.elapsed = elapsed;
        }

        public static Fight None
        {
            get { return default(Fight); }
        }

        public static Fight Of(ActionOutcome outcome)
        {
            var joined = new Fight(outcome, 0f);
            return joined.IsJoined ? joined : None;
        }

        public ActionOutcome Outcome
        {
            get { return outcome; }
        }

        public bool IsJoined
        {
            get { return Seconds > 0f; }
        }

        public bool IsSettled
        {
            get { return elapsed >= Seconds; }
        }

        public float Seconds
        {
            get
            {
                var recovery = recoveries[Slot()];
                return recovery <= 0f ? 0f : ShoveSeconds + recovery;
            }
        }

        public float Shove
        {
            get { return Thrown(shoves[Slot()]); }
        }

        public float Recoil
        {
            get { return Thrown(recoils[Slot()]); }
        }

        public bool Dissolves
        {
            get { return dissolves[Slot()]; }
        }

        public float Fade
        {
            get
            {
                if (!Dissolves)
                {
                    return 1f;
                }

                if (IsSettled)
                {
                    return 0f;
                }

                var dissolving = elapsed - ShoveSeconds;
                return dissolving <= 0f ? 1f : 1f - EaseOut(dissolving / DissolveSeconds);
            }
        }

        public Spark Spark
        {
            get
            {
                var flash = sparks[Slot()];
                if (!IsJoined || elapsed <= 0f || elapsed >= BlowSeconds)
                {
                    return flash.Sized(0f);
                }

                var struck = elapsed / BlowSeconds;
                return flash.Sized(flash.Scale * 4f * struck * (1f - struck));
            }
        }

        public Fight Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A fight only ever runs forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            return new Fight(outcome, elapsed + deltaSeconds);
        }

        int Slot()
        {
            var slot = (int)outcome;
            if (slot < 0 || slot >= recoveries.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outcome), outcome, "No fight plays out that outcome.");
            }

            return slot;
        }

        float Thrown(float peak)
        {
            if (peak == 0f || elapsed <= 0f)
            {
                return 0f;
            }

            if (elapsed < ShoveSeconds)
            {
                return peak * EaseOut(elapsed / ShoveSeconds);
            }

            var recovering = (elapsed - ShoveSeconds) / recoveries[Slot()];
            return recovering >= 1f ? 0f : peak * (1f - EaseOut(recovering));
        }

        static float EaseOut(float t)
        {
            var remaining = 1f - t;
            return 1f - remaining * remaining * remaining;
        }

        public bool Equals(Fight other)
        {
            return outcome == other.outcome && elapsed.Equals(other.elapsed);
        }

        public override bool Equals(object obj)
        {
            return obj is Fight other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)outcome * 397) ^ elapsed.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (!IsJoined)
            {
                return "no fight";
            }

            return string.Concat(outcome.ToString(), IsSettled ? ", fought" : ", fighting");
        }
    }
}
