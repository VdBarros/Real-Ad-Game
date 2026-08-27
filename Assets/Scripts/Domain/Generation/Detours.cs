using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class Detours
    {
        readonly bool[] flags;
        readonly List<int> nodeIds;

        Detours(bool[] flags, List<int> nodeIds)
        {
            this.flags = flags;
            this.nodeIds = nodeIds;
        }

        public IReadOnlyList<int> NodeIds
        {
            get { return nodeIds; }
        }

        public int Count
        {
            get { return nodeIds.Count; }
        }

        public bool Holds(int nodeId)
        {
            return nodeId >= 0 && nodeId < flags.Length && flags[nodeId];
        }

        public static Detours Of(MazeLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            return Of(layout, DeepestSlotOf(layout));
        }

        public static Detours Of(MazeLayout layout, int bossNodeId)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            return Of(layout.Graph, layout.SlotNodeIds, bossNodeId);
        }

        public static Detours Of(LevelGraph level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var slots = new List<int>();
            var bossNodeId = -1;
            foreach (var node in level.Decisions.Nodes)
            {
                if (node.Type == NodeType.Boss)
                {
                    bossNodeId = node.Id;
                    slots.Add(node.Id);
                }
                else if (node.Type == NodeType.Enemy
                    || node.Type == NodeType.Additive
                    || node.Type == NodeType.Multiplier)
                {
                    slots.Add(node.Id);
                }
            }

            if (bossNodeId < 0)
            {
                throw new ArgumentException("A level with no boss has no route to be off.", nameof(level));
            }

            return Of(level, slots, bossNodeId);
        }

        public static int DeepestSlotOf(MazeLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var deepest = -1;
            var deepestDistance = -1;
            foreach (var slotId in layout.SlotNodeIds)
            {
                var distance = layout.DistanceFromStart.DistanceTo(
                    layout.Graph.Decisions.Node(slotId).Position);
                if (distance <= deepestDistance)
                {
                    continue;
                }

                deepest = slotId;
                deepestDistance = distance;
            }

            return deepest;
        }

        static Detours Of(LevelGraph level, IReadOnlyList<int> slotIds, int bossNodeId)
        {
            var decisions = level.Decisions;
            var count = decisions.Nodes.Count;

            var isSlot = new bool[count];
            foreach (var slotId in slotIds)
            {
                isSlot[slotId] = true;
            }

            var isGate = new bool[count];
            foreach (var nodeId in ArticulationPoints.Of(decisions))
            {
                isGate[nodeId] = true;
            }

            var startNodeId = StartOf(decisions);
            var offTheRoute = OffTheRoute(level, startNodeId, bossNodeId);

            var flags = new bool[count];
            var nodeIds = new List<int>();

            foreach (var slotId in slotIds)
            {
                if (slotId == bossNodeId || !offTheRoute[slotId])
                {
                    continue;
                }

                if (isGate[slotId] && SeparatesASlot(decisions, startNodeId, slotId, isSlot))
                {
                    continue;
                }

                flags[slotId] = true;
                nodeIds.Add(slotId);
            }

            return new Detours(flags, nodeIds);
        }

        static bool[] OffTheRoute(LevelGraph level, int startNodeId, int bossNodeId)
        {
            var decisions = level.Decisions;
            var fromStart = TileDistanceMap.From(level.Tiles, decisions.Node(startNodeId).Position);
            var fromBoss = TileDistanceMap.From(level.Tiles, decisions.Node(bossNodeId).Position);
            var beeline = fromStart.DistanceTo(decisions.Node(bossNodeId).Position);

            var flags = new bool[decisions.Nodes.Count];
            foreach (var node in decisions.Nodes)
            {
                flags[node.Id] = fromStart.DistanceTo(node.Position)
                    + fromBoss.DistanceTo(node.Position) != beeline;
            }

            return flags;
        }

        static bool SeparatesASlot(DecisionGraph decisions, int startNodeId, int nodeId, bool[] isSlot)
        {
            var seen = new bool[decisions.Nodes.Count];
            seen[startNodeId] = true;
            var order = new List<int> { startNodeId };

            for (var head = 0; head < order.Count; head++)
            {
                foreach (var neighbour in decisions.NeighboursOf(order[head]))
                {
                    if (seen[neighbour] || neighbour == nodeId)
                    {
                        continue;
                    }

                    seen[neighbour] = true;
                    order.Add(neighbour);
                }
            }

            for (var other = 0; other < seen.Length; other++)
            {
                if (isSlot[other] && other != nodeId && !seen[other])
                {
                    return true;
                }
            }

            return false;
        }

        static int StartOf(DecisionGraph decisions)
        {
            foreach (var node in decisions.Nodes)
            {
                if (node.Type == NodeType.Start)
                {
                    return node.Id;
                }
            }

            throw new ArgumentException("A level with no start has no route to walk.", nameof(decisions));
        }
    }
}
