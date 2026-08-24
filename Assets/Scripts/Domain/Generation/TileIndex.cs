using System.Collections.Generic;

namespace Game.Domain
{
    sealed class TileIndex
    {
        readonly List<TilePosition> ordered;
        readonly Dictionary<TilePosition, int> indexByPosition;

        public TileIndex(IEnumerable<TilePosition> positions)
        {
            ordered = new List<TilePosition>(positions);
            ordered.Sort();

            indexByPosition = new Dictionary<TilePosition, int>(ordered.Count);
            for (var index = 0; index < ordered.Count; index++)
            {
                indexByPosition.Add(ordered[index], index);
            }
        }

        public IReadOnlyList<TilePosition> Ordered
        {
            get { return ordered; }
        }

        public int Count
        {
            get { return ordered.Count; }
        }

        public TilePosition this[int index]
        {
            get { return ordered[index]; }
        }

        public bool Contains(TilePosition position)
        {
            return indexByPosition.ContainsKey(position);
        }

        public int Of(TilePosition position)
        {
            return indexByPosition[position];
        }
    }
}
