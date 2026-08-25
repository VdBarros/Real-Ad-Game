using System;

namespace Game.Presentation.Pure
{
    public readonly struct ZoomBeat : IEquatable<ZoomBeat>
    {
        public const float FloorSeconds = 0.35f;

        public const float CapSeconds = 1.2f;

        readonly CameraFraming subject;
        readonly float elapsed;
        readonly bool live;
        readonly bool released;

        ZoomBeat(CameraFraming subject, float elapsed, bool live, bool released)
        {
            this.subject = subject;
            this.elapsed = elapsed;
            this.live = live;
            this.released = released;
        }

        public static ZoomBeat None
        {
            get { return default(ZoomBeat); }
        }

        public static ZoomBeat On(CameraFraming subject)
        {
            return new ZoomBeat(subject, 0f, true, false);
        }

        public CameraFraming Framing
        {
            get { return subject; }
        }

        public bool IsSettled
        {
            get
            {
                if (!live)
                {
                    return true;
                }

                return elapsed >= CapSeconds || (released && elapsed >= FloorSeconds);
            }
        }

        public ZoomBeat Released()
        {
            if (released || !live)
            {
                return this;
            }

            return new ZoomBeat(subject, elapsed, true, true);
        }

        public ZoomBeat Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A beat only ever runs forwards.");
            }

            if (IsSettled)
            {
                return this;
            }

            return new ZoomBeat(subject, elapsed + deltaSeconds, true, released);
        }

        public bool Equals(ZoomBeat other)
        {
            return subject.Equals(other.subject)
                && elapsed.Equals(other.elapsed)
                && live == other.live
                && released == other.released;
        }

        public override bool Equals(object obj)
        {
            return obj is ZoomBeat other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = subject.GetHashCode();
                hash = (hash * 397) ^ elapsed.GetHashCode();
                hash = (hash * 397) ^ live.GetHashCode();
                hash = (hash * 397) ^ released.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (!live)
            {
                return "no beat";
            }

            return string.Concat(IsSettled ? "cut back from " : "held on ", subject.ToString());
        }
    }
}
