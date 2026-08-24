using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class LevelGraphBuilder
    {
        readonly long seed;
        readonly string preset;
        readonly List<Tile> tiles = new List<Tile>();
        readonly List<StairLink> stairs = new List<StairLink>();
        readonly List<PendingNode> pendingNodes = new List<PendingNode>();
        readonly List<PendingCorridor> pendingCorridors = new List<PendingCorridor>();

        public LevelGraphBuilder(long seed, string preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            this.seed = seed;
            this.preset = preset;
        }

        public LevelGraphBuilder AddTile(TilePosition position, int regionId)
        {
            tiles.Add(new Tile(position, regionId));
            return this;
        }

        public LevelGraphBuilder AddStair(TilePosition first, TilePosition second)
        {
            stairs.Add(StairLink.Between(first, second));
            return this;
        }

        public LevelGraphBuilder AddNode(TilePosition position, NodeType type, int value = 0)
        {
            pendingNodes.Add(new PendingNode(position, type, value));
            return this;
        }

        public LevelGraphBuilder Connect(TilePosition first, TilePosition second, IEnumerable<TilePosition> tilePath = null)
        {
            pendingCorridors.Add(new PendingCorridor(first, second, tilePath));
            return this;
        }

        public LevelGraph Build()
        {
            var grid = new TileGrid(tiles, stairs);

            var sweep = new List<PendingNode>(pendingNodes);
            sweep.Sort(ComparePendingNodes);

            var nodes = new List<DecisionNode>(sweep.Count);
            for (var id = 0; id < sweep.Count; id++)
            {
                var pending = sweep[id];
                if (!grid.Contains(pending.Position))
                {
                    throw new InvalidOperationException(
                        "Node at " + pending.Position + " sits where there is no tile.");
                }

                if (id > 0 && sweep[id - 1].Position.Equals(pending.Position))
                {
                    throw new InvalidOperationException(
                        "Two nodes were added at " + pending.Position + ".");
                }

                nodes.Add(new DecisionNode(id, pending.Position, pending.Type, pending.Value));
            }

            var idByPosition = new Dictionary<TilePosition, int>(nodes.Count);
            foreach (var node in nodes)
            {
                idByPosition.Add(node.Position, node.Id);
            }

            var corridors = new List<Corridor>(pendingCorridors.Count);
            foreach (var pending in pendingCorridors)
            {
                var firstId = NodeIdAt(idByPosition, pending.First);
                var secondId = NodeIdAt(idByPosition, pending.Second);
                if (firstId == secondId)
                {
                    throw new InvalidOperationException(
                        "A corridor joins two nodes, but both ends sit at " + pending.First + ".");
                }

                var path = new List<TilePosition>(pending.TilePath);
                if (firstId > secondId)
                {
                    path.Reverse();
                }

                corridors.Add(new Corridor(
                    Math.Min(firstId, secondId),
                    Math.Max(firstId, secondId),
                    path));
            }

            corridors.Sort(CompareCorridors);
            VerifyCorridorPaths(grid, nodes, idByPosition, corridors);

            return new LevelGraph(seed, preset, grid, new DecisionGraph(nodes, corridors));
        }

        static void VerifyCorridorPaths(
            TileGrid grid,
            List<DecisionNode> nodes,
            Dictionary<TilePosition, int> idByPosition,
            List<Corridor> corridors)
        {
            var interiorOwner = new Dictionary<TilePosition, Corridor>();
            foreach (var corridor in corridors)
            {
                var previous = nodes[corridor.LowNodeId].Position;
                foreach (var tile in corridor.TilePath)
                {
                    if (!grid.Contains(tile))
                    {
                        throw new InvalidOperationException(
                            "Corridor " + corridor + " runs over " + tile + ", where there is no tile.");
                    }

                    if (idByPosition.ContainsKey(tile))
                    {
                        throw new InvalidOperationException(
                            "Corridor " + corridor + " runs through the node at " + tile
                            + ". A corridor never branches and never passes a decision node.");
                    }

                    if (interiorOwner.ContainsKey(tile))
                    {
                        throw new InvalidOperationException(
                            "Tile " + tile + " lies in the interior of both " + interiorOwner[tile]
                            + " and " + corridor + ".");
                    }

                    RequireAdjacent(grid, corridor, previous, tile);
                    interiorOwner.Add(tile, corridor);
                    previous = tile;
                }

                RequireAdjacent(grid, corridor, previous, nodes[corridor.HighNodeId].Position);
            }
        }

        static void RequireAdjacent(TileGrid grid, Corridor corridor, TilePosition previous, TilePosition next)
        {
            if (!grid.AreAdjacent(previous, next))
            {
                throw new InvalidOperationException(
                    "Corridor " + corridor + " is broken between " + previous + " and " + next + ".");
            }
        }

        static int CompareCorridors(Corridor left, Corridor right)
        {
            var byLow = left.LowNodeId.CompareTo(right.LowNodeId);
            return byLow != 0 ? byLow : left.HighNodeId.CompareTo(right.HighNodeId);
        }

        static int NodeIdAt(Dictionary<TilePosition, int> idByPosition, TilePosition position)
        {
            int id;
            if (!idByPosition.TryGetValue(position, out id))
            {
                throw new InvalidOperationException("A corridor ends at " + position + ", where there is no node.");
            }

            return id;
        }

        static int ComparePendingNodes(PendingNode left, PendingNode right)
        {
            return left.Position.CompareTo(right.Position);
        }

        readonly struct PendingNode
        {
            public PendingNode(TilePosition position, NodeType type, int value)
            {
                Position = position;
                Type = type;
                Value = value;
            }

            public TilePosition Position { get; }

            public NodeType Type { get; }

            public int Value { get; }
        }

        sealed class PendingCorridor
        {
            public PendingCorridor(TilePosition first, TilePosition second, IEnumerable<TilePosition> tilePath)
            {
                First = first;
                Second = second;
                TilePath = tilePath == null ? new List<TilePosition>() : new List<TilePosition>(tilePath);
            }

            public TilePosition First { get; }

            public TilePosition Second { get; }

            public IReadOnlyList<TilePosition> TilePath { get; }
        }
    }
}
