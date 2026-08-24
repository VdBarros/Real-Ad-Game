using System;

namespace Game.Domain
{
    public sealed class LevelGraph : IEquatable<LevelGraph>
    {
        public LevelGraph(long seed, string preset, TileGrid tiles, DecisionGraph decisions)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (decisions == null)
            {
                throw new ArgumentNullException(nameof(decisions));
            }

            Seed = seed;
            Preset = preset;
            Tiles = tiles;
            Decisions = decisions;
        }

        public long Seed { get; }

        public string Preset { get; }

        public TileGrid Tiles { get; }

        public DecisionGraph Decisions { get; }

        public int RegionOf(int nodeId)
        {
            return Tiles.RegionOf(Decisions.Node(nodeId).Position);
        }

        public bool Equals(LevelGraph other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Seed == other.Seed
                && string.Equals(Preset, other.Preset, StringComparison.Ordinal)
                && Tiles.Equals(other.Tiles)
                && Decisions.Equals(other.Decisions);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LevelGraph);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Seed.GetHashCode();
                hash = (hash * 397) ^ Preset.GetHashCode();
                hash = (hash * 397) ^ Tiles.GetHashCode();
                hash = (hash * 397) ^ Decisions.GetHashCode();
                return hash;
            }
        }
    }
}
