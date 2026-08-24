using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class ArticulationPoints
    {
        public static IReadOnlyList<int> Of(DecisionGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var count = graph.Nodes.Count;
            var discovered = new int[count];
            var lowest = new int[count];
            var cut = new bool[count];
            var time = 0;

            for (var root = 0; root < count; root++)
            {
                if (discovered[root] != 0)
                {
                    continue;
                }

                Walk(graph, root, -1, discovered, lowest, cut, ref time);
            }

            var found = new List<int>();
            for (var nodeId = 0; nodeId < count; nodeId++)
            {
                if (cut[nodeId])
                {
                    found.Add(nodeId);
                }
            }

            return found;
        }

        static void Walk(
            DecisionGraph graph, int nodeId, int parent, int[] discovered, int[] lowest, bool[] cut, ref int time)
        {
            discovered[nodeId] = ++time;
            lowest[nodeId] = discovered[nodeId];
            var children = 0;

            foreach (var neighbour in graph.NeighboursOf(nodeId))
            {
                if (discovered[neighbour] == 0)
                {
                    children++;
                    Walk(graph, neighbour, nodeId, discovered, lowest, cut, ref time);
                    lowest[nodeId] = Math.Min(lowest[nodeId], lowest[neighbour]);
                    if (parent >= 0 && lowest[neighbour] >= discovered[nodeId])
                    {
                        cut[nodeId] = true;
                    }
                }
                else if (neighbour != parent)
                {
                    lowest[nodeId] = Math.Min(lowest[nodeId], discovered[neighbour]);
                }
            }

            if (parent < 0 && children > 1)
            {
                cut[nodeId] = true;
            }
        }
    }
}
