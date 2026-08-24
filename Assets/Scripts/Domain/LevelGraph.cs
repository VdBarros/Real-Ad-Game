using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class LevelGraph : IEquatable<LevelGraph>
    {
        const string DecisionsParameter = "decisions";

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
                throw new ArgumentNullException(DecisionsParameter);
            }

            Seed = seed;
            Preset = preset;
            Tiles = tiles;
            Decisions = decisions;

            VerifyNodesStandOnTiles();
            VerifyCorridorPaths();
        }

        void VerifyNodesStandOnTiles()
        {
            foreach (var node in Decisions.Nodes)
            {
                if (!Tiles.Contains(node.Position))
                {
                    throw new ArgumentException(
                        "Node " + node + " sits where there is no tile.", DecisionsParameter);
                }
            }
        }

        void VerifyCorridorPaths()
        {
            var interiorOwner = new Dictionary<TilePosition, Corridor>();
            foreach (var corridor in Decisions.Corridors)
            {
                var previous = Decisions.Node(corridor.LowNodeId).Position;
                foreach (var tile in corridor.TilePath)
                {
                    if (!Tiles.Contains(tile))
                    {
                        throw new ArgumentException(
                            "Corridor " + corridor + " runs over " + tile + ", where there is no tile.",
                            DecisionsParameter);
                    }

                    if (Decisions.NodeAt(tile) != null)
                    {
                        throw new ArgumentException(
                            "Corridor " + corridor + " runs through the node at " + tile
                            + ". A corridor never branches and never passes a decision node.",
                            DecisionsParameter);
                    }

                    if (interiorOwner.ContainsKey(tile))
                    {
                        throw new ArgumentException(
                            "Tile " + tile + " lies in the interior of both " + interiorOwner[tile]
                            + " and " + corridor + ".",
                            DecisionsParameter);
                    }

                    RequireAdjacent(corridor, previous, tile);
                    interiorOwner.Add(tile, corridor);
                    previous = tile;
                }

                RequireAdjacent(corridor, previous, Decisions.Node(corridor.HighNodeId).Position);
            }
        }

        void RequireAdjacent(Corridor corridor, TilePosition previous, TilePosition next)
        {
            if (!Tiles.AreAdjacent(previous, next))
            {
                throw new ArgumentException(
                    "Corridor " + corridor + " is broken between " + previous + " and " + next
                    + ". A corridor's tiles run in order from its low node id to its high one.",
                    DecisionsParameter);
            }
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
