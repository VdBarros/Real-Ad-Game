using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class MazeLayout
    {
        public MazeLayout(
            long attemptSeed,
            MazePreset preset,
            LevelGraph graph,
            TileDistanceMap distanceFromStart,
            LayoutMetrics metrics)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (distanceFromStart == null)
            {
                throw new ArgumentNullException(nameof(distanceFromStart));
            }

            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            AttemptSeed = attemptSeed;
            Preset = preset;
            Graph = graph;
            DistanceFromStart = distanceFromStart;
            Metrics = metrics;

            var slots = new List<int>();
            var startNodeId = -1;
            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Unassigned)
                {
                    slots.Add(node.Id);
                }
                else if (node.Type == NodeType.Start)
                {
                    if (startNodeId >= 0)
                    {
                        throw new ArgumentException("A level has one start, but found two.", nameof(graph));
                    }

                    startNodeId = node.Id;
                }
            }

            if (startNodeId < 0)
            {
                throw new ArgumentException("A level has a start, but this one has none.", nameof(graph));
            }

            StartNodeId = startNodeId;
            SlotNodeIds = slots;
        }

        public long AttemptSeed { get; }

        public MazePreset Preset { get; }

        public LevelGraph Graph { get; }

        public TileDistanceMap DistanceFromStart { get; }

        public LayoutMetrics Metrics { get; }

        public int StartNodeId { get; }

        public IReadOnlyList<int> SlotNodeIds { get; }
    }
}
