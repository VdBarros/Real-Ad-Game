using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class TileFootings
    {
        public static TileFooting Under(TileGrid tiles, TilePosition position)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            TileSide down;

            if (StandsOneStepAbove(tiles, position, out down))
            {
                return TileFooting.Flight;
            }

            return StaircaseClimb.Climbs(position) ? TileFooting.Plinth : TileFooting.Nothing;
        }

        public static bool StandsOneStepAbove(TileGrid tiles, TilePosition position, out TileSide down)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            foreach (var neighbour in tiles.Neighbours(position))
            {
                if (neighbour.Elevation < position.Elevation)
                {
                    down = TileSides.Between(position, neighbour);
                    return true;
                }
            }

            down = TileSide.North;
            return false;
        }

        public static bool IsLevelGround(TileGrid tiles, TilePosition position)
        {
            return Under(tiles, position) == TileFooting.Plinth;
        }

        public static TileSide AscentOf(TileGrid tiles, TilePosition position)
        {
            TileSide down;

            if (!StandsOneStepAbove(tiles, position, out down))
            {
                throw new ArgumentException(
                    "Tile " + position + " stands above nothing, so no flight is laid under it.",
                    nameof(position));
            }

            return StaircaseClimb.Climbs(position)
                ? StaircaseClimb.AscentOf(tiles, position)
                : TileSides.Opposite(down);
        }
    }
}
