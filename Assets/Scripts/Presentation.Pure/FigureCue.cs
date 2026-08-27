using System;

namespace Game.Presentation.Pure
{
    public readonly struct FigureCue : IEquatable<FigureCue>
    {
        readonly FigureAct act;
        readonly float beat;

        FigureCue(FigureAct act, float beat)
        {
            this.act = act;
            this.beat = beat;
        }

        public static FigureCue Still
        {
            get { return default(FigureCue); }
        }

        public static FigureCue Walking
        {
            get { return new FigureCue(FigureAct.Walk, 0f); }
        }

        public static FigureCue Looping(FigureAct looped)
        {
            return new FigureCue(looped, 0f);
        }

        public static FigureCue Within(FigureAct played, float seconds)
        {
            if (seconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "A cue cut to a beat needs a beat longer than nothing.");
            }

            return new FigureCue(played, seconds);
        }

        public FigureAct Act
        {
            get { return act; }
        }

        public float Beat
        {
            get { return beat; }
        }

        public bool Loops
        {
            get { return beat <= 0f; }
        }

        public string Clip
        {
            get { return AdventurerClips.NameOf(act); }
        }

        public float SpeedIn(float clipSeconds)
        {
            if (clipSeconds <= 0f)
            {
                return 1f;
            }

            if (Loops || clipSeconds <= beat)
            {
                return 1f;
            }

            return clipSeconds / beat;
        }

        public float TimeIn(float clipSeconds, float elapsed)
        {
            if (clipSeconds <= 0f || elapsed <= 0f)
            {
                return 0f;
            }

            if (Loops)
            {
                var laps = (float)Math.Floor(elapsed / clipSeconds);
                return elapsed - laps * clipSeconds;
            }

            var played = elapsed * SpeedIn(clipSeconds);
            return played >= clipSeconds ? clipSeconds : played;
        }

        public bool EndsWithin(float clipSeconds)
        {
            if (Loops)
            {
                return true;
            }

            return Math.Abs(TimeIn(clipSeconds, beat) - clipSeconds) <= 1e-4f;
        }

        public bool Equals(FigureCue other)
        {
            return act == other.act && beat.Equals(other.beat);
        }

        public override bool Equals(object obj)
        {
            return obj is FigureCue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)act * 397) ^ beat.GetHashCode();
            }
        }

        public override string ToString()
        {
            return Loops
                ? act + " on a loop"
                : act + " cut to " + beat.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                    + "s";
        }
    }
}
