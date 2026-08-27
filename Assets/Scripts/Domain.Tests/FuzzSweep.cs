using System;
using System.Collections.Generic;

namespace Game.Domain.Tests
{
    sealed class AcceptedLevel
    {
        public AcceptedLevel(PlacedLevel level, SolvabilityVerdict verdict)
        {
            Level = level;
            Verdict = verdict;
        }

        public PlacedLevel Level { get; }

        public SolvabilityVerdict Verdict { get; }
    }

    sealed class FuzzSweep
    {
        readonly List<AcceptedLevel> accepted;
        readonly Dictionary<LayoutRejection, int> layoutReasons = new Dictionary<LayoutRejection, int>();
        readonly Dictionary<ContentRejection, int> contentReasons = new Dictionary<ContentRejection, int>();
        double milliseconds;
        List<int> enemyValues;

        public FuzzSweep(string name, LevelPlan plan, int seeds)
        {
            Name = name;
            Plan = plan;
            Seeds = seeds;
            accepted = new List<AcceptedLevel>(seeds);
        }

        public string Name { get; }

        public LevelPlan Plan { get; }

        public MazePreset Preset
        {
            get { return Plan.Preset; }
        }

        public int Seeds { get; }

        public int Attempts { get; private set; }

        public int Rejections { get; private set; }

        public IReadOnlyList<AcceptedLevel> Accepted
        {
            get { return accepted; }
        }

        public double RejectionRate
        {
            get { return Attempts == 0 ? 0.0 : (double)Rejections / Attempts; }
        }

        public void Took(PlacedLevel level, LevelGenerationReport report, double elapsed)
        {
            accepted.Add(new AcceptedLevel(level, SolvabilityValidator.Validate(level.Graph, level.Tuning)));
            Attempts += report.Attempts;
            Rejections += report.Rejections;
            milliseconds += elapsed;

            foreach (LayoutRejection reason in Enum.GetValues(typeof(LayoutRejection)))
            {
                Count(layoutReasons, reason, report.CountOf(reason));
            }

            foreach (ContentRejection reason in Enum.GetValues(typeof(ContentRejection)))
            {
                Count(contentReasons, reason, report.CountOf(reason));
            }
        }

        public string Spread()
        {
            var everywhere = SpreadsEverywhere();
            var chosen = SpreadsARouteChooses();

            return "away from the start " + Percentiles(chosen)
                + "; every region " + Percentiles(everywhere);
        }

        public double SpreadFloorReached()
        {
            var chosen = SpreadsARouteChooses();

            return chosen.Count == 0 ? 0.0 : SweepStatistics.Percentile(chosen, 0.1);
        }

        List<double> SpreadsEverywhere()
        {
            var spreads = new List<double>();
            foreach (var level in accepted)
            {
                foreach (var region in level.Level.Envelope.Regions)
                {
                    spreads.Add(region.Spread);
                }
            }

            spreads.Sort();
            return spreads;
        }

        List<double> SpreadsARouteChooses()
        {
            var spreads = new List<double>();
            foreach (var level in accepted)
            {
                foreach (var region in level.Level.Envelope.Regions)
                {
                    if (!region.HoldsTheStart)
                    {
                        spreads.Add(region.Spread);
                    }
                }
            }

            spreads.Sort();
            return spreads;
        }

        static string Percentiles(List<double> sorted)
        {
            if (sorted.Count == 0)
            {
                return "no regions";
            }

            return "p10 " + SweepStatistics.Round(SweepStatistics.Percentile(sorted, 0.1))
                + " p50 " + SweepStatistics.Round(SweepStatistics.Percentile(sorted, 0.5))
                + " p90 " + SweepStatistics.Round(SweepStatistics.Percentile(sorted, 0.9))
                + " over " + sorted.Count + " regions";
        }

        public string Opening()
        {
            var choices = new List<double>();
            var fewest = double.MaxValue;

            foreach (var level in accepted)
            {
                var count = OpeningFrontier.Of(level.Level.Graph, level.Level.Tuning).Count;
                choices.Add(count);
                fewest = Math.Min(fewest, count);
            }

            choices.Sort();
            return "p50 " + SweepStatistics.Round(SweepStatistics.Percentile(choices, 0.5))
                + " p90 " + SweepStatistics.Round(SweepStatistics.Percentile(choices, 0.9))
                + " affordable enemies, fewest " + SweepStatistics.Round(fewest)
                + ", asked for " + Plan.OpeningChoices;
        }

        public string EnemyNumbers()
        {
            var enemies = EnemyValues();
            if (enemies.Count == 0)
            {
                return "no enemies";
            }

            return "p50 " + SweepStatistics.Percentile(enemies, 0.5)
                + " p90 " + SweepStatistics.Percentile(enemies, 0.9)
                + ", " + SweepStatistics.Round(100.0 * ShareOfEnemiesAtOne())
                + "% at 1, over " + enemies.Count + " enemies";
        }

        public string SpineReach()
        {
            var reach = new List<double>();
            var content = 0;
            var onTheSpine = 0;
            var reaching = 0;

            foreach (var level in accepted)
            {
                var spine = Spine.Of(level.Level.Graph, level.Level.Tuning);
                var slots = 0;
                foreach (var node in level.Level.Graph.Decisions.Nodes)
                {
                    if (node.Type == NodeType.Enemy
                        || node.Type == NodeType.Additive
                        || node.Type == NodeType.Multiplier)
                    {
                        slots++;
                    }
                }

                content += slots;
                onTheSpine += spine.Length;
                reach.Add(slots == 0 ? 0.0 : (double)spine.Length / slots);
                if (spine.ReachesTheBoss)
                {
                    reaching++;
                }
            }

            reach.Sort();
            return "holds " + onTheSpine + " of " + content + " content nodes ("
                + SweepStatistics.Round(100.0 * onTheSpine / content) + "%), p50 share "
                + SweepStatistics.Round(SweepStatistics.Percentile(reach, 0.5))
                + ", " + SweepStatistics.Round(100.0 * reaching / accepted.Count) + "% afford the boss";
        }

        public string Locks()
        {
            var perLevel = new List<double>();
            var holding = 0;
            foreach (var level in accepted)
            {
                var count = Elites.Of(level.Level.Graph, level.Level.Tuning).Count;
                perLevel.Add(count);
                if (count > 0)
                {
                    holding++;
                }
            }

            perLevel.Sort();
            return "p50 " + SweepStatistics.Percentile(perLevel, 0.5)
                + " p90 " + SweepStatistics.Percentile(perLevel, 0.9)
                + " an accepted level, " + SweepStatistics.Round(100.0 * holding / accepted.Count)
                + "% of levels hold one, deepest lock " + DeepestLock();
        }

        public string DeepestLock()
        {
            var depths = new List<double>();
            foreach (var level in accepted)
            {
                depths.Add(DeepestLockOn(level.Level));
            }

            depths.Sort();
            return "p50 " + SweepStatistics.Round(SweepStatistics.Percentile(depths, 0.5))
                + " p90 " + SweepStatistics.Round(SweepStatistics.Percentile(depths, 0.9)) + " times P_min";
        }

        public double MedianDeepestLock()
        {
            var depths = new List<double>();
            foreach (var level in accepted)
            {
                depths.Add(DeepestLockOn(level.Level));
            }

            depths.Sort();
            return SweepStatistics.Percentile(depths, 0.5);
        }

        static double DeepestLockOn(PlacedLevel level)
        {
            var cheapestEntry = new Dictionary<int, int>();
            foreach (var region in level.Envelope.Regions)
            {
                cheapestEntry[region.RegionId] = region.CheapestEntry;
            }

            var deepest = 0.0;
            foreach (var node in level.Graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Enemy)
                {
                    continue;
                }

                int entry;
                if (cheapestEntry.TryGetValue(level.Graph.RegionOf(node.Id), out entry) && entry > 0)
                {
                    deepest = Math.Max(deepest, (double)node.Value / entry);
                }
            }

            return deepest;
        }

        public double ShareOfEnemiesAtOne()
        {
            var enemies = EnemyValues();
            if (enemies.Count == 0)
            {
                return 0.0;
            }

            var ones = 0;
            foreach (var value in enemies)
            {
                if (value == 1)
                {
                    ones++;
                }
            }

            return (double)ones / enemies.Count;
        }

        public override string ToString()
        {
            var description = Name + ": " + Seeds + " seeds, " + Attempts + " attempts, "
                + Rejections + " rejected (" + SweepStatistics.Round(100.0 * RejectionRate)
                + "% of attempts), " + SweepStatistics.Round(milliseconds / Seeds) + " ms mean";

            foreach (var reason in layoutReasons)
            {
                description += ", " + reason.Key + "=" + reason.Value;
            }

            foreach (var reason in contentReasons)
            {
                description += ", " + reason.Key + "=" + reason.Value;
            }

            return description;
        }

        List<int> EnemyValues()
        {
            if (enemyValues != null)
            {
                return enemyValues;
            }

            var values = new List<int>();
            foreach (var level in accepted)
            {
                foreach (var node in level.Level.Graph.Decisions.Nodes)
                {
                    if (node.Type == NodeType.Enemy)
                    {
                        values.Add(node.Value);
                    }
                }
            }

            values.Sort();
            enemyValues = values;
            return enemyValues;
        }

        static void Count<TReason>(Dictionary<TReason, int> counts, TReason reason, int seen)
        {
            if (seen == 0)
            {
                return;
            }

            int running;
            counts.TryGetValue(reason, out running);
            counts[reason] = running + seen;
        }
    }
}
