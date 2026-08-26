using System.Collections.Generic;

namespace Game.Domain.Tests
{
    using PlacedCorridor = System.ValueTuple<TilePosition, TilePosition, IReadOnlyList<TilePosition>>;
    using PlacedNode = System.ValueTuple<TilePosition, NodeType, int>;

    static class LevelGraphFixture
    {
        public const long Seed = 20250824L;

        public const string Preset = "tiny";

        public static LevelGraph TwoTerraces()
        {
            return Compose(backwards: false);
        }

        public static LevelGraph TwoTerracesAssembledBackwards()
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
                builder.AddNode(node.Item1, node.Item2, node.Item3);
            }

            foreach (var corridor in Order(Corridors(), backwards))
            {
                if (backwards)
                {
                    var reversedPath = new List<TilePosition>(corridor.Item3);
                    reversedPath.Reverse();
                    builder.Connect(corridor.Item2, corridor.Item1, reversedPath);
                }
                else
                {
                    builder.Connect(corridor.Item1, corridor.Item2, corridor.Item3);
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
            tiles.Add(new Tile(At(2, 5, 0), regionId: 2));
            tiles.Add(new Tile(At(2, 6, 0), regionId: 2));
            tiles.Add(new Tile(At(2, 6, 1), regionId: 2));
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
                Node(At(0, 1, 0), NodeType.Start, 0),
                Node(At(0, 5, 0), NodeType.Empty, 0),
                Node(At(0, 1, 2), NodeType.Enemy, 4),
                Node(At(0, 5, 2), NodeType.Additive, 12),
                Node(At(2, 5, 0), NodeType.Empty, 0),
                Node(At(2, 6, 0), NodeType.Multiplier, 3),
                Node(At(2, 6, 1), NodeType.Boss, 30)
            };
        }

        static IReadOnlyList<PlacedCorridor> Corridors()
        {
            return new[]
            {
                Joined(At(0, 1, 0), At(0, 5, 0), At(0, 2, 0), At(0, 3, 0), At(0, 4, 0)),
                Joined(At(0, 1, 0), At(0, 1, 2), At(0, 1, 1)),
                Joined(At(0, 5, 0), At(0, 5, 2), At(0, 5, 1)),
                Joined(At(0, 1, 2), At(0, 5, 2), At(0, 2, 2), At(0, 3, 2), At(0, 4, 2)),
                Joined(At(0, 5, 0), At(2, 5, 0)),
                Joined(At(2, 5, 0), At(2, 6, 0)),
                Joined(At(2, 6, 0), At(2, 6, 1))
            };
        }

        static PlacedNode Node(TilePosition position, NodeType type, int value)
        {
            return (position, type, value);
        }

        static PlacedCorridor Joined(TilePosition first, TilePosition second, params TilePosition[] path)
        {
            return (first, second, path);
        }

        static TilePosition At(int elevation, int x, int y)
        {
            return new TilePosition(elevation, x, y);
        }

    }
}
