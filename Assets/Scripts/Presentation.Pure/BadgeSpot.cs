using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct BadgeSpot : IEquatable<BadgeSpot>
    {
        public BadgeSpot(int nodeId, int elevation, WorldPoint anchor, float width, float height)
        {
            if (!(width > 0f) || float.IsInfinity(width))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), width, "A badge that is on the screen at all takes up a width of it.");
            }

            if (!(height > 0f) || float.IsInfinity(height))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "A badge that is on the screen at all takes up a height of it.");
            }

            NodeId = nodeId;
            Elevation = elevation;
            Anchor = anchor;
            Width = width;
            Height = height;
        }

        public int NodeId { get; }

        public int Elevation { get; }

        public WorldPoint Anchor { get; }

        public float Width { get; }

        public float Height { get; }

        public float Across
        {
            get { return WorldPoint.Dot(Anchor, IsoProjection.CameraRight); }
        }

        public float Up
        {
            get { return WorldPoint.Dot(Anchor, IsoProjection.CameraUp); }
        }

        public float Depth
        {
            get { return WorldPoint.Dot(Anchor, IsoProjection.CameraForward); }
        }

        public static BadgeSpot Of(BadgePart part)
        {
            var size = part.Size;

            return new BadgeSpot(part.NodeId, part.Elevation, part.Position, size.Width, size.Height);
        }

        public BadgeSpot Lifted(float metres)
        {
            var up = IsoProjection.CameraUp;

            return new BadgeSpot(
                NodeId,
                Elevation,
                new WorldPoint(
                    Anchor.X + up.X * metres,
                    Anchor.Y + up.Y * metres,
                    Anchor.Z + up.Z * metres),
                Width,
                Height);
        }

        public BadgeSpot Widened(float width)
        {
            return new BadgeSpot(NodeId, Elevation, Anchor, width, Height);
        }

        public bool Equals(BadgeSpot other)
        {
            return NodeId == other.NodeId
                && Elevation == other.Elevation
                && Anchor.Equals(other.Anchor)
                && Width.Equals(other.Width)
                && Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            return obj is BadgeSpot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = NodeId;
                hash = (hash * 397) ^ Elevation;
                hash = (hash * 397) ^ Anchor.GetHashCode();
                hash = (hash * 397) ^ Width.GetHashCode();
                hash = (hash * 397) ^ Height.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "node ",
                NodeId.ToString(CultureInfo.InvariantCulture),
                " at ",
                Anchor.ToString(),
                ", ",
                Width.ToString("0.###", CultureInfo.InvariantCulture),
                " x ",
                Height.ToString("0.###", CultureInfo.InvariantCulture));
        }
    }
}
