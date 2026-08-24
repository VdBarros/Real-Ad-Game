using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class LayoutMetrics
    {
        LayoutMetrics(
            int tileCount,
            int nodeCount,
            int slotCount,
            int emptyCount,
            int corridorCount,
            int gateSlotCount,
            int pocketSlotCount,
            int bossDepth,
            int offPathSlotCount)
        {
            TileCount = tileCount;
            NodeCount = nodeCount;
            SlotCount = slotCount;
            EmptyCount = emptyCount;
            CorridorCount = corridorCount;
            GateSlotCount = gateSlotCount;
            PocketSlotCount = pocketSlotCount;
            BossDepth = bossDepth;
            OffPathSlotCount = offPathSlotCount;
        }

        public int TileCount { get; }

        public int NodeCount { get; }

        public int SlotCount { get; }

        public int EmptyCount { get; }

        public int CorridorCount { get; }

        public int GateSlotCount { get; }

        public int PocketSlotCount { get; }

        public int BossDepth { get; }

        public int OffPathSlotCount { get; }

        public double GateRatio
        {
            get { return SlotCount == 0 ? 0.0 : (double)GateSlotCount / SlotCount; }
        }

        public static LayoutMetrics Of(LevelGraph graph, TileDistanceMap distanceFromStart)
        {
            var isGate = new bool[graph.Decisions.Nodes.Count];
            foreach (var nodeId in ArticulationPoints.Of(graph.Decisions))
            {
                isGate[nodeId] = true;
            }

            var slots = new List<DecisionNode>();
            var emptyCount = 0;
            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Unassigned)
                {
                    slots.Add(node);
                }
                else if (node.Type == NodeType.Empty)
                {
                    emptyCount++;
                }
            }

            var gateSlotCount = 0;
            var pocketSlotCount = 0;
            DecisionNode deepest = null;

            foreach (var slot in slots)
            {
                if (isGate[slot.Id])
                {
                    gateSlotCount++;
                }

                if (graph.Tiles.Neighbours(slot.Position).Count == 1)
                {
                    pocketSlotCount++;
                }

                if (deepest == null
                    || distanceFromStart.DistanceTo(slot.Position) > distanceFromStart.DistanceTo(deepest.Position))
                {
                    deepest = slot;
                }
            }

            var bossDepth = 0;
            var offPathSlotCount = 0;

            if (deepest != null)
            {
                bossDepth = distanceFromStart.DistanceTo(deepest.Position);
                var distanceFromDeepest = TileDistanceMap.From(graph.Tiles, deepest.Position);

                foreach (var slot in slots)
                {
                    if (slot.Id == deepest.Id)
                    {
                        continue;
                    }

                    var throughSlot = distanceFromStart.DistanceTo(slot.Position)
                        + distanceFromDeepest.DistanceTo(slot.Position);
                    if (throughSlot != bossDepth)
                    {
                        offPathSlotCount++;
                    }
                }
            }

            return new LayoutMetrics(
                graph.Tiles.Tiles.Count,
                graph.Decisions.Nodes.Count,
                slots.Count,
                emptyCount,
                graph.Decisions.Corridors.Count,
                gateSlotCount,
                pocketSlotCount,
                bossDepth,
                offPathSlotCount);
        }
    }
}
