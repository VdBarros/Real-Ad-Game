using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public readonly struct LandmarkSpot : IEquatable<LandmarkSpot>
    {
        public LandmarkSpot(TilePosition tile, TileSide against, LandmarkKind kind)
        {
            Tile = tile;
            Against = against;
            Kind = kind;
        }

        public TilePosition Tile { get; }

        public TileSide Against { get; }

        public LandmarkKind Kind { get; }

        public bool Equals(LandmarkSpot other)
        {
            return Tile.Equals(other.Tile) && Against == other.Against && Kind == other.Kind;
        }

        public override bool Equals(object obj)
        {
            return obj is LandmarkSpot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Tile.GetHashCode();
                hash = (hash * 397) ^ (int)Against;
                hash = (hash * 397) ^ (int)Kind;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(Kind.ToString(), " on ", Tile.ToString(), " against its ", Against.ToString());
        }
    }
}
