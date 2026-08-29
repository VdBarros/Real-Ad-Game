using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct CameraBounds : IEquatable<CameraBounds>
    {
        readonly IReadOnlyList<Tile> tiles;
        readonly float lowAcross;
        readonly float highAcross;
        readonly float lowUp;
        readonly float highUp;

        CameraBounds(IReadOnlyList<Tile> tiles, float lowAcross, float highAcross, float lowUp, float highUp)
        {
            this.tiles = tiles;
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
                tiles, lowAcross - acrossMargin, highAcross + acrossMargin, lowUp - upMargin, highUp + upMargin);
        }

        public WorldPoint Clamp(WorldPoint target)
        {
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;
            var across = WorldPoint.Dot(target, right);
            var upwards = WorldPoint.Dot(target, up);
            var acrossDrift = Held(across, lowAcross, highAcross);
            var upDrift = Held(upwards, lowUp, highUp);

            float acrossPull;
            float upPull;
            OntoTheNearestTile(across + acrossDrift, upwards + upDrift, out acrossPull, out upPull);

            acrossDrift += acrossPull;
            upDrift += upPull;

            if (acrossDrift == 0f && upDrift == 0f)
            {
                return target;
            }

            return new WorldPoint(
                target.X + right.X * acrossDrift + up.X * upDrift,
                target.Y + right.Y * acrossDrift + up.Y * upDrift,
                target.Z + right.Z * acrossDrift + up.Z * upDrift);
        }

        void OntoTheNearestTile(float across, float upwards, out float acrossPull, out float upPull)
        {
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;
            var halfUp = LevelFraming.PlaySize
                - IsoProjection.TileEdge * 0.5f * (Math.Abs(up.X) + Math.Abs(up.Z));
            var halfAcross = LevelFraming.PlaySize * ScreenFrame.Width / ScreenFrame.Height
                - IsoProjection.TileEdge * 0.5f * (Math.Abs(right.X) + Math.Abs(right.Z));

            acrossPull = 0f;
            upPull = 0f;
            var shortest = float.MaxValue;

            for (var index = 0; index < tiles.Count; index++)
            {
                var point = IsoProjection.Of(tiles[index].Position);
                var sideways = WorldPoint.Dot(point, right);
                var upright = WorldPoint.Dot(point, up);
                var sidewaysPull = Held(across, sideways - halfAcross, sideways + halfAcross);
                var uprightPull = Held(upwards, upright - halfUp, upright + halfUp);
                var reach = sidewaysPull * sidewaysPull + uprightPull * uprightPull;

                if (reach >= shortest)
                {
                    continue;
                }

                shortest = reach;
                acrossPull = sidewaysPull;
                upPull = uprightPull;

                if (reach == 0f)
                {
                    return;
                }
            }
        }

        public bool Equals(CameraBounds other)
        {
            return ReferenceEquals(tiles, other.tiles)
                && lowAcross.Equals(other.lowAcross)
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
                var hash = tiles == null ? 0 : tiles.Count;
                hash = (hash * 397) ^ lowAcross.GetHashCode();
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
