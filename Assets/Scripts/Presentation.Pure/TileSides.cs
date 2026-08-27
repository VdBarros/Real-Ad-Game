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

        public static WorldPoint Toward(TileSide side)
        {
            switch (side)
            {
                case TileSide.North:
                    return new WorldPoint(0f, 0f, 1f);
                case TileSide.East:
                    return new WorldPoint(1f, 0f, 0f);
                case TileSide.South:
                    return new WorldPoint(0f, 0f, -1f);
                case TileSide.West:
                    return new WorldPoint(-1f, 0f, 0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "A tile has four sides.");
            }
        }

        public static TileSide Opposite(TileSide side)
        {
            switch (side)
            {
                case TileSide.North:
                    return TileSide.South;
                case TileSide.East:
                    return TileSide.West;
                case TileSide.South:
                    return TileSide.North;
                case TileSide.West:
                    return TileSide.East;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "A tile has four sides.");
            }
        }

        public static TileSide OfInwardYaw(float yaw)
        {
            var quarters = ((int)Math.Round(yaw / 90.0) % 4 + 4) % 4;

            switch (quarters)
            {
                case 0:
                    return TileSide.North;
                case 1:
                    return TileSide.East;
                case 2:
                    return TileSide.South;
                default:
                    return TileSide.West;
            }
        }

        public static TileSide Between(TilePosition from, TilePosition to)
        {
            foreach (var side in SweepOrder)
            {
                var step = Step(from, side);
                if (step.X == to.X && step.Y == to.Y)
                {
                    return side;
                }
            }

            throw new ArgumentException(
                "Tile " + to + " does not share a side with " + from + ".", nameof(to));
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
