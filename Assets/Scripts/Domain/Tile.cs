using System;

namespace Game.Domain
{
    public readonly struct Tile : IEquatable<Tile>
    {
        public Tile(TilePosition position, int regionId)
        {
            Position = position;
            RegionId = regionId;
        }

        public TilePosition Position { get; }

        public int RegionId { get; }

        public bool Equals(Tile other)
        {
            return Position.Equals(other.Position) && RegionId == other.RegionId;
        }

        public override bool Equals(object obj)
        {
            return obj is Tile other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ RegionId;
            }
        }

        public override string ToString()
        {
            return Position + " r" + RegionId;
        }
    }
}
