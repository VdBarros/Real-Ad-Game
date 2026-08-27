using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class StaircaseClimb
    {
        public static bool Climbs(TilePosition position)
        {
            return !Terraces.IsTerrace(position.Elevation);
        }

        public static TileSide AscentOf(TileGrid tiles, TilePosition position)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (!Climbs(position))
            {
                throw new ArgumentException(
                    "Tile " + position + " stands on a terrace, so it climbs nowhere.", nameof(position));
            }

            TileSide side;

            if (Neighbouring(tiles, position, 1, out side))
            {
                return side;
            }

            if (Neighbouring(tiles, position, -1, out side))
            {
                return TileSides.Opposite(side);
            }

            if (FirstStepAlongTheRun(tiles, position, 1, out side))
            {
                return side;
            }

            if (FirstStepAlongTheRun(tiles, position, -1, out side))
            {
                return TileSides.Opposite(side);
            }

            return TileSide.North;
        }

        public static float YawOf(TileGrid tiles, TilePosition position)
        {
            return TileSides.InwardYaw(AscentOf(tiles, position));
        }

        static bool Neighbouring(TileGrid tiles, TilePosition position, int sign, out TileSide side)
        {
            foreach (var neighbour in tiles.Neighbours(position))
            {
                if (Math.Sign(neighbour.Elevation - position.Elevation) == sign)
                {
                    side = TileSides.Between(position, neighbour);
                    return true;
                }
            }

            side = TileSide.North;
            return false;
        }

        static bool FirstStepAlongTheRun(
            TileGrid tiles, TilePosition position, int sign, out TileSide side)
        {
            var reached = new List<TilePosition> { position };
            var opening = new List<TileSide> { TileSide.North };
            var visited = new HashSet<TilePosition> { position };

            for (var index = 0; index < reached.Count; index++)
            {
                foreach (var neighbour in tiles.Neighbours(reached[index]))
                {
                    if (visited.Contains(neighbour))
                    {
                        continue;
                    }

                    if (neighbour.Elevation != position.Elevation)
                    {
                        if (Math.Sign(neighbour.Elevation - position.Elevation) == sign)
                        {
                            side = opening[index];
                            return true;
                        }

                        continue;
                    }

                    visited.Add(neighbour);
                    reached.Add(neighbour);
                    opening.Add(index == 0 ? TileSides.Between(position, neighbour) : opening[index]);
                }
            }

            side = TileSide.North;
            return false;
        }
    }
}
