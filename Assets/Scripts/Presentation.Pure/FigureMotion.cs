using System;

namespace Game.Presentation.Pure
{
    public readonly struct FigureMotion : IEquatable<FigureMotion>
    {
        readonly FigureCue cue;
        readonly float elapsed;
        readonly FigureCue spent;

        FigureMotion(FigureCue cue, float elapsed, FigureCue spent)
        {
            this.cue = cue;
            this.elapsed = elapsed;
            this.spent = spent;
        }

        public static FigureMotion Still
        {
            get { return default(FigureMotion); }
        }

        public FigureCue Cue
        {
            get { return cue; }
        }

        public FigureAct Act
        {
            get { return cue.Act; }
        }

        public float Elapsed
        {
            get { return elapsed; }
        }

        public bool HasSpentABeat
        {
            get { return !spent.Equals(FigureCue.Still); }
        }

        public FigureMotion Cued(FigureCue wanted)
        {
            if (wanted.Equals(cue) || (HasSpentABeat && wanted.Equals(spent)))
            {
                return this;
            }

            return new FigureMotion(wanted, 0f, FigureCue.Still);
        }

        public FigureMotion Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A figure only ever animates forwards.");
            }

            var moved = elapsed + deltaSeconds;

            if (cue.Loops || moved < cue.Beat)
            {
                return new FigureMotion(cue, moved, spent);
            }

            return new FigureMotion(FigureCue.Still, moved - cue.Beat, cue);
        }

        public float TimeIn(float clipSeconds)
        {
            return cue.TimeIn(clipSeconds, elapsed);
        }

        public float SpeedIn(float clipSeconds)
        {
            return cue.SpeedIn(clipSeconds);
        }

        public bool Equals(FigureMotion other)
        {
            return cue.Equals(other.cue) && elapsed.Equals(other.elapsed) && spent.Equals(other.spent);
        }

        public override bool Equals(object obj)
        {
            return obj is FigureMotion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = cue.GetHashCode();
                hash = (hash * 397) ^ elapsed.GetHashCode();
                hash = (hash * 397) ^ spent.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return cue + " for "
                + elapsed.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "s";
        }
    }
}
