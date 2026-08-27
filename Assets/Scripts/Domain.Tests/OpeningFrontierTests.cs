using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class OpeningFrontierTests
    {
        const long Seed = 20250826L;

        const int MoreChoicesThanALevelHolds = 20;

        [Test]
        public void ATuningCarriesTheNumberOfChoicesALevelHasToOpenOn()
        {
            Assert.That(PowerTuning.Ship.OpeningChoices, Is.EqualTo(1));
            Assert.That(PowerTuning.Ship.Opening(2).OpeningChoices, Is.EqualTo(2));
            Assert.That(PowerTuning.Ship.Opening(2).EnemyCap, Is.EqualTo(PowerTuning.Ship.EnemyCap));
            Assert.That(PowerTuning.Ship.Opening(PowerTuning.Ship.OpeningChoices), Is.SameAs(PowerTuning.Ship));
        }

        [Test]
        public void ALevelOpensOnAtLeastOneFight()
        {
            Assert.That(() => PowerTuning.Ship.Opening(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheOpeningFrontierHoldsTheEnemiesARunCanBeatBeforeMovingAnywhere()
        {
            var frontier = OpeningFrontier.Of(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(frontier.NodeIds, Is.EqualTo(new[] { LevelSketch.GateEnemyNodeId }));
            Assert.That(frontier.Count, Is.EqualTo(1));
        }

        [Test]
        public void AnEnemyBehindAnotherIsNoChoiceTheLevelOpensOn()
        {
            var frontier = OpeningFrontier.Of(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(frontier.NodeIds, Has.No.Member(LevelSketch.DeepEnemyNodeId));
        }

        [Test]
        public void AnEnemyTooDearToBeatOnArrivalIsNoChoiceEither()
        {
            var level = LevelSketch.Branching(gateEnemy: LevelSketch.Tuning.StartingPower).Build();

            Assert.That(OpeningFrontier.Of(level, LevelSketch.Tuning).Count, Is.Zero);
        }

        [Test]
        public void ALevelOpeningOnFewerChoicesThanItsPlanAsksForIsRejectedForThatReason()
        {
            LevelGenerationReport report;
            var exhausted = Assert.Throws<LevelGenerationException>(
                () => LevelGenerator.Generate(
                    Seed,
                    MazePreset.Ship,
                    ContentRecipe.Ship,
                    PowerTuning.Ship.Opening(MoreChoicesThanALevelHolds),
                    out report));

            Assert.That(exhausted.CountOf(ContentRejection.OpeningWithoutAChoice), Is.GreaterThan(0));
        }

        [Test]
        public void AnAcceptedLevelOffersEveryChoiceItsPlanAskedFor()
        {
            var tuning = PowerTuning.Ship.Opening(2);
            LevelGenerationReport report;
            var level = LevelGenerator.Generate(Seed, MazePreset.Ship, ContentRecipe.Ship, tuning, out report);

            Assert.That(OpeningFrontier.Of(level.Graph, tuning).Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void AStartWithOneWayOutIsAllTheOpeningPlanAsksTheLayoutFor()
        {
            MazeLayout narrow;
            LayoutRejection rejection;

            Assert.That(
                MazeLayoutGenerator.TryGenerate(Seed, MazePreset.Ship, 1, out narrow, out rejection),
                Is.True);
            Assert.That(WaysOutOfTheStart(narrow), Is.EqualTo(1));
        }

        [Test]
        public void ALevelThatOpensOnAChoiceAsksTheLayoutForAStartWithTwoWaysOut()
        {
            MazeLayout open;
            LayoutRejection rejection;

            Assert.That(
                MazeLayoutGenerator.TryGenerate(Seed, MazePreset.Ship, 2, out open, out rejection),
                Is.True);
            Assert.That(WaysOutOfTheStart(open), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void TheOpeningRuleOnlyRejectsAndNeverEditsAMintedNumber()
        {
            var strict = PowerTuning.Ship.Opening(2);
            var loose = PowerTuning.Ship.Opening(1);

            for (var attempt = 0; attempt < LevelGenerator.MaximumAttempts; attempt++)
            {
                MazeLayout layout;
                LayoutRejection layoutRejection;
                var attemptSeed = MazeLayoutGenerator.SeedOfAttempt(Seed, attempt);

                if (!MazeLayoutGenerator.TryGenerate(
                        attemptSeed, MazePreset.Ship, strict.OpeningChoices, out layout, out layoutRejection))
                {
                    continue;
                }

                PlacedLevel asked;
                PlacedLevel unasked;
                ContentRejection rejection;

                if (!ContentPlacer.TryPlace(layout, ContentRecipe.Ship, strict, out asked, out rejection))
                {
                    continue;
                }

                Assert.That(
                    ContentPlacer.TryPlace(layout, ContentRecipe.Ship, loose, out unasked, out rejection),
                    Is.True);
                Assert.That(Values(unasked), Is.EqualTo(Values(asked)));
                return;
            }

            Assert.Fail("No layout off seed " + Seed + " opened on a choice, so nothing was compared.");
        }

        [Test]
        public void TheOpeningPlanAsksForTheOneChoiceInvariantAAlreadyGuarantees()
        {
            Assert.That(LevelPlan.OpeningChoicesAt(1), Is.EqualTo(1));
            Assert.That(LevelPlan.For(1).OpeningChoices, Is.EqualTo(1));
        }

        [Test]
        public void EveryLevelAboveTheOpeningOneAsksForTwoChoices()
        {
            for (var level = 2; level <= 40; level++)
            {
                Assert.That(
                    LevelPlan.OpeningChoicesAt(level),
                    Is.EqualTo(LevelPlan.PlateauOpeningChoices),
                    "Level " + level + " opens on a corridor rather than a choice.");
                Assert.That(LevelPlan.For(level).OpeningChoices, Is.EqualTo(LevelPlan.OpeningChoicesAt(level)));
            }
        }

        [Test]
        public void LevelsAreCountedFromTheFirstOne()
        {
            Assert.That(() => LevelPlan.OpeningChoicesAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        static int WaysOutOfTheStart(MazeLayout layout)
        {
            return layout.Graph.Decisions.NeighboursOf(layout.StartNodeId).Count;
        }

        static List<int> Values(PlacedLevel level)
        {
            var values = new List<int>();
            foreach (var node in level.Graph.Decisions.Nodes)
            {
                values.Add(node.Value);
            }

            return values;
        }
    }
}
