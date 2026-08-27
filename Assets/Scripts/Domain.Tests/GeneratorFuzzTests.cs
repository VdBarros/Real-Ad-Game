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

        const int PlanSeeds = 1000;

        const int MutatedLevels = 400;

        const int ShipOracleSample = 6;

        const double RejectionBar = 0.9;

        const double MissBar = 0.004;

        static readonly int[] Inflations = { 3, 10, 50 };

        static readonly Dictionary<string, FuzzSweep> SweepByPreset = new Dictionary<string, FuzzSweep>();

        static readonly Dictionary<int, FuzzSweep> SweepByLevel = new Dictionary<int, FuzzSweep>();

        static MutationSweep mutants;

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        static IEnumerable<int> EveryPlanOnTheCurve()
        {
            yield return 1;
            yield return 7;
            yield return LevelPlan.PlateauLevel;
            yield return 20;
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void TheValidatorClearsEveryAcceptedLevel(MazePreset preset)
        {
            TheValidatorClears(Sweep(preset));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheValidatorClearsEveryLevelOnEveryPlan(int levelNumber)
        {
            TheValidatorClears(PlanSweep(levelNumber));
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoPolicyOnThePanelStrandsAnAcceptedLevel(MazePreset preset)
        {
            NoPolicyOnThePanelStrands(Sweep(preset));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void NoPolicyOnThePanelStrandsALevelOnAnyPlan(int levelNumber)
        {
            NoPolicyOnThePanelStrands(PlanSweep(levelNumber));
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void EveryBossStaysUnderTheBoundInvariantBDerivesItFrom(MazePreset preset)
        {
            EveryBossStaysUnderItsBound(Sweep(preset));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void EveryBossOnEveryPlanStaysUnderTheBoundInvariantBDerivesItFrom(int levelNumber)
        {
            EveryBossStaysUnderItsBound(PlanSweep(levelNumber));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheRejectionRateOfEveryPlanStaysUnderTheBar(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            Assert.That(
                sweep.RejectionRate,
                Is.LessThan(RejectionBar),
                sweep.Name + " rejected " + sweep.Rejections + " of " + sweep.Attempts + " attempts.");
        }

        [Test]
        public void TheSeaOfOnesThinsAsTheCurveClimbs()
        {
            var opening = PlanSweep(1);
            var plateau = PlanSweep(LevelPlan.PlateauLevel);

            Assert.That(
                plateau.ShareOfEnemiesAtOne(),
                Is.LessThan(opening.ShareOfEnemiesAtOne()),
                "opening " + opening.EnemyNumbers() + "; plateau " + plateau.EnemyNumbers());
        }

        [Test]
        public void ThePlanSweepReportsWhatItMeasured()
        {
            foreach (var levelNumber in EveryPlanOnTheCurve())
            {
                var sweep = PlanSweep(levelNumber);
                Console.WriteLine(sweep + ", " + sweep.Plan);
                Console.WriteLine("  spread P_max/P_min " + sweep.Spread());
                Console.WriteLine("  enemy numbers " + sweep.EnemyNumbers());
            }
        }

        static void TheValidatorClears(FuzzSweep sweep)
        {
            foreach (var accepted in sweep.Accepted)
            {
                Assert.That(
                    accepted.Verdict.IsSafe,
                    Is.True,
                    "Seed " + accepted.Level.AttemptSeed + ": " + accepted.Verdict);
            }
        }

        static void NoPolicyOnThePanelStrands(FuzzSweep sweep)
        {
            foreach (var accepted in sweep.Accepted)
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

        static void EveryBossStaysUnderItsBound(FuzzSweep sweep)
        {
            foreach (var accepted in sweep.Accepted)
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
            sweep = Walked(new FuzzSweep(preset.Name, LevelPlan.For(preset, 1), seeds));

            SweepByPreset.Add(preset.Name, sweep);
            return sweep;
        }

        static FuzzSweep PlanSweep(int levelNumber)
        {
            FuzzSweep sweep;
            if (SweepByLevel.TryGetValue(levelNumber, out sweep))
            {
                return sweep;
            }

            sweep = Walked(new FuzzSweep("level " + levelNumber, LevelPlan.For(levelNumber), PlanSeeds));

            SweepByLevel.Add(levelNumber, sweep);
            return sweep;
        }

        static FuzzSweep Walked(FuzzSweep sweep)
        {
            var plan = sweep.Plan;
            var clock = new Stopwatch();

            for (var seed = 1; seed <= sweep.Seeds; seed++)
            {
                LevelGenerationReport report;
                clock.Restart();

                try
                {
                    var level = LevelGenerator.Generate(seed, plan.Preset, plan.Recipe, plan.Tuning, out report);
                    clock.Stop();
                    sweep.Took(level, report, clock.Elapsed.TotalMilliseconds);
                }
                catch (LevelGenerationException exhausted)
                {
                    Assert.Fail("Seed " + seed + " on " + sweep.Name + ": " + exhausted.Message);
                }
            }

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
