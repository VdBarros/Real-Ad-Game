using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class LevelFraming
    {
        public const float OpeningSize = 4.2f;

        public const float CloseUpSize = 4f;

        public static CameraFraming Play(LevelGraph graph)
        {
            return new CameraFraming(Centre(graph), IsoProjection.OrthographicSize);
        }

        public static CameraFraming Opening(LevelGraph graph)
        {
            return new CameraFraming(IsoProjection.Of(Start(graph).Position), OpeningSize);
        }

        public static CameraFraming CloseUp(TilePosition position)
        {
            return new CameraFraming(IsoProjection.Of(position), CloseUpSize);
        }

        public static WorldPoint Centre(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var tiles = graph.Tiles.Tiles;
            if (tiles.Count == 0)
            {
                throw new ArgumentException("A level with no tiles has no centre to frame.", nameof(graph));
            }

            var lowFloor = int.MaxValue;
            var highFloor = int.MinValue;
            var lowX = int.MaxValue;
            var highX = int.MinValue;
            var lowY = int.MaxValue;
            var highY = int.MinValue;

            foreach (var tile in tiles)
            {
                var position = tile.Position;
                lowFloor = Math.Min(lowFloor, position.Floor);
                highFloor = Math.Max(highFloor, position.Floor);
                lowX = Math.Min(lowX, position.X);
                highX = Math.Max(highX, position.X);
                lowY = Math.Min(lowY, position.Y);
                highY = Math.Max(highY, position.Y);
            }

            return new WorldPoint(
                (lowX + highX) * 0.5f * IsoProjection.TileEdge,
                (lowFloor + highFloor) * 0.5f * IsoProjection.FloorHeight,
                (lowY + highY) * 0.5f * IsoProjection.TileEdge);
        }

        static DecisionNode Start(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Start)
                {
                    return node;
                }
            }

            throw new ArgumentException("A level always has one start to open on.", nameof(graph));
        }
    }
}
