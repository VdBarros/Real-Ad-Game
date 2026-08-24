using System;
using System.Collections.Generic;

namespace Game.Domain
{
    sealed class ContentBoard
    {
        readonly LevelGraph source;
        readonly NodeType[] types;
        readonly int[] values;
        readonly bool[] minted;
        readonly int[] regions;
        readonly List<int> regionIds;

        ContentBoard(LevelGraph source)
        {
            this.source = source;

            var decisions = source.Decisions;
            var count = decisions.Nodes.Count;
            types = new NodeType[count];
            values = new int[count];
            minted = new bool[count];
            regions = new int[count];

            var starts = 0;
            var seenRegions = new List<int>();
            foreach (var node in decisions.Nodes)
            {
                types[node.Id] = node.Type;
                values[node.Id] = node.Value;
                minted[node.Id] = node.Type != NodeType.Unassigned;
                regions[node.Id] = source.RegionOf(node.Id);

                if (!seenRegions.Contains(regions[node.Id]))
                {
                    seenRegions.Add(regions[node.Id]);
                }

                if (node.Type != NodeType.Start)
                {
                    continue;
                }

                starts++;
                StartNodeId = node.Id;
            }

            if (starts != 1)
            {
                throw new ArgumentException("A level has exactly one start to walk out of.", nameof(source));
            }

            seenRegions.Sort();
            regionIds = seenRegions;
        }

        public static ContentBoard Of(LevelGraph level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            return new ContentBoard(level);
        }

        public int StartNodeId { get; }

        public int Count
        {
            get { return types.Length; }
        }

        public IReadOnlyList<int> RegionIds
        {
            get { return regionIds; }
        }

        public NodeType TypeOf(int nodeId)
        {
            return types[nodeId];
        }

        public int ValueOf(int nodeId)
        {
            return values[nodeId];
        }

        public int RegionOf(int nodeId)
        {
            return regions[nodeId];
        }

        public bool IsMinted(int nodeId)
        {
            return minted[nodeId];
        }

        public bool IsContent(int nodeId)
        {
            var type = types[nodeId];
            return type == NodeType.Enemy || type == NodeType.Additive || type == NodeType.Multiplier;
        }

        public void SetType(int nodeId, NodeType type)
        {
            types[nodeId] = type;
            minted[nodeId] = false;
            values[nodeId] = 0;
        }

        public void SetValue(int nodeId, int value)
        {
            values[nodeId] = value;
            minted[nodeId] = true;
        }

        public List<int> ReachableFrom(bool[] consumed)
        {
            return Flood(nodeId => IsPassable(nodeId, consumed));
        }

        public bool[] ReachableFlags(bool[] consumed)
        {
            var flags = new bool[types.Length];
            foreach (var nodeId in ReachableFrom(consumed))
            {
                flags[nodeId] = true;
            }

            return flags;
        }

        public bool[] ReachableAround(int impassableNodeId)
        {
            var flags = new bool[types.Length];
            foreach (var nodeId in Flood(nodeId => nodeId != impassableNodeId))
            {
                flags[nodeId] = true;
            }

            return flags;
        }

        public int PowerAfter(int power, int nodeId)
        {
            return types[nodeId] == NodeType.Multiplier ? power * values[nodeId] : power + values[nodeId];
        }

        List<int> Flood(Func<int, bool> passable)
        {
            var seen = new bool[types.Length];
            var order = new List<int> { StartNodeId };
            seen[StartNodeId] = true;

            for (var head = 0; head < order.Count; head++)
            {
                var nodeId = order[head];
                if (nodeId != StartNodeId && !passable(nodeId))
                {
                    continue;
                }

                foreach (var neighbour in source.Decisions.NeighboursOf(nodeId))
                {
                    if (seen[neighbour])
                    {
                        continue;
                    }

                    seen[neighbour] = true;
                    order.Add(neighbour);
                }
            }

            return order;
        }

        public List<int> ShortestRouteTo(int targetNodeId)
        {
            var arrivedFrom = new int[types.Length];
            for (var nodeId = 0; nodeId < arrivedFrom.Length; nodeId++)
            {
                arrivedFrom[nodeId] = -1;
            }

            arrivedFrom[StartNodeId] = StartNodeId;
            var order = new List<int> { StartNodeId };

            for (var head = 0; head < order.Count; head++)
            {
                foreach (var neighbour in source.Decisions.NeighboursOf(order[head]))
                {
                    if (arrivedFrom[neighbour] >= 0)
                    {
                        continue;
                    }

                    arrivedFrom[neighbour] = order[head];
                    order.Add(neighbour);
                }
            }

            if (arrivedFrom[targetNodeId] < 0)
            {
                return null;
            }

            var route = new List<int>();
            for (var step = targetNodeId; step != StartNodeId; step = arrivedFrom[step])
            {
                route.Add(step);
            }

            route.Add(StartNodeId);
            route.Reverse();
            return route;
        }

        public LevelGraph Rebuild()
        {
            var builder = new LevelGraphBuilder(source.Seed, source.Preset);

            foreach (var tile in source.Tiles.Tiles)
            {
                builder.AddTile(tile.Position, tile.RegionId);
            }

            foreach (var stair in source.Tiles.Stairs)
            {
                builder.AddStair(stair.Lower, stair.Upper);
            }

            foreach (var node in source.Decisions.Nodes)
            {
                builder.AddNode(node.Position, types[node.Id], values[node.Id]);
            }

            foreach (var corridor in source.Decisions.Corridors)
            {
                builder.Connect(
                    source.Decisions.Node(corridor.LowNodeId).Position,
                    source.Decisions.Node(corridor.HighNodeId).Position,
                    corridor.TilePath);
            }

            return builder.Build();
        }

        bool IsPassable(int nodeId, bool[] consumed)
        {
            if (consumed[nodeId])
            {
                return true;
            }

            var type = types[nodeId];
            return type != NodeType.Enemy && type != NodeType.Boss && type != NodeType.Unassigned;
        }
    }
}
