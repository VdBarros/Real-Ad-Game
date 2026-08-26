using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class TileSides
    {
        static readonly TileSide[] SweepOrder = { TileSide.North, TileSide.East, TileSide.South, TileSide.West };

        public static IReadOnlyList<TileSide> All
        {
            get { return SweepOrder; }
        }

        public static TilePosition Step(TilePosition position, TileSide side)
        {
            switch (side)
            {
                case TileSide.North:
                    return new TilePosition(position.Elevation, position.X, position.Y + 1);
                case TileSide.East:
                    return new TilePosition(position.Elevation, position.X + 1, position.Y);
                case TileSide.South:
                    return new TilePosition(position.Elevation, position.X, position.Y - 1);
                case TileSide.West:
                    return new TilePosition(position.Elevation, position.X - 1, position.Y);
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "A tile has four sides.");
            }
        }

        public static float InwardYaw(TileSide side)
        {
            switch (side)
            {
                case TileSide.North:
                    return 0f;
                case TileSide.East:
                    return 90f;
                case TileSide.South:
                    return 180f;
                case TileSide.West:
                    return 270f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "A tile has four sides.");
            }
        }
    }
}
