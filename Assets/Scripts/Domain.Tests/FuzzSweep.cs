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
            var spreads = new List<double>();
            foreach (var level in accepted)
            {
                foreach (var region in level.Level.Envelope.Regions)
                {
                    spreads.Add(region.Spread);
                }
            }

            spreads.Sort();
            return "p10 " + SweepStatistics.Round(SweepStatistics.Percentile(spreads, 0.1))
                + " p50 " + SweepStatistics.Round(SweepStatistics.Percentile(spreads, 0.5))
                + " p90 " + SweepStatistics.Round(SweepStatistics.Percentile(spreads, 0.9))
                + " over " + spreads.Count + " regions";
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
