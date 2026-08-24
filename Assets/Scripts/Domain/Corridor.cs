using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Domain
{
    public sealed class Corridor : IEquatable<Corridor>
    {
        readonly List<TilePosition> tilePath;

        public Corridor(int lowNodeId, int highNodeId, IEnumerable<TilePosition> tilePath)
        {
            if (lowNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lowNodeId), lowNodeId, "Corridors join dense node ids.");
            }

            if (highNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(highNodeId), highNodeId, "Corridors join dense node ids.");
            }

            if (lowNodeId >= highNodeId)
            {
                throw new ArgumentException(
                    "A corridor runs from the lower node id to the higher one, but got "
                    + lowNodeId + " and " + highNodeId + ".",
                    nameof(lowNodeId));
            }

            LowNodeId = lowNodeId;
            HighNodeId = highNodeId;
            this.tilePath = tilePath == null ? new List<TilePosition>() : new List<TilePosition>(tilePath);
        }

        public int LowNodeId { get; }

        public int HighNodeId { get; }

        public IReadOnlyList<TilePosition> TilePath
        {
            get { return tilePath; }
        }

        public bool Equals(Corridor other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (LowNodeId != other.LowNodeId || HighNodeId != other.HighNodeId)
            {
                return false;
            }

            if (tilePath.Count != other.tilePath.Count)
            {
                return false;
            }

            for (var index = 0; index < tilePath.Count; index++)
            {
                if (!tilePath[index].Equals(other.tilePath[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Corridor);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = LowNodeId;
                hash = (hash * 397) ^ HighNodeId;
                foreach (var tile in tilePath)
                {
                    hash = (hash * 397) ^ tile.GetHashCode();
                }

                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                LowNodeId.ToString(CultureInfo.InvariantCulture),
                "-",
                HighNodeId.ToString(CultureInfo.InvariantCulture),
                " over ",
                tilePath.Count.ToString(CultureInfo.InvariantCulture),
                " tiles");
        }
    }
}
