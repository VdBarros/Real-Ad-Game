using System.Collections.Generic;

namespace Game.Domain
{
    sealed class TileTopology
    {
        readonly List<TilePosition> ordered;
        readonly Dictionary<TilePosition, int> indexByPosition;
        readonly int[][] neighbours;

        public TileTopology(TileGrid geometry)
        {
            ordered = new List<TilePosition>(geometry.Tiles.Count);
            foreach (var tile in geometry.Tiles)
            {
                ordered.Add(tile.Position);
            }

            ordered.Sort();

            indexByPosition = new Dictionary<TilePosition, int>(ordered.Count);
            for (var tile = 0; tile < ordered.Count; tile++)
            {
                indexByPosition.Add(ordered[tile], tile);
            }

            neighbours = new int[ordered.Count][];
            Stairs = new bool[ordered.Count];
            for (var tile = 0; tile < ordered.Count; tile++)
            {
                var adjacent = geometry.Neighbours(ordered[tile]);
                var mapped = new int[adjacent.Count];
                for (var step = 0; step < adjacent.Count; step++)
                {
                    mapped[step] = indexByPosition[adjacent[step]];
                }

                neighbours[tile] = mapped;
                Stairs[tile] = geometry.CarriesStair(ordered[tile]);
            }
        }

        public int Count
        {
            get { return ordered.Count; }
        }

        public int[][] Neighbours
        {
            get { return neighbours; }
        }

        public bool[] Stairs { get; }

        public TilePosition this[int tile]
        {
            get { return ordered[tile]; }
        }

        public int Of(TilePosition position)
        {
            return indexByPosition[position];
        }

        public int Degree(int tile)
        {
            return neighbours[tile].Length;
        }

        public int ElevationOf(int tile)
        {
            return ordered[tile].Elevation;
        }

        public int ReachedFrom(int source)
        {
            var seen = new bool[ordered.Count];
            var queue = new List<int> { source };
            seen[source] = true;

            for (var head = 0; head < queue.Count; head++)
            {
                foreach (var neighbour in neighbours[queue[head]])
                {
                    if (seen[neighbour])
                    {
                        continue;
                    }

                    seen[neighbour] = true;
                    queue.Add(neighbour);
                }
            }

            return queue.Count;
        }

        public List<int> TilesAtElevation(int elevation)
        {
            var onThisTerrace = new List<int>();
            for (var tile = 0; tile < ordered.Count; tile++)
            {
                if (ordered[tile].Elevation == elevation)
                {
                    onThisTerrace.Add(tile);
                }
            }

            return onThisTerrace;
        }
    }
}
