using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct Fight : IEquatable<Fight>
    {
        public const float ShoveSeconds = 0.08f;

        public const float TieSeconds = 0.24f;

        public const float LossSeconds = 0.44f;

        public const float ExecutionAt = 0.34f;

        public const float ThrowSeconds = 0.12f;

        public const float ImpactSeconds = 0.20f;

        public const float ClashTiles = 0.26f;

        public const float KnockbackTiles = 0.92f;

        public const float LungeTiles = 0.18f;

        public const float BlowTiles = 0.68f;

        static readonly float[] recoveries = { 0f, 0f, 0f, TieSeconds, LossSeconds };

        static readonly float[] shoves = { 0f, 0f, 0f, -ClashTiles, -KnockbackTiles };

        static readonly float[] recoils = { 0f, 0f, 0f, ClashTiles, -LungeTiles };

        static readonly bool[] dissolves = { false, false, true, false, false };

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
                var slot = Slot();
                if (Won)
                {
                    return VictoryStages.BlockingSeconds;
                }

                var recovery = recoveries[slot];
                return recovery <= 0f ? 0f : ShoveSeconds + recovery;
            }
        }

        public VictoryTimeline Timeline
        {
            get { return Won ? VictoryTimeline.Begun.Advanced(elapsed) : VictoryTimeline.Unbegun; }
        }

        public VictoryStage Stage
        {
            get { return Timeline.Stage; }
        }

        public float ContactAt
        {
            get { return Won ? ExecutionAt : ShoveSeconds; }
        }

        public bool HasStruck
        {
            get { return IsJoined && !IsSettled && elapsed >= ContactAt; }
        }

        public bool IsExecuting
        {
            get { return Won && !IsSettled && Timeline.Stage == VictoryStage.Clash; }
        }

        public float BlowBeat
        {
            get
            {
                if (!IsJoined)
                {
                    return 0f;
                }

                return Won ? VictoryStages.ClashSeconds : Seconds;
            }
        }

        public float FallBeat
        {
            get
            {
                if (!IsJoined)
                {
                    return 0f;
                }

                if (Won)
                {
                    return VictoryStages.BlockingSeconds - ExecutionAt;
                }

                return outcome == ActionOutcome.Loss ? Seconds - ShoveSeconds : 0f;
            }
        }

        public float Impact
        {
            get
            {
                if (!IsJoined || IsSettled)
                {
                    return 0f;
                }

                var into = elapsed - ContactAt;
                if (into < 0f || into >= ImpactSeconds)
                {
                    return 0f;
                }

                var left = 1f - into / ImpactSeconds;
                return left * left;
            }
        }

        public float Shove
        {
            get { return Won ? Lunged(LungeTiles) : Swung(shoves[Slot()]); }
        }

        public float Recoil
        {
            get { return Won ? Thrown(BlowTiles) : Swung(recoils[Slot()]); }
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

                var timeline = Timeline;
                if (timeline.Stage == VictoryStage.Clash)
                {
                    return 1f;
                }

                if (timeline.Stage != VictoryStage.Dissolve)
                {
                    return 0f;
                }

                return 1f - EaseOut(timeline.Through);
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

        public Fight Broken()
        {
            if (!IsJoined || IsSettled)
            {
                return this;
            }

            return new Fight(outcome, Seconds);
        }

        bool Won
        {
            get { return outcome == ActionOutcome.Win; }
        }

        float Lunged(float peak)
        {
            if (IsSettled || Timeline.Stage != VictoryStage.Clash)
            {
                return 0f;
            }

            var at = Timeline.StageElapsed;
            if (at <= 0f)
            {
                return 0f;
            }

            if (at < ExecutionAt)
            {
                return peak * EaseOut(at / ExecutionAt);
            }

            var settling = (at - ExecutionAt) / (VictoryStages.ClashSeconds - ExecutionAt);
            return settling >= 1f ? 0f : peak * (1f - EaseOut(settling));
        }

        float Thrown(float peak)
        {
            if (IsSettled)
            {
                return 0f;
            }

            var into = elapsed - ExecutionAt;
            if (into <= 0f)
            {
                return 0f;
            }

            return into >= ThrowSeconds ? peak : peak * EaseOut(into / ThrowSeconds);
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

        float Swung(float peak)
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

            if (Won)
            {
                return string.Concat(outcome.ToString(), ", ", Timeline.ToString());
            }

            return string.Concat(outcome.ToString(), IsSettled ? ", fought" : ", fighting");
        }
    }
}
