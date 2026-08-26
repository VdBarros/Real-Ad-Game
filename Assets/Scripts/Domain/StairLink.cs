using System;

namespace Game.Domain
{
    public readonly struct StairLink : IEquatable<StairLink>
    {
        public StairLink(TilePosition lower)
        {
            Lower = lower;
        }

        public TilePosition Lower { get; }

        public TilePosition Upper
        {
            get { return new TilePosition(Lower.Elevation + Terraces.Rise, Lower.X, Lower.Y); }
        }

        public static StairLink Between(TilePosition first, TilePosition second)
        {
            if (first.X != second.X || first.Y != second.Y)
            {
                throw new ArgumentException(
                    "A stair joins the same (x, y) on consecutive terraces, but got " + first + " and " + second + ".");
            }

            if (first.Elevation + Terraces.Rise == second.Elevation)
            {
                return new StairLink(first);
            }

            if (second.Elevation + Terraces.Rise == first.Elevation)
            {
                return new StairLink(second);
            }

            throw new ArgumentException(
                "A stair joins consecutive terraces, but got " + first + " and " + second + ".");
        }

        public bool Equals(StairLink other)
        {
            return Lower.Equals(other.Lower);
        }

        public override bool Equals(object obj)
        {
            return obj is StairLink other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Lower.GetHashCode();
        }

        public override string ToString()
        {
            return Lower + "<->" + Upper;
        }
    }
}
