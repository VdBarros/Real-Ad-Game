using System.Collections.Generic;

namespace Game.Domain
{
    sealed class CarvedMaze
    {
        public CarvedMaze(
            IReadOnlyList<TilePosition> tiles,
            IReadOnlyList<StairLink> stairs,
            IReadOnlyList<TilePosition> staircaseTiles)
        {
            Tiles = tiles;
            Stairs = stairs;
            StaircaseTiles = staircaseTiles;
        }

        public IReadOnlyList<TilePosition> Tiles { get; }

        public IReadOnlyList<StairLink> Stairs { get; }

        public IReadOnlyList<TilePosition> StaircaseTiles { get; }
    }
}
