using System.Collections.Generic;

namespace Game.Domain
{
    static class SlotSelector
    {
        public static LayoutRejection Fill(
            long seed, MazePreset preset, LayoutPlan plan, IReadOnlyList<CorridorRun> runs)
        {
            var topology = plan.Topology;

            var deadEnds = new List<int>();
            var junctions = new List<int>();
            for (var tile = 0; tile < topology.Count; tile++)
            {
                if (!plan.IsNode(tile) || tile == plan.StartTile)
                {
                    continue;
                }

                if (topology.Degree(tile) == 1)
                {
                    OfferUnlessItClimbs(plan, deadEnds, tile);
                }
                else
                {
                    OfferUnlessItClimbs(plan, junctions, tile);
                }
            }

            if (deadEnds.Count > preset.ContentSlots)
            {
                return LayoutRejection.PocketOverflow;
            }

            var candidates = new List<int>();
            foreach (var run in runs)
            {
                for (var step = 0; step < run.Path.Count; step += 2)
                {
                    OfferUnlessItClimbs(plan, candidates, run.Path[step]);
                }
            }

            candidates.AddRange(junctions);

            var filled = 0;
            foreach (var tile in deadEnds)
            {
                plan.MakeSlot(tile);
                filled++;
            }

            var pool = StageRandom.ForStage(seed, "slots").Shuffled(candidates);

            for (var regionId = 0; regionId < preset.Regions && filled < preset.ContentSlots; regionId++)
            {
                if (RegionAlreadyHoldsASlot(plan, regionId))
                {
                    continue;
                }

                foreach (var tile in pool)
                {
                    if (plan.IsSlot(tile) || plan.RegionOf(tile) != regionId)
                    {
                        continue;
                    }

                    plan.MakeSlot(tile);
                    filled++;
                    break;
                }
            }

            var silence = SilenceOver(plan);
            var spaced = LongestDeadWalk(plan, silence) <= Pace.DeadWalkBudgetSteps;

            while (filled < preset.ContentSlots)
            {
                var chosen = spaced ? -1 : DeepestInTheSilence(plan, pool, silence);

                if (chosen < 0)
                {
                    chosen = FirstFree(plan, pool);
                }

                if (chosen < 0)
                {
                    break;
                }

                plan.MakeSlot(chosen);
                BreakTheSilence(plan, silence, chosen);
                filled++;

                spaced = spaced || LongestDeadWalk(plan, silence) <= Pace.DeadWalkBudgetSteps;
            }

            return filled < preset.ContentSlots ? LayoutRejection.SlotShortfall : LayoutRejection.None;
        }

        static int FirstFree(LayoutPlan plan, IReadOnlyList<int> pool)
        {
            foreach (var tile in pool)
            {
                if (!plan.IsSlot(tile))
                {
                    return tile;
                }
            }

            return -1;
        }

        static int DeepestInTheSilence(LayoutPlan plan, IReadOnlyList<int> pool, int[] silence)
        {
            var chosen = -1;
            var deepest = 0;

            foreach (var tile in pool)
            {
                if (plan.IsSlot(tile) || silence[tile] <= deepest)
                {
                    continue;
                }

                chosen = tile;
                deepest = silence[tile];
            }

            return chosen;
        }

        static int LongestDeadWalk(LayoutPlan plan, int[] silence)
        {
            var topology = plan.Topology;
            var longest = 0;

            for (var tile = 0; tile < topology.Count; tile++)
            {
                if (silence[tile] < 0)
                {
                    continue;
                }

                foreach (var neighbour in topology.Neighbours[tile])
                {
                    var crossing = silence[tile] + silence[neighbour] + 1;
                    if (silence[neighbour] >= 0 && crossing > longest)
                    {
                        longest = crossing;
                    }
                }
            }

            return longest;
        }

        static int[] SilenceOver(LayoutPlan plan)
        {
            var topology = plan.Topology;
            var silence = new int[topology.Count];
            var queue = new List<int>(topology.Count);

            for (var tile = 0; tile < topology.Count; tile++)
            {
                var sounds = tile == plan.StartTile
                    || plan.IsSlot(tile)
                    || DeadWalk.ClimbsATerrace(topology.ElevationOf(tile));

                silence[tile] = sounds ? 0 : -1;
                if (sounds)
                {
                    queue.Add(tile);
                }
            }

            Flood(topology, silence, queue);
            return silence;
        }

        static void BreakTheSilence(LayoutPlan plan, int[] silence, int tile)
        {
            silence[tile] = 0;
            Flood(plan.Topology, silence, new List<int> { tile });
        }

        static void Flood(TileTopology topology, int[] silence, List<int> queue)
        {
            for (var head = 0; head < queue.Count; head++)
            {
                var reached = silence[queue[head]] + 1;
                foreach (var neighbour in topology.Neighbours[queue[head]])
                {
                    if (silence[neighbour] >= 0 && silence[neighbour] <= reached)
                    {
                        continue;
                    }

                    silence[neighbour] = reached;
                    queue.Add(neighbour);
                }
            }
        }

        static void OfferUnlessItClimbs(LayoutPlan plan, List<int> candidates, int tile)
        {
            if (plan.IsStaircase(tile))
            {
                return;
            }

            candidates.Add(tile);
        }

        static bool RegionAlreadyHoldsASlot(LayoutPlan plan, int regionId)
        {
            for (var tile = 0; tile < plan.Topology.Count; tile++)
            {
                if (plan.IsSlot(tile) && plan.RegionOf(tile) == regionId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
