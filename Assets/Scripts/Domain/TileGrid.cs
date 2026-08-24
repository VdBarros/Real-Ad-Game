using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class TileGrid : IEquatable<TileGrid>
    {
        readonly Dictionary<TilePosition, int> regionByPosition;
        readonly Dictionary<TilePosition, TilePosition> stairPartnerByPosition;
        readonly List<Tile> orderedTiles;
        readonly List<StairLink> orderedStairs;

        public TileGrid(IEnumerable<Tile> tiles, IEnumerable<StairLink> stairs)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (stairs == null)
            {
                throw new ArgumentNullException(nameof(stairs));
            }

            regionByPosition = new Dictionary<TilePosition, int>();
            orderedTiles = new List<Tile>();
            foreach (var tile in tiles)
            {
                if (regionByPosition.ContainsKey(tile.Position))
                {
                    throw new ArgumentException("Tile " + tile.Position + " was added twice.", nameof(tiles));
                }

                regionByPosition.Add(tile.Position, tile.RegionId);
                orderedTiles.Add(tile);
            }

            orderedTiles.Sort(CompareTiles);

            stairPartnerByPosition = new Dictionary<TilePosition, TilePosition>();
            orderedStairs = new List<StairLink>();
            foreach (var stair in stairs)
            {
                Link(stair.Lower, stair.Upper);
                Link(stair.Upper, stair.Lower);
                orderedStairs.Add(stair);
            }

            orderedStairs.Sort(CompareStairs);
        }

        public IReadOnlyList<Tile> Tiles
        {
            get { return orderedTiles; }
        }

        public IReadOnlyList<StairLink> Stairs
        {
            get { return orderedStairs; }
        }

        public bool Contains(TilePosition position)
        {
            return regionByPosition.ContainsKey(position);
        }

        public int RegionOf(TilePosition position)
        {
            int regionId;
            if (!regionByPosition.TryGetValue(position, out regionId))
            {
                throw new ArgumentException("No tile at " + position + ".", nameof(position));
            }

            return regionId;
        }

        public bool AreAdjacent(TilePosition first, TilePosition second)
        {
            if (!Contains(first) || !Contains(second))
            {
                return false;
            }

            if (first.Floor == second.Floor
                && Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) == 1)
            {
                return true;
            }

            TilePosition acrossTheStair;
            return stairPartnerByPosition.TryGetValue(first, out acrossTheStair)
                && acrossTheStair.Equals(second);
        }

        public IReadOnlyList<TilePosition> Neighbours(TilePosition position)
        {
            if (!Contains(position))
            {
                throw new ArgumentException("No tile at " + position + ".", nameof(position));
            }

            var neighbours = new List<TilePosition>(5);
            AddIfPresent(neighbours, new TilePosition(position.Floor, position.X - 1, position.Y));
            AddIfPresent(neighbours, new TilePosition(position.Floor, position.X + 1, position.Y));
            AddIfPresent(neighbours, new TilePosition(position.Floor, position.X, position.Y - 1));
            AddIfPresent(neighbours, new TilePosition(position.Floor, position.X, position.Y + 1));

            TilePosition acrossTheStair;
            if (stairPartnerByPosition.TryGetValue(position, out acrossTheStair))
            {
                AddIfPresent(neighbours, acrossTheStair);
            }

            neighbours.Sort();
            return neighbours;
        }

        void AddIfPresent(List<TilePosition> neighbours, TilePosition candidate)
        {
            if (Contains(candidate))
            {
                neighbours.Add(candidate);
            }
        }

        void Link(TilePosition from, TilePosition to)
        {
            if (!Contains(from))
            {
                throw new ArgumentException("A stair ends at " + from + ", where there is no tile.", "stairs");
            }

            if (stairPartnerByPosition.ContainsKey(from))
            {
                throw new ArgumentException("Tile " + from + " already carries a stair.", "stairs");
            }

            stairPartnerByPosition.Add(from, to);
        }

        public bool Equals(TileGrid other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (orderedTiles.Count != other.orderedTiles.Count || orderedStairs.Count != other.orderedStairs.Count)
            {
                return false;
            }

            for (var index = 0; index < orderedTiles.Count; index++)
            {
                if (!orderedTiles[index].Equals(other.orderedTiles[index]))
                {
                    return false;
                }
            }

            for (var index = 0; index < orderedStairs.Count; index++)
            {
                if (!orderedStairs[index].Equals(other.orderedStairs[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TileGrid);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = orderedTiles.Count;
                foreach (var tile in orderedTiles)
                {
                    hash = (hash * 397) ^ tile.GetHashCode();
                }

                foreach (var stair in orderedStairs)
                {
                    hash = (hash * 397) ^ stair.GetHashCode();
                }

                return hash;
            }
        }

        static int CompareTiles(Tile left, Tile right)
        {
            return left.Position.CompareTo(right.Position);
        }

        static int CompareStairs(StairLink left, StairLink right)
        {
            return left.Lower.CompareTo(right.Lower);
        }
    }
}
