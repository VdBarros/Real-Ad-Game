using System;

namespace Game.Presentation.Pure
{
    public readonly struct CameraFollow : IEquatable<CameraFollow>
    {
        public const float SettleSeconds = 0.35f;

        public const float PanShare = 0.75f;

        public const float SettledPixels = 0.25f;

        public const float SettledSize = 1e-4f;

        const float Decay = 3f;

        readonly CameraFraming standing;
        readonly WorldPoint subject;

        CameraFollow(CameraFraming standing, WorldPoint subject)
        {
            this.standing = standing;
            this.subject = subject;
        }

        public static CameraFollow From(CameraFraming standing, WorldPoint subject)
        {
            return new CameraFollow(standing, subject);
        }

        public CameraFraming Framing
        {
            get { return standing; }
        }

        public WorldPoint Subject
        {
            get { return subject; }
        }

        public CameraFraming Wanted
        {
            get { return LevelFraming.Play(subject); }
        }

        public bool IsSettled
        {
            get { return standing.Equals(Wanted); }
        }

        public CameraFollow Toward(WorldPoint followed)
        {
            return followed.Equals(subject) ? this : new CameraFollow(standing, followed);
        }

        public CameraFollow Advanced(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "A follow only ever runs forwards.");
            }

            var wanted = Wanted;
            if (standing.Equals(wanted) || deltaSeconds <= 0f)
            {
                return this;
            }

            var apart = ScreenFrame.PanPixels(standing, wanted);
            var offSize = Math.Abs(standing.OrthographicSize - wanted.OrthographicSize);
            if (apart <= SettledPixels && offSize <= SettledSize)
            {
                return new CameraFollow(wanted, subject);
            }

            var eased = 1f - (float)Math.Exp(-Decay * deltaSeconds / SettleSeconds);
            var budget = ScreenFrame.PanCeiling * PanShare * deltaSeconds;
            var taken = apart > budget ? Math.Min(eased, budget / apart) : eased;

            return new CameraFollow(CameraFraming.Between(standing, wanted, taken), subject);
        }

        public bool Equals(CameraFollow other)
        {
            return standing.Equals(other.standing) && subject.Equals(other.subject);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraFollow other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (standing.GetHashCode() * 397) ^ subject.GetHashCode();
            }
        }

        public override string ToString()
        {
            return IsSettled
                ? "following " + subject
                : string.Concat("closing on ", subject.ToString(), " from ", standing.ToString());
        }
    }
}
