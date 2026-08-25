using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class FloorSweep
    {
        public const float SpreadSeconds = 0.28f;

        public const float FadeSeconds = 0.32f;

        public const float Seconds = SpreadSeconds + FadeSeconds;

        public static IReadOnlyList<int> Ranks(
            TileGrid grid, IReadOnlyList<TilePosition> flipping, FloorReading before)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (flipping == null)
            {
                throw new ArgumentNullException(nameof(flipping));
            }

            if (before == null)
            {
                throw new ArgumentNullException(nameof(before));
            }

            var rankByPosition = new Dictionary<TilePosition, int>(flipping.Count);
            var pending = new HashSet<TilePosition>(flipping);
            var wave = new List<TilePosition>();

            foreach (var position in flipping)
            {
                if (!Touches(grid, before, position))
                {
                    continue;
                }

                pending.Remove(position);
                rankByPosition.Add(position, 0);
                wave.Add(position);
            }

            for (var rank = 1; wave.Count > 0; rank++)
            {
                var next = new List<TilePosition>();

                foreach (var position in wave)
                {
                    foreach (var neighbour in grid.Neighbours(position))
                    {
                        if (!pending.Remove(neighbour))
                        {
                            continue;
                        }

                        rankByPosition.Add(neighbour, rank);
                        next.Add(neighbour);
                    }
                }

                wave = next;
            }

            var ranks = new List<int>(flipping.Count);
            foreach (var position in flipping)
            {
                int rank;
                ranks.Add(rankByPosition.TryGetValue(position, out rank) ? rank : 0);
            }

            return ranks;
        }

        public static float Blend(int rank, int deepestRank, float elapsed)
        {
            if (elapsed >= Seconds)
            {
                return 1f;
            }

            if (elapsed <= 0f)
            {
                return 0f;
            }

            var start = deepestRank <= 0 ? 0f : SpreadSeconds * ((float)rank / deepestRank);
            var blend = (elapsed - start) / FadeSeconds;

            if (blend <= 0f)
            {
                return 0f;
            }

            return blend >= 1f ? 1f : blend;
        }

        static bool Touches(TileGrid grid, FloorReading before, TilePosition position)
        {
            foreach (var neighbour in grid.Neighbours(position))
            {
                if (before.IsCleared(neighbour))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
