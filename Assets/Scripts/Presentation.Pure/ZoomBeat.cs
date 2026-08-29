using System;

namespace Game.Presentation.Pure
{
    public readonly struct ZoomBeat : IEquatable<ZoomBeat>
    {
        public const float Punch = 1.18f;

        public const float InSeconds = 0.15f;

        public const float OutSeconds = 0.4f;

        public const float FloorSeconds = 0.35f;

        public const float CapSeconds = 1.2f;

        const float Loose = -1f;

        readonly WorldPoint anchor;
        readonly float elapsed;
        readonly float releasedAt;
        readonly bool live;

        ZoomBeat(WorldPoint anchor, float elapsed, float releasedAt, bool live)
        {
            this.anchor = anchor;
            this.elapsed = elapsed;
            this.releasedAt = releasedAt;
            this.live = live;
        }

        public static ZoomBeat None
        {
            get { return default(ZoomBeat); }
        }

        public static ZoomBeat On(WorldPoint anchor)
        {
            return new ZoomBeat(anchor, 0f, Loose, true);
        }

        public WorldPoint Anchor
        {
            get { return anchor; }
        }

        public float ReturnsAt
        {
            get
            {
                var held = releasedAt < 0f ? CapSeconds : Math.Max(FloorSeconds, releasedAt);
                return Math.Min(CapSeconds, held);
            }
        }

        public float Amount
        {
            get
            {
                if (!live)
                {
                    return 0f;
                }

                if (elapsed < InSeconds)
                {
                    return EasedOut(elapsed / InSeconds);
                }

                var returns = ReturnsAt;
                if (elapsed < returns)
                {
                    return 1f;
                }

                var back = (elapsed - returns) / OutSeconds;

                return back >= 1f ? 0f : 1f - EasedOut(back);
            }
        }

        public bool IsGripping
        {
            get { return live && elapsed < ReturnsAt; }
        }

        public bool IsSettled
        {
            get { return !live || elapsed >= ReturnsAt + OutSeconds; }
        }

        public CameraFraming Over(CameraFraming basis)
        {
            if (!live)
            {
                return basis;
            }

            var punched = new CameraFraming(anchor, basis.OrthographicSize / Punch);

            return CameraFraming.Between(basis, punched, Amount);
        }

        public ZoomBeat Released()
        {
            if (!live || releasedAt >= 0f)
            {
                return this;
            }

            return new ZoomBeat(anchor, elapsed, elapsed, true);
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

            return new ZoomBeat(anchor, elapsed + deltaSeconds, releasedAt, true);
        }

        public bool Equals(ZoomBeat other)
        {
            return anchor.Equals(other.anchor)
                && elapsed.Equals(other.elapsed)
                && releasedAt.Equals(other.releasedAt)
                && live == other.live;
        }

        public override bool Equals(object obj)
        {
            return obj is ZoomBeat other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = anchor.GetHashCode();
                hash = (hash * 397) ^ elapsed.GetHashCode();
                hash = (hash * 397) ^ releasedAt.GetHashCode();
                hash = (hash * 397) ^ live.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (!live)
            {
                return "no beat";
            }

            if (IsSettled)
            {
                return "eased back off " + anchor;
            }

            return string.Concat(
                IsGripping ? "punching in on " : "easing back off ",
                anchor.ToString(),
                " at ",
                Amount.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }

        static float EasedOut(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            var left = 1f - t;

            return 1f - left * left;
        }
    }
}
