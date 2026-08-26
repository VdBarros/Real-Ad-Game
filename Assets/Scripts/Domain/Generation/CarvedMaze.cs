using System.Collections.Generic;

namespace Game.Domain
{
    sealed class CarvedMaze
    {
        public CarvedMaze(IReadOnlyList<TilePosition> tiles, IReadOnlyList<TilePosition> staircaseTiles)
        {
            Tiles = tiles;
            StaircaseTiles = staircaseTiles;
        }

        public IReadOnlyList<TilePosition> Tiles { get; }

        public IReadOnlyList<TilePosition> StaircaseTiles { get; }
    }
}
