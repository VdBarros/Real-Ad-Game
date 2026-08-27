using System;
using Game.Presentation.Pure;

namespace Game.Domain.Tests
{
    static class StairFixture
    {
        public static TilePosition TileUnder(LevelGraph graph, WorldPart stair)
        {
            foreach (var tile in graph.Tiles.Tiles)
            {
                if (PartNames.Stair(tile.Position) == stair.Name)
                {
                    return tile.Position;
                }
            }

            throw new InvalidOperationException("No tile under " + stair.Name + ".");
        }
    }

    static class FootingFixture
    {
        public static TilePosition TileUnder(LevelGraph graph, WorldPart footing)
        {
            foreach (var tile in graph.Tiles.Tiles)
            {
                if (PartNames.Footing(tile.Position) == footing.Name)
                {
                    return tile.Position;
                }
            }

            throw new InvalidOperationException("No tile under " + footing.Name + ".");
        }
    }
}
