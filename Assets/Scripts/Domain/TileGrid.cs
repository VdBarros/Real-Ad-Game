using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class TileGrid : IEquatable<TileGrid>
    {
        readonly Dictionary<TilePosition, int> regionByPosition;
        readonly Dictionary<long, TilePosition> tileByPlace;
        readonly List<Tile> orderedTiles;

        public TileGrid(IEnumerable<Tile> tiles)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            regionByPosition = new Dictionary<TilePosition, int>();
            tileByPlace = new Dictionary<long, TilePosition>();
            orderedTiles = new List<Tile>();
            foreach (var tile in tiles)
            {
                if (regionByPosition.ContainsKey(tile.Position))
                {
                    throw new ArgumentException("Tile " + tile.Position + " was added twice.", nameof(tiles));
                }

                TilePosition standingThere;
                if (tileByPlace.TryGetValue(PlaceOf(tile.Position.X, tile.Position.Y), out standingThere))
                {
                    throw new ArgumentException(
                        "Tile " + tile.Position + " stands in the same place as " + standingThere
                        + ", so one of them would be hidden under the other.",
                        nameof(tiles));
                }

                regionByPosition.Add(tile.Position, tile.RegionId);
                tileByPlace.Add(PlaceOf(tile.Position.X, tile.Position.Y), tile.Position);
                orderedTiles.Add(tile);
            }

            orderedTiles.Sort(CompareTiles);
        }

        public IReadOnlyList<Tile> Tiles
        {
            get { return orderedTiles; }
        }

        public bool Contains(TilePosition position)
        {
            return regionByPosition.ContainsKey(position);
        }

        public bool ContainsPlace(int x, int y)
        {
            return tileByPlace.ContainsKey(PlaceOf(x, y));
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
            return Contains(first)
                && Contains(second)
                && Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) == 1;
        }

        public IReadOnlyList<TilePosition> Neighbours(TilePosition position)
        {
            if (!Contains(position))
            {
                throw new ArgumentException("No tile at " + position + ".", nameof(position));
            }

            var neighbours = new List<TilePosition>(4);
            AddIfPresent(neighbours, position.X - 1, position.Y);
            AddIfPresent(neighbours, position.X + 1, position.Y);
            AddIfPresent(neighbours, position.X, position.Y - 1);
            AddIfPresent(neighbours, position.X, position.Y + 1);

            neighbours.Sort();
            return neighbours;
        }

        void AddIfPresent(List<TilePosition> neighbours, int x, int y)
        {
            TilePosition standingThere;
            if (tileByPlace.TryGetValue(PlaceOf(x, y), out standingThere))
            {
                neighbours.Add(standingThere);
            }
        }

        public bool Equals(TileGrid other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (orderedTiles.Count != other.orderedTiles.Count)
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

                return hash;
            }
        }

        static long PlaceOf(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }

        static int CompareTiles(Tile left, Tile right)
        {
            return left.Position.CompareTo(right.Position);
        }
    }
}
