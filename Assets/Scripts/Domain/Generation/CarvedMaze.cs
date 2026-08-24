using System.Collections.Generic;

namespace Game.Domain
{
    sealed class CarvedMaze
    {
        public CarvedMaze(
            IReadOnlyList<TilePosition> tiles,
            IReadOnlyList<StairLink> stairs,
            IReadOnlyList<TilePosition> stairTiles)
        {
            Tiles = tiles;
            Stairs = stairs;
            StairTiles = stairTiles;
        }

        public IReadOnlyList<TilePosition> Tiles { get; }

        public IReadOnlyList<StairLink> Stairs { get; }

        public IReadOnlyList<TilePosition> StairTiles { get; }
    }
}
