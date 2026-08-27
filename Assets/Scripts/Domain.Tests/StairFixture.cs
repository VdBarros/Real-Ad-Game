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
}
