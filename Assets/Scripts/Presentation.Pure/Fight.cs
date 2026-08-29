using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct Fight : IEquatable<Fight>
    {
        public const float BlowSeconds = 0.09f;

        public const float ShoveSeconds = 0.08f;

        public const float TieSeconds = 0.24f;

        public const float LossSeconds = 0.44f;

        public const float ClashTiles = 0.26f;

        public const float KnockbackTiles = 0.92f;

        public const float LungeTiles = 0.18f;

        public const float BlowTiles = 0.68f;

        static readonly float[] blowsAt = { 0f, 0.32f, 0.58f };

        static readonly bool[] thrownByThePlayer = { true, false, true };

        static readonly Spark playersBlow = new Spark(0.25f, 0.95f, new Tint(1f, 0.95f, 0.72f));

        static readonly Spark enemysBlow = new Spark(-0.45f, 0.86f, new Tint(0.78f, 0.87f, 0.98f));

        static readonly float[] recoveries = { 0f, 0f, 0f, TieSeconds, LossSeconds };

        static readonly float[] shoves = { 0f, 0f, 0f, -ClashTiles, -KnockbackTiles };

        static readonly float[] recoils = { 0f, 0f, 0f, ClashTiles, 0f };

        static readonly bool[] dissolves = { false, false, true, false, false };

        static readonly Spark[] sparks =
        {
            Spark.None,
            Spark.None,
            Spark.None,
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

        public static int Blows
        {
            get { return blowsAt.Length; }
        }

        public static float BlowOpensAt(int blow)
        {
            return blowsAt[Thrown(blow)];
        }

        public static float BlowSecondsOf(int blow)
        {
            var slot = Thrown(blow);
            var closes = slot + 1 < blowsAt.Length ? blowsAt[slot + 1] : VictoryStages.ClashSeconds;

            return closes - blowsAt[slot];
        }

        public static bool BlowIsThePlayers(int blow)
        {
            return thrownByThePlayer[Thrown(blow)];
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

        public bool IsTrading
        {
            get { return Blow() >= 0; }
        }

        public bool ThePlayerThrewIt
        {
            get
            {
                var blow = Blow();
                return blow < 0 || thrownByThePlayer[blow];
            }
        }

        public float Beat
        {
            get
            {
                if (!IsJoined)
                {
                    return 0f;
                }

                if (!Won)
                {
                    return Seconds;
                }

                var blow = Blow();
                return blow < 0 ? 0f : BlowSecondsOf(blow);
            }
        }

        public float Shove
        {
            get
            {
                if (!Won)
                {
                    return Swung(shoves[Slot()]);
                }

                var blow = Blow();
                return blow < 0 ? 0f : Landed(blow, thrownByThePlayer[blow] ? LungeTiles : -BlowTiles);
            }
        }

        public float Recoil
        {
            get
            {
                if (!Won)
                {
                    return Swung(recoils[Slot()]);
                }

                var blow = Blow();
                return blow < 0 ? 0f : Landed(blow, thrownByThePlayer[blow] ? BlowTiles : -LungeTiles);
            }
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

        public Spark Spark
        {
            get
            {
                if (Won)
                {
                    return Struck();
                }

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

        int Blow()
        {
            if (!Won || Timeline.Stage != VictoryStage.Clash)
            {
                return -1;
            }

            var at = Timeline.StageElapsed;
            var thrown = 0;

            for (var slot = 1; slot < blowsAt.Length; slot++)
            {
                if (at >= blowsAt[slot])
                {
                    thrown = slot;
                }
            }

            return thrown;
        }

        Spark Struck()
        {
            var blow = Blow();
            if (blow < 0)
            {
                return Spark.None;
            }

            var flash = thrownByThePlayer[blow] ? playersBlow : enemysBlow;
            var into = Timeline.StageElapsed - blowsAt[blow];

            if (into <= 0f || into >= BlowSeconds)
            {
                return flash.Sized(0f);
            }

            var struck = into / BlowSeconds;
            return flash.Sized(flash.Scale * 4f * struck * (1f - struck));
        }

        float Landed(int blow, float peak)
        {
            var into = Timeline.StageElapsed - blowsAt[blow];
            if (peak == 0f || into <= 0f)
            {
                return 0f;
            }

            if (into < BlowSeconds)
            {
                return peak * EaseOut(into / BlowSeconds);
            }

            var recovering = BlowSecondsOf(blow) - BlowSeconds;
            if (recovering <= 0f)
            {
                return 0f;
            }

            var settling = (into - BlowSeconds) / recovering;
            return settling >= 1f ? 0f : peak * (1f - EaseOut(settling));
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

        static int Thrown(int blow)
        {
            if (blow < 0 || blow >= blowsAt.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blow), blow, "The clash throws no such blow.");
            }

            return blow;
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
