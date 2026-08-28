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

        const int EliteMissDivisor = 100;

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

        static IEnumerable<int> EveryPlanAboveTheOpeningOne()
        {
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

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void EveryRegionAwayFromTheStartClearsItsPlansSpreadFloor(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);
            var floor = sweep.Plan.SpreadFloor;

            foreach (var accepted in sweep.Accepted)
            {
                foreach (var region in accepted.Level.Envelope.Regions)
                {
                    Assert.That(
                        region.SpreadClears(floor),
                        Is.True,
                        "Seed " + accepted.Level.AttemptSeed + " shipped " + region
                            + " under a floor of " + SweepStatistics.Round(floor) + ".");
                }
            }
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void EveryAcceptedLevelOpensOnTheChoicesItsPlanAsksFor(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            foreach (var accepted in sweep.Accepted)
            {
                Assert.That(
                    OpeningFrontier.Of(accepted.Level.Graph, accepted.Level.Tuning).Count,
                    Is.GreaterThanOrEqualTo(sweep.Plan.OpeningChoices),
                    "Seed " + accepted.Level.AttemptSeed + " opened on a corridor.");
            }
        }

        [TestCaseSource(nameof(EveryPlanAboveTheOpeningOne))]
        public void EveryLevelAboveTheOpeningPlanOpensOnMoreThanOneFight(int levelNumber)
        {
            foreach (var accepted in PlanSweep(levelNumber).Accepted)
            {
                Assert.That(
                    OpeningFrontier.Of(accepted.Level.Graph, accepted.Level.Tuning).Count,
                    Is.GreaterThan(1),
                    "Seed " + accepted.Level.AttemptSeed + " left the first tap without a choice.");
            }
        }

        [TestCaseSource(nameof(EveryPlanAboveTheOpeningOne))]
        public void NoBoostOnAnyPlanAboveTheOpeningOneSitsWhereTheRouteAlreadyGoes(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            foreach (var accepted in sweep.Accepted)
            {
                var detours = Detours.Of(accepted.Level.Graph);

                foreach (var node in accepted.Level.Graph.Decisions.Nodes)
                {
                    if (node.Type != NodeType.Additive && node.Type != NodeType.Multiplier)
                    {
                        continue;
                    }

                    Assert.That(
                        detours.Holds(node.Id),
                        Is.True,
                        "Seed " + accepted.Level.AttemptSeed + " handed out node #" + node.Id
                            + " on the way past.");
                }
            }

            Assert.That(sweep.PickupsOffADetour(), Is.Zero, sweep.Pickups());
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void EveryPlanAsksForTheSlotsItsSizeOffersSoNothingIsTurnedAwayForAMismatch(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            Assert.That(sweep.Plan.Recipe.Slots, Is.EqualTo(sweep.Preset.ContentSlots));
            Assert.That(sweep.CountOf(ContentRejection.RecipeSlotMismatch), Is.Zero);
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheOffPathFloorOfEveryPlanTurnsAwayFewerLayoutsThanItKeeps(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            Assert.That(
                sweep.CountOf(LayoutRejection.TooFewOffPathSlots),
                Is.LessThan(sweep.Attempts / 2),
                sweep.Name + " " + sweep.Pickups());
        }

        [Test]
        public void TheBraidOfALevelIsTheSameAtEveryPlan()
        {
            foreach (var levelNumber in EveryPlanOnTheCurve())
            {
                Assert.That(
                    PlanSweep(levelNumber).Preset.BraidFactor,
                    Is.EqualTo(MazePreset.Ship.BraidFactor),
                    "Level " + levelNumber + " braided its maze differently.");
            }
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheSpreadOfEveryPlanLeavesOneBehind(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            Assert.That(
                sweep.SpreadFloorReached(),
                Is.GreaterThan(1.0),
                sweep.Name + " spread " + sweep.Spread());
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheSpineOfEveryAcceptedLevelAffordsTheBoss(int levelNumber)
        {
            foreach (var accepted in PlanSweep(levelNumber).Accepted)
            {
                Assert.That(
                    Spine.Of(accepted.Level.Graph, accepted.Level.Tuning).ReachesTheBoss,
                    Is.True,
                    "Seed " + accepted.Level.AttemptSeed + " left its Spine short of the boss.");
            }
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void EveryEnemyOnTheSpineIsAffordableWhenTheSpineReachesIt(int levelNumber)
        {
            foreach (var accepted in PlanSweep(levelNumber).Accepted)
            {
                var decisions = accepted.Level.Graph.Decisions;
                var spine = Spine.Of(accepted.Level.Graph, accepted.Level.Tuning);

                for (var index = 0; index < spine.Length; index++)
                {
                    var node = decisions.Node(spine.NodeIds[index]);
                    if (node.Type != NodeType.Enemy)
                    {
                        continue;
                    }

                    Assert.That(
                        spine.ArrivalPowerAt(index),
                        Is.GreaterThan(node.Value),
                        "Seed " + accepted.Level.AttemptSeed + " put enemy #" + node.Id
                            + " on its Spine unable to pay for it.");
                }
            }
        }

        [TestCaseSource(nameof(EveryPlanAboveTheOpeningOne))]
        public void AllButAHandfulOfLevelsAboveTheOpeningPlanHoldAnEnemyOutOfReachOnArrival(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);
            var opened = 0;

            foreach (var accepted in sweep.Accepted)
            {
                if (Elites.Of(accepted.Level.Graph, accepted.Level.Tuning).Count == 0)
                {
                    opened++;
                }
            }

            Assert.That(
                opened,
                Is.LessThan(sweep.Accepted.Count / EliteMissDivisor),
                sweep.Name + " left " + opened + " of " + sweep.Accepted.Count
                    + " levels with every door already open; " + sweep.Locks());
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheParOfEveryAcceptedLevelStandsOnItsBeelineAndOnARouteThatBeatsTheBoss(int levelNumber)
        {
            foreach (var accepted in PlanSweep(levelNumber).Accepted)
            {
                var level = accepted.Level;
                var walk = ParWalk.Richest(level.Graph, level.Tuning);

                Assert.That(
                    level.Par.Floor,
                    Is.EqualTo(level.ShortestPathPower + level.BossPower),
                    "Seed " + level.AttemptSeed + " authored a Par floor away from its beeline.");

                Assert.That(
                    walk.BeatsTheBoss,
                    Is.True,
                    "Seed " + level.AttemptSeed + " left its richest walk short of the boss.");

                Assert.That(
                    level.Par.Ceiling,
                    Is.EqualTo(walk.Finish),
                    "Seed " + level.AttemptSeed + " authored a Par ceiling away from its richest walk.");
            }
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheWallsOfEveryAcceptedLevelsParStandStrictlyApart(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            foreach (var accepted in sweep.Accepted)
            {
                var par = accepted.Level.Par;

                Assert.That(
                    par.Floor,
                    Is.LessThan(par.Ceiling),
                    "Seed " + accepted.Level.AttemptSeed + " shipped " + par + ".");

                Assert.That(
                    par.IsDegenerate,
                    Is.False,
                    "Seed " + accepted.Level.AttemptSeed + " shipped " + par + ".");
            }

            Assert.That(sweep.ParsWithTheirWallsMet(), Is.Zero, sweep.Name + " par " + sweep.Rating());
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void NoBeelineOnAnyPlanCanFinishAboveItsOwnParsCeiling(int levelNumber)
        {
            foreach (var accepted in PlanSweep(levelNumber).Accepted)
            {
                var level = accepted.Level;

                Assert.That(
                    level.BossPower,
                    Is.GreaterThan(level.ShortestPathPower),
                    "Seed " + level.AttemptSeed + " let a beeline take the boss.");

                Assert.That(
                    level.Par.Floor,
                    Is.LessThan(2 * level.BossPower),
                    "Seed " + level.AttemptSeed + " put its Par floor above what beating the boss can pay.");

                Assert.That(
                    level.Par.Ceiling,
                    Is.GreaterThan(2 * level.BossPower),
                    "Seed " + level.AttemptSeed + " shipped a ceiling no winning route could hold.");
            }
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void TheCeilingOfEveryRatedLevelIsWhatAConstructedLegalRunActuallyHolds(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            foreach (var accepted in sweep.Rated())
            {
                var level = accepted.Level;

                Assert.That(
                    ReferenceRuns.Best(level),
                    Is.EqualTo(level.Par.Ceiling),
                    "Seed " + level.AttemptSeed + " could not be routed to its own Par ceiling.");

                Assert.That(
                    Stars.For(level.Par, ReferenceRuns.Best(level), levelNumber),
                    Is.EqualTo(Stars.Most),
                    "Seed " + level.AttemptSeed + " kept the third star out of reach of a legal run.");
            }

            Assert.That(sweep.CeilingsNoRouteReaches(), Is.Zero, sweep.Name + " par " + sweep.Rating());
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void ARunFinishingAtEitherWallOfItsParIsRatedFromThatWall(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            foreach (var accepted in sweep.Accepted)
            {
                var par = accepted.Level.Par;

                Assert.That(
                    Stars.For(par, par.Ceiling, levelNumber),
                    Is.EqualTo(Stars.Most),
                    "Seed " + accepted.Level.AttemptSeed + " kept the third star out of reach of " + par + ".");

                Assert.That(
                    Stars.For(par, par.Floor, levelNumber),
                    Is.EqualTo(Stars.Fewest),
                    "Seed " + accepted.Level.AttemptSeed + " handed a beeline more than one star on " + par + ".");
            }
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void ARoutedRunOutScoresAStingyOneOnEveryPlan(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);
            var stingy = Tallied(sweep.StingyStars());
            var routed = Tallied(sweep.RoutedStars());

            Assert.That(
                routed,
                Is.GreaterThan(stingy),
                sweep.Name + " rated routing no better than plodding: " + sweep.Rating());
        }

        [Test]
        public void TheThirdStarGetsHarderToHoldAsTheCurveClimbs()
        {
            var opening = PlanSweep(1);
            var plateau = PlanSweep(LevelPlan.PlateauLevel);

            Assert.That(
                LevelPlan.ThirdStarAt(plateau.LevelNumber),
                Is.GreaterThan(LevelPlan.ThirdStarAt(opening.LevelNumber)),
                "opening " + opening.Rating() + "; plateau " + plateau.Rating());

            Assert.That(
                ShareAtTheTop(plateau.RoutedStars()),
                Is.LessThan(ShareAtTheTop(opening.RoutedStars())),
                "A routed run held the third star as often at the plateau: opening "
                    + opening.Rating() + "; plateau " + plateau.Rating());

            Assert.That(
                ShareAtTheTop(plateau.PloddingStars()),
                Is.LessThan(ShareAtTheTop(opening.PloddingStars())),
                "A plodding run held the third star as often at the plateau: opening "
                    + opening.Rating() + "; plateau " + plateau.Rating());
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void ARoutedRunOutScoresAPloddingOneOnEveryPlan(int levelNumber)
        {
            var sweep = PlanSweep(levelNumber);

            Assert.That(
                Tallied(sweep.RoutedStars()),
                Is.GreaterThan(Tallied(sweep.PloddingStars())),
                sweep.Name + " rated routing no better than plodding: " + sweep.Rating());
        }

        static double ShareAtTheTop(IReadOnlyList<int> stars)
        {
            if (stars.Count == 0)
            {
                return 0.0;
            }

            var top = 0;
            foreach (var count in stars)
            {
                if (count == Stars.Most)
                {
                    top++;
                }
            }

            return (double)top / stars.Count;
        }

        static double Tallied(IReadOnlyList<int> stars)
        {
            if (stars.Count == 0)
            {
                return 0.0;
            }

            var total = 0;
            foreach (var count in stars)
            {
                total += count;
            }

            return (double)total / stars.Count;
        }

        [Test]
        public void TheDeepestLockOnALevelGetsDeeperAsTheCurveClimbs()
        {
            var opening = PlanSweep(1);
            var plateau = PlanSweep(LevelPlan.PlateauLevel);

            Assert.That(
                plateau.MedianDeepestLock(),
                Is.GreaterThan(opening.MedianDeepestLock()),
                "opening " + opening.DeepestLock() + "; plateau " + plateau.DeepestLock());
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
                Console.WriteLine("  elites " + sweep.Locks());
                Console.WriteLine("  spine " + sweep.SpineReach());
                Console.WriteLine("  opening " + sweep.Opening());
                Console.WriteLine("  boosts " + sweep.Pickups());
                Console.WriteLine("  par " + sweep.Rating());
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

        [TestCaseSource(nameof(EveryPreset))]
        public void NoDecisionNodeOnAnAcceptedLevelStandsOnAClimbingTile(MazePreset preset)
        {
            NoDecisionNodeStandsOnAClimbingTile(Sweep(preset));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void NoDecisionNodeOnAnyPlanStandsOnAClimbingTile(int levelNumber)
        {
            NoDecisionNodeStandsOnAClimbingTile(PlanSweep(levelNumber));
        }

        static void NoDecisionNodeStandsOnAClimbingTile(FuzzSweep sweep)
        {
            var climbing = 0;

            foreach (var accepted in sweep.Accepted)
            {
                foreach (var tile in accepted.Level.Graph.Tiles.Tiles)
                {
                    if (!Terraces.IsTerrace(tile.Position.Elevation))
                    {
                        climbing++;
                    }
                }

                foreach (var node in accepted.Level.Graph.Decisions.Nodes)
                {
                    Assert.That(
                        Terraces.IsTerrace(node.Position.Elevation),
                        Is.True,
                        "Seed " + accepted.Level.AttemptSeed + " stood node #" + node.Id
                            + " (" + node.Type + ") mid-climb at " + node.Position + ".");
                }
            }

            if (sweep.Preset.Stairs > 0)
            {
                Assert.That(
                    climbing,
                    Is.GreaterThan(0),
                    sweep.Name + " swept no climbing tile, so it proved nothing.");
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
            sweep = Walked(new FuzzSweep(preset.Name, LevelPlan.For(preset, 1), seeds, 1));

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

            sweep = Walked(new FuzzSweep(
                "level " + levelNumber, LevelPlan.For(levelNumber), PlanSeeds, levelNumber));

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
