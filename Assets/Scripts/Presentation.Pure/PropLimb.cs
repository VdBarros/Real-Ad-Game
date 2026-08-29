using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct PropLimb : IEquatable<PropLimb>
    {
        public PropLimb(WorldPoint size, WorldPoint offset, WorldPoint rotation)
        {
            Size = size;
            Offset = offset;
            Rotation = rotation;
        }

        public WorldPoint Size { get; }

        public WorldPoint Offset { get; }

        public WorldPoint Rotation { get; }

        public WorldPoint Extent
        {
            get
            {
                var x = Radians(Rotation.X);
                var y = Radians(Rotation.Y);
                var z = Radians(Rotation.Z);

                var cx = (float)Math.Cos(x);
                var sx = (float)Math.Sin(x);
                var cy = (float)Math.Cos(y);
                var sy = (float)Math.Sin(y);
                var cz = (float)Math.Cos(z);
                var sz = (float)Math.Sin(z);

                var half = new WorldPoint(Size.X * 0.5f, Size.Y * 0.5f, Size.Z * 0.5f);

                return new WorldPoint(
                    Spread(cy * cz + sy * sx * sz, sy * sx * cz - cy * sz, sy * cx, half),
                    Spread(cx * sz, cx * cz, -sx, half),
                    Spread(cy * sx * sz - sy * cz, sy * sz + cy * sx * cz, cy * cx, half));
            }
        }

        public float Top
        {
            get { return Offset.Y + Extent.Y; }
        }

        public float Foot
        {
            get { return Offset.Y - Extent.Y; }
        }

        public float Reach
        {
            get
            {
                var extent = Extent;
                var right = Math.Abs(Offset.X) + extent.X;
                var forward = Math.Abs(Offset.Z) + extent.Z;

                return right > forward ? right : forward;
            }
        }

        static float Spread(float first, float second, float third, WorldPoint half)
        {
            return Math.Abs(first) * half.X + Math.Abs(second) * half.Y + Math.Abs(third) * half.Z;
        }

        static double Radians(float degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public bool Equals(PropLimb other)
        {
            return Size.Equals(other.Size)
                && Offset.Equals(other.Offset)
                && Rotation.Equals(other.Rotation);
        }

        public override bool Equals(object obj)
        {
            return obj is PropLimb other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Size.GetHashCode();
                hash = (hash * 397) ^ Offset.GetHashCode();
                hash = (hash * 397) ^ Rotation.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                Size.ToString(),
                " at ",
                Offset.ToString(),
                " turned ",
                Rotation.ToString(),
                ", topping out at ",
                Top.ToString("0.###", CultureInfo.InvariantCulture));
        }
    }
}
