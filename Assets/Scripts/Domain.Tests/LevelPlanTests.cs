using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LevelPlanTests
    {
        const int WellPastThePlateau = 40;

        static readonly int[] TheCurve = { 2, 3, 4, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16 };

        [Test]
        public void TheOpeningPlanIsTheShippedRecipeAndTuningVerbatim()
        {
            var opening = LevelPlan.For(1);

            Assert.That(opening.Preset, Is.SameAs(MazePreset.Ship));
            Assert.That(opening.Recipe, Is.SameAs(ContentRecipe.Ship));
            Assert.That(opening.Tuning, Is.SameAs(PowerTuning.Ship));
        }

        [Test]
        public void TheOpeningPlanStartsOnThePowerTheReferenceAdOpensWith()
        {
            Assert.That(LevelPlan.StartingPowerAt(1), Is.EqualTo(2));
            Assert.That(LevelPlan.StartingPowerAt(1), Is.EqualTo(PowerTuning.Ship.StartingPower));
        }

        [Test]
        public void LevelsAreCountedFromTheFirstOne()
        {
            Assert.That(() => LevelPlan.For(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => LevelPlan.For(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => LevelPlan.StartingPowerAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void StartingPowerClimbsAtEveryLevelUpToThePlateau()
        {
            for (var level = 2; level <= LevelPlan.PlateauLevel; level++)
            {
                Assert.That(
                    LevelPlan.StartingPowerAt(level),
                    Is.GreaterThan(LevelPlan.StartingPowerAt(level - 1)),
                    "Level " + level + " opens no richer than the level before it.");
            }
        }

        [Test]
        public void StartingPowerIsFlatAboveThePlateau()
        {
            Assert.That(LevelPlan.StartingPowerAt(LevelPlan.PlateauLevel), Is.EqualTo(LevelPlan.PlateauStartingPower));

            for (var level = LevelPlan.PlateauLevel; level <= WellPastThePlateau; level++)
            {
                Assert.That(LevelPlan.StartingPowerAt(level), Is.EqualTo(LevelPlan.PlateauStartingPower));
            }
        }

        [Test]
        public void ThePlateauIsReachedInsideASingleSitting()
        {
            Assert.That(LevelPlan.PlateauLevel, Is.InRange(12, 15));
        }

        [Test]
        public void TheCurveIsTheOneWrittenDown()
        {
            Assert.That(TheCurve.Length, Is.EqualTo(LevelPlan.PlateauLevel));

            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var expected = level <= TheCurve.Length ? TheCurve[level - 1] : LevelPlan.PlateauStartingPower;

                Assert.That(LevelPlan.StartingPowerAt(level), Is.EqualTo(expected), "Level " + level + " left the curve.");
            }
        }

        [Test]
        public void AskingTwiceForALevelsPlanAnswersTheSame()
        {
            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var first = LevelPlan.For(level);
                var second = LevelPlan.For(level);

                Assert.That(second.Preset, Is.SameAs(first.Preset));
                Assert.That(second.Recipe, Is.SameAs(first.Recipe));
                Assert.That(second.Tuning.StartingPower, Is.EqualTo(first.Tuning.StartingPower));
                Assert.That(second.Tuning.StripTarget, Is.EqualTo(first.Tuning.StripTarget));
            }
        }

        [Test]
        public void EveryLevelIsPlayedAtTheSizeTheGameShips()
        {
            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                Assert.That(LevelPlan.For(level).Preset, Is.SameAs(MazePreset.Ship));
            }
        }

        [Test]
        public void EveryPlanAsksForExactlyTheSlotsItsSizeOffers()
        {
            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var plan = LevelPlan.For(level);

                Assert.That(plan.Recipe.Slots, Is.EqualTo(plan.Preset.ContentSlots));
            }
        }

        [Test]
        public void EveryPlanKeepsTheStripRatioTheOpeningPlanSets()
        {
            var ratio = PowerTuning.Ship.StripTarget / PowerTuning.Ship.StartingPower;

            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var tuning = LevelPlan.For(level).Tuning;

                Assert.That(tuning.StripTarget, Is.EqualTo(ratio * tuning.StartingPower));
            }
        }

        [Test]
        public void APlanMovesNothingButThePowerTheRunOpensWith()
        {
            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var plan = LevelPlan.For(level);

                Assert.That(plan.Recipe, Is.SameAs(ContentRecipe.Ship));
                Assert.That(plan.Tuning.EnemyCap, Is.EqualTo(PowerTuning.Ship.EnemyCap));
                Assert.That(plan.Tuning.Jitter, Is.EqualTo(PowerTuning.Ship.Jitter));
                Assert.That(plan.Tuning.BossFactor, Is.EqualTo(PowerTuning.Ship.BossFactor));
                Assert.That(plan.Tuning.GatePreference, Is.EqualTo(PowerTuning.Ship.GatePreference));
                Assert.That(plan.Tuning.PocketTreasure, Is.EqualTo(PowerTuning.Ship.PocketTreasure));
            }
        }

        [Test]
        public void APlanForAnotherSizeKeepsThatSizesRecipeAndClimbsTheSameCurve()
        {
            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Stress })
            {
                Assert.That(LevelPlan.For(preset, 1).Recipe, Is.SameAs(ContentRecipe.For(preset)));
                Assert.That(LevelPlan.For(preset, 1).Tuning, Is.SameAs(PowerTuning.For(preset)));

                for (var level = 1; level <= WellPastThePlateau; level++)
                {
                    var plan = LevelPlan.For(preset, level);

                    Assert.That(plan.Preset, Is.SameAs(preset));
                    Assert.That(plan.StartingPower, Is.EqualTo(LevelPlan.StartingPowerAt(level)));
                }
            }
        }

        [Test]
        public void APlanNeedsASizeARecipeAndATuning()
        {
            Assert.That(() => new LevelPlan(null, ContentRecipe.Ship, PowerTuning.Ship), Throws.ArgumentNullException);
            Assert.That(() => new LevelPlan(MazePreset.Ship, null, PowerTuning.Ship), Throws.ArgumentNullException);
            Assert.That(() => new LevelPlan(MazePreset.Ship, ContentRecipe.Ship, null), Throws.ArgumentNullException);
            Assert.That(() => LevelPlan.For(null, 1), Throws.ArgumentNullException);
        }

        [Test]
        public void APlanNamesItsSizeAndThePowerItOpensOn()
        {
            var plan = LevelPlan.For(LevelPlan.PlateauLevel);

            Assert.That(plan.StartingPower, Is.EqualTo(LevelPlan.PlateauStartingPower));
            Assert.That(plan.ToString(), Does.Contain(MazePreset.Ship.Name));
            Assert.That(plan.ToString(), Does.Contain(LevelPlan.PlateauStartingPower.ToString()));
        }

        [Test]
        public void ALevelCarriesThePlanItWasGeneratedFrom()
        {
            var plan = LevelPlan.For(9);
            LevelGenerationReport report;
            var level = LevelGenerator.Generate(20250826L, plan.Preset, plan.Recipe, plan.Tuning, out report);

            Assert.That(level.Plan.Preset, Is.SameAs(plan.Preset));
            Assert.That(level.Plan.Recipe, Is.SameAs(plan.Recipe));
            Assert.That(level.Plan.Tuning, Is.SameAs(plan.Tuning));
            Assert.That(level.StartingPower, Is.EqualTo(LevelPlan.StartingPowerAt(9)));
        }

        [Test]
        public void ALevelGeneratedOffAPresetAloneCarriesThatPresetsOpeningPlan()
        {
            var level = LevelGenerator.Generate(20250826L, MazePreset.Ship);

            Assert.That(level.Plan.Preset, Is.SameAs(MazePreset.Ship));
            Assert.That(level.Plan.Recipe, Is.SameAs(ContentRecipe.Ship));
            Assert.That(level.Plan.Tuning, Is.SameAs(PowerTuning.Ship));
        }

        [Test]
        public void EveryLevelOnTheCurveGenerates()
        {
            var powers = new List<int>();

            for (var level = 1; level <= LevelPlan.PlateauLevel; level++)
            {
                var plan = LevelPlan.For(level);
                LevelGenerationReport report;
                var placed = LevelGenerator.Generate(4242L + level, plan.Preset, plan.Recipe, plan.Tuning, out report);

                Assert.That(placed.Verdict.IsSafe, Is.True);
                powers.Add(placed.StartingPower);
            }

            Assert.That(powers[powers.Count - 1], Is.GreaterThan(powers[0]));
        }
    }
}
