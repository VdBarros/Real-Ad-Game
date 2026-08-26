using System;
using System.Collections.Generic;

namespace Game.Domain
{
    static class RegionPainter
    {
        public static void Paint(long seed, MazePreset preset, LayoutPlan plan)
        {
            var topology = plan.Topology;
            var perTerrace = preset.RegionsPerTerrace;

            for (var terrace = 0; terrace < preset.Terraces; terrace++)
            {
                var elevation = Terraces.ElevationOf(terrace);
                var onThisTerrace = topology.TilesAtElevation(elevation);
                var random = StageRandom.ForStage(seed, "regions:" + terrace);
                var sources = FarthestApart(random, topology, onThisTerrace, perTerrace);
                var baseRegionId = terrace * perTerrace;

                var queue = new List<int>();
                for (var source = 0; source < sources.Count; source++)
                {
                    plan.PaintRegion(sources[source], baseRegionId + source);
                    queue.Add(sources[source]);
                }

                for (var head = 0; head < queue.Count; head++)
                {
                    var current = queue[head];
                    foreach (var neighbour in topology.Neighbours[current])
                    {
                        if (topology.ElevationOf(neighbour) != elevation || plan.RegionOf(neighbour) >= 0)
                        {
                            continue;
                        }

                        plan.PaintRegion(neighbour, plan.RegionOf(current));
                        queue.Add(neighbour);
                    }
                }

                foreach (var tile in onThisTerrace)
                {
                    if (plan.RegionOf(tile) < 0)
                    {
                        plan.PaintRegion(tile, baseRegionId);
                    }
                }
            }
        }

        static List<int> FarthestApart(
            StageRandom random, TileTopology topology, IReadOnlyList<int> candidates, int wanted)
        {
            var taken = new bool[topology.Count];
            var sources = new List<int> { random.Pick(candidates) };
            taken[sources[0]] = true;

            while (sources.Count < wanted)
            {
                var best = -1;
                var bestDistance = -1;

                foreach (var candidate in candidates)
                {
                    if (taken[candidate])
                    {
                        continue;
                    }

                    var nearest = int.MaxValue;
                    foreach (var source in sources)
                    {
                        var distance = Manhattan(topology[source], topology[candidate]);
                        if (distance < nearest)
                        {
                            nearest = distance;
                        }
                    }

                    if (nearest > bestDistance)
                    {
                        bestDistance = nearest;
                        best = candidate;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                taken[best] = true;
                sources.Add(best);
            }

            return sources;
        }

        static int Manhattan(TilePosition first, TilePosition second)
        {
            return Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
        }
    }
}
