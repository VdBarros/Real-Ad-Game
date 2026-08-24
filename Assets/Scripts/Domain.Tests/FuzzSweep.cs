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

        public FuzzSweep(MazePreset preset, int seeds)
        {
            Preset = preset;
            Seeds = seeds;
            accepted = new List<AcceptedLevel>(seeds);
        }

        public MazePreset Preset { get; }

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

        public override string ToString()
        {
            var description = Preset.Name + ": " + Seeds + " seeds, " + Attempts + " attempts, "
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
