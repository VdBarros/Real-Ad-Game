using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct VictoryTimeline : IEquatable<VictoryTimeline>
    {
        readonly bool begun;
        readonly float elapsed;

        VictoryTimeline(bool begun, float elapsed)
        {
            this.begun = begun;
            this.elapsed = elapsed;
        }

        public static VictoryTimeline Unbegun
        {
            get { return default(VictoryTimeline); }
        }

        public static VictoryTimeline Begun
        {
            get { return new VictoryTimeline(true, 0f); }
        }

        public bool HasBegun
        {
            get { return begun; }
        }

        public float Elapsed
        {
            get { return begun ? elapsed : 0f; }
        }

        public VictoryStage Stage
        {
            get
            {
                if (!begun)
                {
                    return VictoryStage.None;
                }

                foreach (var stage in VictoryStages.Order)
                {
                    if (elapsed < VictoryStages.ClosesAt(stage))
                    {
                        return stage;
                    }
                }

                return VictoryStage.Done;
            }
        }

        public float StageSeconds
        {
            get { return VictoryStages.SecondsOf(Stage); }
        }

        public float StageElapsed
        {
            get
            {
                if (!begun)
                {
                    return 0f;
                }

                var opened = elapsed - VictoryStages.OpensAt(Stage);
                return opened < 0f ? 0f : opened;
            }
        }

        public float Through
        {
            get
            {
                var span = StageSeconds;
                if (span <= 0f)
                {
                    return 1f;
                }

                var through = StageElapsed / span;
                return through > 1f ? 1f : through;
            }
        }

        public bool BlocksInput
        {
            get { return begun && VictoryStages.BlocksInput(Stage); }
        }

        public bool IsOver
        {
            get { return !begun || elapsed >= VictoryStages.Seconds; }
        }

        public float BlockingSecondsLeft
        {
            get
            {
                if (!begun)
                {
                    return 0f;
                }

                var left = VictoryStages.BlockingSeconds - elapsed;
                return left < 0f ? 0f : left;
            }
        }

        public float Overrun
        {
            get
            {
                if (!begun)
                {
                    return 0f;
                }

                var past = elapsed - VictoryStages.BlockingSeconds;
                return past < 0f ? 0f : past;
            }
        }

        public VictoryTimeline Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A victory only ever plays forwards.");
            }

            if (!begun || deltaSeconds == 0f)
            {
                return this;
            }

            return new VictoryTimeline(true, elapsed + deltaSeconds);
        }

        public VictoryTimeline Broken()
        {
            if (!begun || elapsed >= VictoryStages.BlockingSeconds)
            {
                return this;
            }

            return new VictoryTimeline(true, VictoryStages.BlockingSeconds);
        }

        public bool Equals(VictoryTimeline other)
        {
            return begun == other.begun && elapsed.Equals(other.elapsed);
        }

        public override bool Equals(object obj)
        {
            return obj is VictoryTimeline other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((begun ? 1 : 0) * 397) ^ elapsed.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (!begun)
            {
                return "no victory";
            }

            return string.Concat(
                Stage.ToString(),
                " ",
                StageElapsed.ToString("0.###", CultureInfo.InvariantCulture),
                "s of ",
                StageSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                "s, ",
                BlocksInput ? "holding the controls" : "handing the controls back");
        }
    }
}
