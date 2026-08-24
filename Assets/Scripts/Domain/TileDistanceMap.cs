using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class TileDistanceMap
    {
        readonly Dictionary<TilePosition, int> distanceByPosition;

        TileDistanceMap(TilePosition source, Dictionary<TilePosition, int> distanceByPosition)
        {
            Source = source;
            this.distanceByPosition = distanceByPosition;
        }

        public static TileDistanceMap From(TileGrid grid, TilePosition source)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!grid.Contains(source))
            {
                throw new ArgumentException("No tile at " + source + ".", nameof(source));
            }

            var distances = new Dictionary<TilePosition, int> { { source, 0 } };
            var queue = new List<TilePosition> { source };

            for (var head = 0; head < queue.Count; head++)
            {
                var current = queue[head];
                var next = distances[current] + 1;
                foreach (var neighbour in grid.Neighbours(current))
                {
                    if (distances.ContainsKey(neighbour))
                    {
                        continue;
                    }

                    distances.Add(neighbour, next);
                    queue.Add(neighbour);
                }
            }

            return new TileDistanceMap(source, distances);
        }

        public TilePosition Source { get; }

        public int ReachedCount
        {
            get { return distanceByPosition.Count; }
        }

        public int DistanceTo(TilePosition position)
        {
            int distance;
            if (!distanceByPosition.TryGetValue(position, out distance))
            {
                throw new ArgumentException(
                    "No walk runs from " + Source + " to " + position + ".", nameof(position));
            }

            return distance;
        }
    }
}
