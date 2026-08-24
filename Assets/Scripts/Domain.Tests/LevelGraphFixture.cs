using System;
using System.Collections.Generic;

namespace Game.Domain.Tests
{
    static class LevelGraphFixture
    {
        public const long Seed = 20250824L;

        public const string Preset = "tiny";

        public static LevelGraph TwoFloors()
        {
            return Compose(backwards: false);
        }

        public static LevelGraph TwoFloorsAssembledBackwards()
        {
            return Compose(backwards: true);
        }

        static LevelGraph Compose(bool backwards)
        {
            var builder = new LevelGraphBuilder(Seed, Preset);

            foreach (var tile in Order(Tiles(), backwards))
            {
                builder.AddTile(tile.Position, tile.RegionId);
            }

            foreach (var stair in Order(Stairs(), backwards))
            {
                builder.AddStair(stair.Lower, stair.Upper);
            }

            foreach (var node in Order(Nodes(), backwards))
            {
                builder.AddNode(node.Position, node.Type, node.Value);
            }

            foreach (var corridor in Order(Corridors(), backwards))
            {
                if (backwards)
                {
                    var reversedPath = new List<TilePosition>(corridor.Path);
                    reversedPath.Reverse();
                    builder.Connect(corridor.Second, corridor.First, reversedPath);
                }
                else
                {
                    builder.Connect(corridor.First, corridor.Second, corridor.Path);
                }
            }

            return builder.Build();
        }

        static IEnumerable<T> Order<T>(IReadOnlyList<T> items, bool backwards)
        {
            for (var step = 0; step < items.Count; step++)
            {
                yield return backwards ? items[items.Count - 1 - step] : items[step];
            }
        }

        static IReadOnlyList<Tile> Tiles()
        {
            var tiles = new List<Tile>();
            for (var x = 1; x <= 5; x++)
            {
                tiles.Add(new Tile(At(0, x, 0), regionId: 0));
                tiles.Add(new Tile(At(0, x, 2), regionId: 1));
            }

            tiles.Add(new Tile(At(0, 1, 1), regionId: 0));
            tiles.Add(new Tile(At(0, 5, 1), regionId: 0));
            tiles.Add(new Tile(At(1, 5, 0), regionId: 2));
            tiles.Add(new Tile(At(1, 6, 0), regionId: 2));
            tiles.Add(new Tile(At(1, 6, 1), regionId: 2));
            return tiles;
        }

        static IReadOnlyList<StairLink> Stairs()
        {
            return new[] { new StairLink(At(0, 5, 0)) };
        }

        static IReadOnlyList<PlacedNode> Nodes()
        {
            return new[]
            {
                new PlacedNode(At(0, 1, 0), NodeType.Start, 1),
                new PlacedNode(At(0, 5, 0), NodeType.Empty, 0),
                new PlacedNode(At(0, 1, 2), NodeType.Enemy, 4),
                new PlacedNode(At(0, 5, 2), NodeType.Additive, 12),
                new PlacedNode(At(1, 5, 0), NodeType.Empty, 0),
                new PlacedNode(At(1, 6, 0), NodeType.Multiplier, 3),
                new PlacedNode(At(1, 6, 1), NodeType.Boss, 30)
            };
        }

        static IReadOnlyList<PlacedCorridor> Corridors()
        {
            return new[]
            {
                new PlacedCorridor(At(0, 1, 0), At(0, 5, 0), At(0, 2, 0), At(0, 3, 0), At(0, 4, 0)),
                new PlacedCorridor(At(0, 1, 0), At(0, 1, 2), At(0, 1, 1)),
                new PlacedCorridor(At(0, 5, 0), At(0, 5, 2), At(0, 5, 1)),
                new PlacedCorridor(At(0, 1, 2), At(0, 5, 2), At(0, 2, 2), At(0, 3, 2), At(0, 4, 2)),
                new PlacedCorridor(At(0, 5, 0), At(1, 5, 0)),
                new PlacedCorridor(At(1, 5, 0), At(1, 6, 0)),
                new PlacedCorridor(At(1, 6, 0), At(1, 6, 1))
            };
        }

        static TilePosition At(int floor, int x, int y)
        {
            return new TilePosition(floor, x, y);
        }

        sealed class PlacedNode
        {
            public PlacedNode(TilePosition position, NodeType type, int value)
            {
                Position = position;
                Type = type;
                Value = value;
            }

            public TilePosition Position { get; }

            public NodeType Type { get; }

            public int Value { get; }
        }

        sealed class PlacedCorridor
        {
            public PlacedCorridor(TilePosition first, TilePosition second, params TilePosition[] path)
            {
                First = first;
                Second = second;
                Path = path ?? Array.Empty<TilePosition>();
            }

            public TilePosition First { get; }

            public TilePosition Second { get; }

            public IReadOnlyList<TilePosition> Path { get; }
        }
    }
}
