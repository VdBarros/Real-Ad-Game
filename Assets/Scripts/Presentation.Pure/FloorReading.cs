using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class FloorReading
    {
        public static readonly FloorReading Nothing = new FloorReading(new List<TilePosition>());

        readonly List<TilePosition> cleared;
        readonly HashSet<TilePosition> lookup;

        FloorReading(List<TilePosition> cleared)
        {
            cleared.Sort();
            this.cleared = cleared;
            lookup = new HashSet<TilePosition>(cleared);
        }

        public static FloorReading Of(RunState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var grid = state.Level.Tiles;
            var found = new List<TilePosition> { StartTile(state) };
            var seen = new HashSet<TilePosition>(found);

            for (var head = 0; head < found.Count; head++)
            {
                foreach (var neighbour in grid.Neighbours(found[head]))
                {
                    if (Shut(state, neighbour) || !seen.Add(neighbour))
                    {
                        continue;
                    }

                    found.Add(neighbour);
                }
            }

            return new FloorReading(found);
        }

        public IReadOnlyList<TilePosition> Cleared
        {
            get { return cleared; }
        }

        public bool IsCleared(TilePosition position)
        {
            return lookup.Contains(position);
        }

        public IReadOnlyList<TilePosition> Since(FloorReading earlier)
        {
            if (earlier == null)
            {
                throw new ArgumentNullException(nameof(earlier));
            }

            var flipping = new List<TilePosition>();
            foreach (var position in cleared)
            {
                if (!earlier.IsCleared(position))
                {
                    flipping.Add(position);
                }
            }

            return flipping;
        }

        static bool Shut(RunState state, TilePosition position)
        {
            var node = state.Level.Decisions.NodeAt(position);
            return node != null && state.BlocksPassage(node.Id);
        }

        static TilePosition StartTile(RunState state)
        {
            foreach (var node in state.Level.Decisions.Nodes)
            {
                if (node.Type == NodeType.Start)
                {
                    return node.Position;
                }
            }

            throw new ArgumentException("The floor is read outward from a start, and this level has none.", nameof(state));
        }
    }
}
