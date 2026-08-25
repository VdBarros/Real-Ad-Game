using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct CameraFraming : IEquatable<CameraFraming>
    {
        public CameraFraming(WorldPoint target, float orthographicSize)
        {
            if (orthographicSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orthographicSize), orthographicSize, "A framing always has a positive size.");
            }

            Target = target;
            OrthographicSize = orthographicSize;
        }

        public WorldPoint Target { get; }

        public float OrthographicSize { get; }

        public WorldPoint Position
        {
            get
            {
                var forward = IsoProjection.CameraForward;
                return new WorldPoint(
                    Target.X - forward.X * IsoProjection.CameraBack,
                    Target.Y - forward.Y * IsoProjection.CameraBack,
                    Target.Z - forward.Z * IsoProjection.CameraBack);
            }
        }

        public static CameraFraming Between(CameraFraming from, CameraFraming to, float t)
        {
            if (t <= 0f)
            {
                return from;
            }

            if (t >= 1f)
            {
                return to;
            }

            return new CameraFraming(
                new WorldPoint(
                    from.Target.X + (to.Target.X - from.Target.X) * t,
                    from.Target.Y + (to.Target.Y - from.Target.Y) * t,
                    from.Target.Z + (to.Target.Z - from.Target.Z) * t),
                from.OrthographicSize + (to.OrthographicSize - from.OrthographicSize) * t);
        }

        public bool Equals(CameraFraming other)
        {
            return Target.Equals(other.Target) && OrthographicSize.Equals(other.OrthographicSize);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraFraming other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Target.GetHashCode() * 397) ^ OrthographicSize.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Concat(
                Target.ToString(),
                " at size ",
                OrthographicSize.ToString("0.###", CultureInfo.InvariantCulture));
        }
    }
}
