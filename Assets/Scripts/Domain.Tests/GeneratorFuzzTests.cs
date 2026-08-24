using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class GeneratorFuzzTests
    {
        const int TinySeeds = 5000;

        const int ShipSeeds = 5000;

        const int MutatedLevels = 400;

        const int ShipOracleSample = 6;

        const double RejectionBar = 0.9;

        const double MissBar = 0.0025;

        static readonly int[] Inflations = { 3, 10, 50 };

        static readonly Dictionary<string, FuzzSweep> SweepByPreset = new Dictionary<string, FuzzSweep>();

        static MutationSweep mutants;

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void TheValidatorClearsEveryAcceptedLevel(MazePreset preset)
        {
            foreach (var accepted in Sweep(preset).Accepted)
            {
                Assert.That(
                    accepted.Verdict.IsSafe,
                    Is.True,
                    "Seed " + accepted.Level.AttemptSeed + ": " + accepted.Verdict);
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoPolicyOnThePanelStrandsAnAcceptedLevel(MazePreset preset)
        {
            foreach (var accepted in Sweep(preset).Accepted)
            {
                foreach (var policy in AdversaryPanel.Policies)
                {
                    Assert.That(
                        AdversaryPanel.Walk(accepted.Level.Graph, accepted.Level.Tuning, policy),
                        Is.Null,
                        "Seed " + accepted.Level.AttemptSeed + " strands " + policy + ".");
                }
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EveryBossStaysUnderTheBoundInvariantBDerivesItFrom(MazePreset preset)
        {
            foreach (var accepted in Sweep(preset).Accepted)
            {
                var level = accepted.Level;

                Assert.That(
                    (long)level.BossPower,
                    Is.LessThan(PowerBound.Of(level.Graph, level.Tuning)),
                    "Seed " + level.AttemptSeed + " authored a boss no run can reach.");
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EveryBossAsksForTheDetourInvariantCDemands(MazePreset preset)
        {
            foreach (var accepted in Sweep(preset).Accepted)
            {
                Assert.That(
                    accepted.Verdict.BossPower,
                    Is.GreaterThan(accepted.Verdict.BeelinePower),
                    "Seed " + accepted.Level.AttemptSeed + " lets a beeline take the boss.");
            }
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoUnassignedSlotSurvivesOnAnAcceptedLevel(MazePreset preset)
        {
            foreach (var accepted in Sweep(preset).Accepted)
            {
                foreach (var node in accepted.Level.Graph.Decisions.Nodes)
                {
                    Assert.That(
                        node.Type,
                        Is.Not.EqualTo(NodeType.Unassigned),
                        "Seed " + accepted.Level.AttemptSeed + " shipped node #" + node.Id + " unassigned.");
                }
            }
        }

        [Test]
        public void EveryStallThePanelReportsOnAMutantIsOneTheOracleFindsToo()
        {
            var sweep = Mutants();

            Assert.That(sweep.Cases, Is.GreaterThan(0));
            Assert.That(sweep.OracleStalls, Is.GreaterThan(0), "No mutant broke, so nothing was cross-examined.");
            Assert.That(sweep.FalseAlarms, Is.Zero, Disagreement(sweep));
        }

        [Test]
        public void ThePanelMissesNoMoreOfTheOraclesStallsThanTheMeasuredResidual()
        {
            var sweep = Mutants();

            Assert.That((double)sweep.Misses / sweep.Cases, Is.LessThanOrEqualTo(MissBar), Disagreement(sweep));
        }

        [Test]
        public void TheOracleNeverBlowsItsBudgetOnTheSmallestPreset()
        {
            Assert.That(Mutants().Aborts, Is.Zero);
        }

        [Test]
        public void TheRejectionRateOnTheSmallestPresetStaysUnderTheBar()
        {
            var sweep = Sweep(MazePreset.Tiny);

            Assert.That(
                sweep.RejectionRate,
                Is.LessThan(RejectionBar),
                sweep.Preset.Name + " rejected " + sweep.Rejections + " of " + sweep.Attempts + " attempts.");
        }

        [Test]
        public void TheSweepReportsWhatItMeasured()
        {
            foreach (var preset in EveryPreset())
            {
                var sweep = Sweep(preset);
                Console.WriteLine(sweep.ToString());
                Console.WriteLine("  spread P_max/P_min " + Spread(sweep));
            }

            Console.WriteLine(Mutants().ToString());
            Console.WriteLine(ShipOracleReport());
        }

        static string Disagreement(MutationSweep sweep)
        {
            return sweep + Environment.NewLine + string.Join(Environment.NewLine, sweep.Disagreements);
        }

        static FuzzSweep Sweep(MazePreset preset)
        {
            FuzzSweep sweep;
            if (SweepByPreset.TryGetValue(preset.Name, out sweep))
            {
                return sweep;
            }

            var seeds = preset == MazePreset.Ship ? ShipSeeds : TinySeeds;
            sweep = new FuzzSweep(preset, seeds);
            var clock = new Stopwatch();

            for (var seed = 1; seed <= seeds; seed++)
            {
                LevelGenerationReport report;
                clock.Restart();

                try
                {
                    var level = LevelGenerator.Generate(seed, preset, out report);
                    clock.Stop();
                    sweep.Took(level, report, clock.Elapsed.TotalMilliseconds);
                }
                catch (LevelGenerationException exhausted)
                {
                    Assert.Fail("Seed " + seed + " on " + preset.Name + ": " + exhausted.Message);
                }
            }

            SweepByPreset.Add(preset.Name, sweep);
            return sweep;
        }

        static MutationSweep Mutants()
        {
            if (mutants != null)
            {
                return mutants;
            }

            var accepted = Sweep(MazePreset.Tiny).Accepted;
            var count = Math.Min(accepted.Count, MutatedLevels);
            var sweep = new MutationSweep(count);

            for (var index = 0; index < count; index++)
            {
                var level = accepted[index].Level;
                foreach (var node in level.Graph.Decisions.Nodes)
                {
                    if (node.Type != NodeType.Enemy)
                    {
                        continue;
                    }

                    foreach (var factor in Inflations)
                    {
                        var mutant = LevelMutation.WithNodeInflated(level.Graph, node.Id, factor);
                        var oracle = InvariantAOracle.Sweep(mutant, level.Tuning);
                        var panel = AdversaryPanel.FirstStall(mutant, level.Tuning);

                        sweep.Compared(level, node.Id, factor, oracle, panel);
                    }
                }
            }

            mutants = sweep;
            return mutants;
        }

        static string ShipOracleReport()
        {
            var accepted = Sweep(MazePreset.Ship).Accepted;
            var count = Math.Min(accepted.Count, ShipOracleSample);
            var peaks = new List<int>(count);
            var aborts = 0;
            var clock = new Stopwatch();
            clock.Start();

            for (var index = 0; index < count; index++)
            {
                var level = accepted[index].Level;
                var verdict = InvariantAOracle.Sweep(level.Graph, level.Tuning);
                peaks.Add(verdict.PeakStates);
                if (verdict.Aborted)
                {
                    aborts++;
                }
            }

            clock.Stop();
            peaks.Sort();

            return "ship oracle sample: " + count + " levels, " + aborts + " blew a "
                + InvariantAOracle.DefaultStateBudget + "-state budget, peak states p50 "
                + Percentile(peaks, 0.5) + " max " + peaks[peaks.Count - 1] + ", "
                + Round(clock.Elapsed.TotalMilliseconds / count) + " ms each";
        }

        static string Spread(FuzzSweep sweep)
        {
            var spreads = new List<double>();
            foreach (var accepted in sweep.Accepted)
            {
                foreach (var region in accepted.Level.Envelope.Regions)
                {
                    spreads.Add(region.Spread);
                }
            }

            spreads.Sort();
            return "p10 " + Round(Percentile(spreads, 0.1))
                + " p50 " + Round(Percentile(spreads, 0.5))
                + " p90 " + Round(Percentile(spreads, 0.9))
                + " over " + spreads.Count + " regions";
        }

        static T Percentile<T>(List<T> sorted, double share)
        {
            return sorted[(int)(share * (sorted.Count - 1))];
        }

        static string Round(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

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

            public override string ToString()
            {
                var description = Preset.Name + ": " + Seeds + " seeds, " + Attempts + " attempts, "
                    + Rejections + " rejected (" + Round(100.0 * RejectionRate) + "%), "
                    + Round(milliseconds / Seeds) + " ms mean";

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

        sealed class MutationSweep
        {
            readonly List<int> peaks = new List<int>();
            readonly List<string> disagreements = new List<string>();

            public MutationSweep(int levels)
            {
                Levels = levels;
            }

            public int Levels { get; }

            public int Cases { get; private set; }

            public int OracleStalls { get; private set; }

            public int PanelStalls { get; private set; }

            public int Misses { get; private set; }

            public int FalseAlarms { get; private set; }

            public int Aborts { get; private set; }

            public IReadOnlyList<string> Disagreements
            {
                get { return disagreements; }
            }

            public void Compared(PlacedLevel level, int nodeId, int factor, OracleVerdict oracle, StallReport panel)
            {
                Cases++;
                peaks.Add(oracle.PeakStates);

                if (oracle.Aborted)
                {
                    Aborts++;
                    return;
                }

                if (oracle.Stalled)
                {
                    OracleStalls++;
                }

                if (panel != null)
                {
                    PanelStalls++;
                }

                if (oracle.Stalled == (panel != null))
                {
                    return;
                }

                if (oracle.Stalled)
                {
                    Misses++;
                }
                else
                {
                    FalseAlarms++;
                }

                disagreements.Add(
                    "Seed " + level.AttemptSeed + ", enemy #" + nodeId + " inflated " + factor
                    + "-fold: oracle says " + (oracle.Stalled ? "stall" : "safe") + ", panel says "
                    + (panel != null ? "stall" : "safe") + " — " + oracle);
            }

            public override string ToString()
            {
                var sorted = new List<int>(peaks);
                sorted.Sort();

                return "tiny mutants: " + Levels + " levels, " + Cases + " cases inflated "
                    + string.Join("/", Array.ConvertAll(Inflations, factor => factor + "-fold"))
                    + ", oracle stalls " + OracleStalls + ", panel stalls " + PanelStalls
                    + ", panel missed " + Misses + ", panel false-alarmed " + FalseAlarms
                    + ", peak states p50 " + Percentile(sorted, 0.5)
                    + " p90 " + Percentile(sorted, 0.9)
                    + " max " + sorted[sorted.Count - 1];
            }
        }
    }
}
