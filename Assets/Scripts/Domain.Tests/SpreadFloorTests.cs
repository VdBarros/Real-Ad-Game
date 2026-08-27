using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class SpreadFloorTests
    {
        const long Seed = 20250826L;

        const double UnreachableFloor = 1000000.0;

        [Test]
        public void ATuningCarriesTheFloorEveryRegionsSpreadHasToClear()
        {
            Assert.That(PowerTuning.Ship.SpreadFloor, Is.EqualTo(1.0));
            Assert.That(PowerTuning.Ship.Routing(4.0).SpreadFloor, Is.EqualTo(4.0));
            Assert.That(PowerTuning.Ship.Routing(4.0).EnemyCap, Is.EqualTo(PowerTuning.Ship.EnemyCap));
            Assert.That(PowerTuning.Ship.Routing(PowerTuning.Ship.SpreadFloor), Is.SameAs(PowerTuning.Ship));
        }

        [Test]
        public void ARegionsRichestEntryIsNeverPoorerThanItsCheapestOne()
        {
            Assert.That(
                () => PowerTuning.Ship.Routing(0.5),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheRegionARunBeginsInIsEnteredOnceAndSoIsExemptFromTheFloor()
        {
            var level = LevelSketch.Solvable().Build();
            var envelope = PowerEnvelope.Of(level, LevelSketch.Tuning);
            var startRegion = level.RegionOf(0);

            foreach (var region in envelope.Regions)
            {
                Assert.That(region.HoldsTheStart, Is.EqualTo(region.RegionId == startRegion));
                Assert.That(region.SpreadClears(UnreachableFloor), Is.EqualTo(region.HoldsTheStart));
            }
        }

        [Test]
        public void AnEnvelopeNamesTheFirstRegionWhoseSpreadIsUnderTheFloor()
        {
            var envelope = PowerEnvelope.Of(LevelSketch.Solvable().Build(), LevelSketch.Tuning);

            Assert.That(envelope.FirstRegionUnderTheFloor(1.0), Is.Null);
            Assert.That(envelope.FirstRegionUnderTheFloor(UnreachableFloor), Is.Not.Null);
            Assert.That(envelope.FirstRegionUnderTheFloor(UnreachableFloor).HoldsTheStart, Is.False);
        }

        [Test]
        public void ALevelHoldingARegionUnderItsFloorIsRejectedForThatReason()
        {
            LevelGenerationReport report;
            var exhausted = Assert.Throws<LevelGenerationException>(
                () => LevelGenerator.Generate(
                    Seed,
                    MazePreset.Ship,
                    ContentRecipe.Ship,
                    PowerTuning.Ship.Routing(UnreachableFloor),
                    out report));

            Assert.That(exhausted.CountOf(ContentRejection.RegionSpreadTooThin), Is.GreaterThan(0));
        }

        [Test]
        public void EveryRegionOfAnAcceptedLevelAwayFromTheStartClearsTheFloor()
        {
            var floor = 3.0;
            LevelGenerationReport report;
            var level = LevelGenerator.Generate(
                Seed, MazePreset.Ship, ContentRecipe.Ship, PowerTuning.Ship.Routing(floor), out report);

            foreach (var region in level.Envelope.Regions)
            {
                Assert.That(
                    region.SpreadClears(floor),
                    Is.True,
                    "Seed " + level.AttemptSeed + " shipped " + region + " under a floor of " + floor + ".");
            }
        }

        [Test]
        public void TheFloorOnlyRejectsAndNeverEditsAMintedNumber()
        {
            LevelGenerationReport report;
            var loose = LevelGenerator.Generate(
                Seed, MazePreset.Ship, ContentRecipe.Ship, PowerTuning.Ship.Routing(1.0), out report);

            var thinnest = double.MaxValue;
            foreach (var region in loose.Envelope.Regions)
            {
                if (!region.HoldsTheStart)
                {
                    thinnest = Math.Min(thinnest, region.Spread);
                }
            }

            var tight = LevelGenerator.Generate(
                Seed, MazePreset.Ship, ContentRecipe.Ship, PowerTuning.Ship.Routing(thinnest), out report);

            Assert.That(Values(tight), Is.EqualTo(Values(loose)));
        }

        [Test]
        public void TheOpeningPlanAsksForNoSpreadAtAllAndThePlateauAsksForTheMost()
        {
            Assert.That(LevelPlan.SpreadFloorAt(1), Is.EqualTo(1.0));
            Assert.That(LevelPlan.For(1).SpreadFloor, Is.EqualTo(1.0));
            Assert.That(LevelPlan.SpreadFloorAt(LevelPlan.PlateauLevel), Is.EqualTo(LevelPlan.PlateauSpreadFloor));
        }

        [Test]
        public void TheFloorClimbsAtEveryLevelUpToThePlateauAndIsFlatAboveIt()
        {
            for (var level = 2; level <= LevelPlan.PlateauLevel; level++)
            {
                Assert.That(
                    LevelPlan.SpreadFloorAt(level),
                    Is.GreaterThan(LevelPlan.SpreadFloorAt(level - 1)),
                    "Level " + level + " asks for no more routing value than the level before it.");
            }

            for (var level = LevelPlan.PlateauLevel; level <= 40; level++)
            {
                Assert.That(LevelPlan.SpreadFloorAt(level), Is.EqualTo(LevelPlan.PlateauSpreadFloor));
            }
        }

        [Test]
        public void TheFloorIsAskedForOnThePlanRatherThanLeftToTheSeed()
        {
            for (var level = 1; level <= 40; level++)
            {
                var plan = LevelPlan.For(level);

                Assert.That(plan.SpreadFloor, Is.EqualTo(LevelPlan.SpreadFloorAt(level)));
                Assert.That(plan.Tuning.SpreadFloor, Is.EqualTo(plan.SpreadFloor));
            }
        }

        [Test]
        public void LevelsAreCountedFromTheFirstOne()
        {
            Assert.That(() => LevelPlan.SpreadFloorAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
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
