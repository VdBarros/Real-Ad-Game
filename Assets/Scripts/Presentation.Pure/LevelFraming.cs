using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class LevelFraming
    {
        public const float OpeningSize = 4.2f;

        public const float FigureHeightFraction = 0.07f;

        public const float Headroom =
            LevelBlueprintBuilder.BossScale * 2f + BadgeMetrics.Clearance + BadgeMetrics.Height;

        public static float FigureHeight
        {
            get
            {
                return FigureFit.StandingHeight(
                    CharacterCast.MeshOf(PartStyle.Start), LevelBlueprintBuilder.FigureScale);
            }
        }

        public static float PlaySize
        {
            get { return SizeShowing(FigureHeight, FigureHeightFraction); }
        }

        public static float CloseUpSize
        {
            get { return PlaySize / ZoomBeat.Punch; }
        }

        public static float SizeShowing(float height, float shareOfScreen)
        {
            if (height <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Nothing of no height fills a share of the screen.");
            }

            if (shareOfScreen <= 0f || shareOfScreen > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shareOfScreen), shareOfScreen, "A share of the screen runs above nothing and up to all of it.");
            }

            return height / (2f * shareOfScreen);
        }

        public static float HeightShowing(float shareOfScreen, float orthographicSize)
        {
            if (shareOfScreen <= 0f || shareOfScreen > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shareOfScreen), shareOfScreen, "A share of the screen runs above nothing and up to all of it.");
            }

            if (orthographicSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orthographicSize), orthographicSize, "A framing always has a positive size.");
            }

            return shareOfScreen * 2f * orthographicSize;
        }

        public static float ShareOfScreen(float height, float orthographicSize)
        {
            if (orthographicSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orthographicSize), orthographicSize, "A framing always has a positive size.");
            }

            return height / (2f * orthographicSize);
        }

        public static CameraFraming Play(WorldPoint subject)
        {
            return new CameraFraming(subject, PlaySize);
        }

        public static CameraFraming Opening(LevelGraph graph)
        {
            return new CameraFraming(StartPoint(graph), OpeningSize);
        }

        public static CameraFraming CloseUp(TilePosition position)
        {
            return new CameraFraming(IsoProjection.Of(position), CloseUpSize);
        }

        public static CameraFraming Whole(LevelGraph graph)
        {
            var tiles = Tiles(graph);
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;
            var across = IsoProjection.TileEdge * 0.5f * (Math.Abs(right.X) + Math.Abs(right.Z));
            var alongside = IsoProjection.TileEdge * 0.5f * (Math.Abs(up.X) + Math.Abs(up.Z));

            var lowAcross = float.MaxValue;
            var highAcross = float.MinValue;
            var lowUp = float.MaxValue;
            var highUp = float.MinValue;

            foreach (var tile in tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var sideways = WorldPoint.Dot(point, right);
                var upwards = WorldPoint.Dot(point, up);

                lowAcross = Math.Min(lowAcross, sideways - across);
                highAcross = Math.Max(highAcross, sideways + across);
                lowUp = Math.Min(lowUp, upwards - alongside);
                highUp = Math.Max(highUp, upwards + alongside + Headroom * up.Y);
            }

            var centre = Centre(graph);
            var sidewaysDrift = (lowAcross + highAcross) * 0.5f - WorldPoint.Dot(centre, right);
            var upwardsDrift = (lowUp + highUp) * 0.5f - WorldPoint.Dot(centre, up);

            var target = new WorldPoint(
                centre.X + right.X * sidewaysDrift + up.X * upwardsDrift,
                centre.Y + right.Y * sidewaysDrift + up.Y * upwardsDrift,
                centre.Z + right.Z * sidewaysDrift + up.Z * upwardsDrift);

            var halfUp = (highUp - lowUp) * 0.5f;
            var halfAcross = (highAcross - lowAcross) * 0.5f;
            var byWidth = halfAcross * ScreenFrame.Height / ScreenFrame.Width;

            return new CameraFraming(
                target, Math.Max(PlaySize, Math.Max(halfUp, byWidth)));
        }

        public static WorldPoint Centre(LevelGraph graph)
        {
            var tiles = Tiles(graph);

            var lowElevation = int.MaxValue;
            var highElevation = int.MinValue;
            var lowX = int.MaxValue;
            var highX = int.MinValue;
            var lowY = int.MaxValue;
            var highY = int.MinValue;

            foreach (var tile in tiles)
            {
                var position = tile.Position;
                lowElevation = Math.Min(lowElevation, position.Elevation);
                highElevation = Math.Max(highElevation, position.Elevation);
                lowX = Math.Min(lowX, position.X);
                highX = Math.Max(highX, position.X);
                lowY = Math.Min(lowY, position.Y);
                highY = Math.Max(highY, position.Y);
            }

            return new WorldPoint(
                (lowX + highX) * 0.5f * IsoProjection.TileEdge,
                (lowElevation + highElevation) * 0.5f * IsoProjection.StepHeight,
                (lowY + highY) * 0.5f * IsoProjection.TileEdge);
        }

        public static WorldPoint StartPoint(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Start)
                {
                    return IsoProjection.Of(node.Position);
                }
            }

            throw new ArgumentException("A level always has one start to open on.", nameof(graph));
        }

        static IReadOnlyList<Tile> Tiles(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var tiles = graph.Tiles.Tiles;
            if (tiles.Count == 0)
            {
                throw new ArgumentException("A level with no tiles has no frame to fit it.", nameof(graph));
            }

            return tiles;
        }
    }
}
