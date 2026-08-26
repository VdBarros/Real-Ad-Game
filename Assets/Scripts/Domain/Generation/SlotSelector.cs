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
                    deadEnds.Add(tile);
                }
                else
                {
                    junctions.Add(tile);
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
                    if (plan.IsStaircase(run.Path[step]))
                    {
                        continue;
                    }

                    candidates.Add(run.Path[step]);
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

            foreach (var tile in pool)
            {
                if (filled >= preset.ContentSlots)
                {
                    break;
                }

                if (plan.IsSlot(tile))
                {
                    continue;
                }

                plan.MakeSlot(tile);
                filled++;
            }

            return filled < preset.ContentSlots ? LayoutRejection.SlotShortfall : LayoutRejection.None;
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
