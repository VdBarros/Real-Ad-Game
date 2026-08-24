using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        const double MissBar = 0.004;

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

            Assert.That(sweep.CrossExamined, Is.GreaterThan(0));
            Assert.That(sweep.OracleStalls, Is.GreaterThan(0), "No mutant broke, so nothing was cross-examined.");
            Assert.That(sweep.FalseAlarms, Is.Zero, sweep.Report);
        }

        [Test]
        public void ThePanelMissesNoMoreOfTheOraclesStallsThanTheMeasuredResidual()
        {
            var sweep = Mutants();

            Assert.That(sweep.MissRate, Is.LessThanOrEqualTo(MissBar), sweep.Report);
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
                Console.WriteLine("  spread P_max/P_min " + sweep.Spread());
            }

            Console.WriteLine(Mutants() + ", one enemy at a time inflated "
                + string.Join("/", Array.ConvertAll(Inflations, factor => factor + "-fold")));
            Console.WriteLine(ShipOracleReport());
        }

        static FuzzSweep Sweep(MazePreset preset)
        {
            FuzzSweep sweep;
            if (SweepByPreset.TryGetValue(preset.Name, out sweep))
            {
                return sweep;
            }

            var seeds = preset.Name == MazePreset.Ship.Name ? ShipSeeds : TinySeeds;
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
                        var mutant = Mutant.Of(level, node.Id, factor);

                        sweep.Compared(
                            mutant,
                            InvariantAOracle.Sweep(mutant.Graph, mutant.Tuning),
                            AdversaryPanel.FirstStall(mutant.Graph, mutant.Tuning));
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
                + SweepStatistics.Percentile(peaks, 0.5) + " max " + peaks[peaks.Count - 1] + ", "
                + SweepStatistics.Round(clock.Elapsed.TotalMilliseconds / count) + " ms each";
        }
    }
}
