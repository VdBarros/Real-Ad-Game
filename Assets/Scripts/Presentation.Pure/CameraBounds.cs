using System;
using System.Globalization;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct CameraBounds : IEquatable<CameraBounds>
    {
        readonly float lowAcross;
        readonly float highAcross;
        readonly float lowUp;
        readonly float highUp;

        CameraBounds(float lowAcross, float highAcross, float lowUp, float highUp)
        {
            this.lowAcross = lowAcross;
            this.highAcross = highAcross;
            this.lowUp = lowUp;
            this.highUp = highUp;
        }

        public static CameraBounds Around(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var tiles = graph.Tiles.Tiles;
            if (tiles.Count == 0)
            {
                throw new ArgumentException("A level with no tiles has nothing to look at.", nameof(graph));
            }

            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;
            var acrossMargin = IsoProjection.TileEdge * (Math.Abs(right.X) + Math.Abs(right.Z));
            var upMargin = IsoProjection.TileEdge * (Math.Abs(up.X) + Math.Abs(up.Z))
                + IsoProjection.StepHeight * Math.Abs(up.Y);

            var lowAcross = float.MaxValue;
            var highAcross = float.MinValue;
            var lowUp = float.MaxValue;
            var highUp = float.MinValue;

            foreach (var tile in tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var across = WorldPoint.Dot(point, right);
                var upwards = WorldPoint.Dot(point, up);

                lowAcross = Math.Min(lowAcross, across);
                highAcross = Math.Max(highAcross, across);
                lowUp = Math.Min(lowUp, upwards);
                highUp = Math.Max(highUp, upwards);
            }

            return new CameraBounds(
                lowAcross - acrossMargin, highAcross + acrossMargin, lowUp - upMargin, highUp + upMargin);
        }

        public WorldPoint Clamp(WorldPoint target)
        {
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;
            var acrossDrift = Held(WorldPoint.Dot(target, right), lowAcross, highAcross);
            var upDrift = Held(WorldPoint.Dot(target, up), lowUp, highUp);

            if (acrossDrift == 0f && upDrift == 0f)
            {
                return target;
            }

            return new WorldPoint(
                target.X + right.X * acrossDrift + up.X * upDrift,
                target.Y + right.Y * acrossDrift + up.Y * upDrift,
                target.Z + right.Z * acrossDrift + up.Z * upDrift);
        }

        public bool Equals(CameraBounds other)
        {
            return lowAcross.Equals(other.lowAcross)
                && highAcross.Equals(other.highAcross)
                && lowUp.Equals(other.lowUp)
                && highUp.Equals(other.highUp);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraBounds other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = lowAcross.GetHashCode();
                hash = (hash * 397) ^ highAcross.GetHashCode();
                hash = (hash * 397) ^ lowUp.GetHashCode();
                hash = (hash * 397) ^ highUp.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "across ",
                lowAcross.ToString("0.##", CultureInfo.InvariantCulture),
                " to ",
                highAcross.ToString("0.##", CultureInfo.InvariantCulture),
                ", up ",
                lowUp.ToString("0.##", CultureInfo.InvariantCulture),
                " to ",
                highUp.ToString("0.##", CultureInfo.InvariantCulture));
        }

        static float Held(float value, float low, float high)
        {
            if (value < low)
            {
                return low - value;
            }

            return value > high ? high - value : 0f;
        }
    }
}
