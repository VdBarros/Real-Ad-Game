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
            Assert.That(() => LevelPlan.EliteFractionAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheOpeningPlanAsksForNoLocksAtAll()
        {
            Assert.That(LevelPlan.EliteFractionAt(1), Is.EqualTo(0.0));
            Assert.That(LevelPlan.For(1).EliteFraction, Is.EqualTo(0.0));
        }

        [Test]
        public void TheShareOfLocksClimbsAtEveryLevelUpToThePlateau()
        {
            for (var level = 2; level <= LevelPlan.PlateauLevel; level++)
            {
                Assert.That(
                    LevelPlan.EliteFractionAt(level),
                    Is.GreaterThan(LevelPlan.EliteFractionAt(level - 1)),
                    "Level " + level + " asks for no more locks than the level before it.");
            }
        }

        [Test]
        public void ThePlateauMintsEveryOffSpineEnemyRich()
        {
            Assert.That(LevelPlan.PlateauEliteFraction, Is.EqualTo(1.0));

            for (var level = LevelPlan.PlateauLevel; level <= WellPastThePlateau; level++)
            {
                Assert.That(LevelPlan.EliteFractionAt(level), Is.EqualTo(LevelPlan.PlateauEliteFraction));
            }
        }

        [Test]
        public void TheShareOfLocksIsAuthoredOnThePlanRatherThanLeftToTheSeed()
        {
            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var plan = LevelPlan.For(level);

                Assert.That(plan.EliteFraction, Is.EqualTo(LevelPlan.EliteFractionAt(level)));
                Assert.That(plan.Tuning.EliteFraction, Is.EqualTo(plan.EliteFraction));
            }
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
                Assert.That(second.Recipe.Multipliers, Is.EqualTo(first.Recipe.Multipliers));
                Assert.That(second.Recipe.Enemies, Is.EqualTo(first.Recipe.Enemies));
                Assert.That(second.Recipe.Additives, Is.EqualTo(first.Recipe.Additives));
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
        public void APlanLeavesEveryDialNoLevelNumberOwnsWhereTheOpeningPlanPutIt()
        {
            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var plan = LevelPlan.For(level);

                Assert.That(plan.Preset.BraidFactor, Is.EqualTo(MazePreset.Ship.BraidFactor));
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
        public void TheOpeningPlanKeepsTheTwoMultipliersTheBacklogAllowedIt()
        {
            Assert.That(LevelPlan.For(1).Recipe.Multipliers, Is.EqualTo(2));
            Assert.That(LevelPlan.MultiplierDriftAt(1), Is.Zero);
        }

        [Test]
        public void TheThirdMultiplierEntersOnlyWhereTheRaisedBaseCarriesIt()
        {
            Assert.That(LevelPlan.ThirdMultiplierLevel, Is.GreaterThan(1));
            Assert.That(
                LevelPlan.StartingPowerAt(LevelPlan.ThirdMultiplierLevel),
                Is.GreaterThan(LevelPlan.StartingPowerAt(1) * 4));

            for (var level = 1; level <= WellPastThePlateau; level++)
            {
                var expected = level < LevelPlan.ThirdMultiplierLevel ? 2 : 3;

                Assert.That(
                    LevelPlan.For(level).Recipe.Multipliers,
                    Is.EqualTo(expected),
                    "Level " + level + " counted its multipliers off the curve.");
            }
        }

        [Test]
        public void AdditivesThinAsTheLevelNumberRisesAndEnemiesTakeTheSlots()
        {
            for (var level = 2; level <= WellPastThePlateau; level++)
            {
                var earlier = LevelPlan.For(level - 1).Recipe;
                var later = LevelPlan.For(level).Recipe;

                Assert.That(
                    later.Additives,
                    Is.LessThanOrEqualTo(earlier.Additives),
                    "Level " + level + " asks for more boosts than the level before it.");
                Assert.That(
                    later.Enemies + later.Multipliers,
                    Is.GreaterThanOrEqualTo(earlier.Enemies + earlier.Multipliers),
                    "Level " + level + " let a slot go missing.");
            }

            Assert.That(LevelPlan.For(1).Recipe.Additives, Is.EqualTo(7));
            Assert.That(LevelPlan.For(1).Recipe.Enemies, Is.EqualTo(14));
            Assert.That(LevelPlan.For(LevelPlan.PlateauLevel).Recipe.Additives, Is.EqualTo(4));
            Assert.That(LevelPlan.For(LevelPlan.PlateauLevel).Recipe.Enemies, Is.EqualTo(16));
        }

        [Test]
        public void TheRecipeMixIsFlatAboveThePlateau()
        {
            var plateau = LevelPlan.For(LevelPlan.PlateauLevel).Recipe;

            for (var level = LevelPlan.PlateauLevel; level <= WellPastThePlateau; level++)
            {
                var recipe = LevelPlan.For(level).Recipe;

                Assert.That(recipe.Multipliers, Is.EqualTo(plateau.Multipliers));
                Assert.That(recipe.Enemies, Is.EqualTo(plateau.Enemies));
                Assert.That(recipe.Additives, Is.EqualTo(plateau.Additives));
            }
        }

        [Test]
        public void TheOffPathFloorRisesWithTheLevelNumber()
        {
            Assert.That(LevelPlan.For(1).MinimumOffPathSlots, Is.EqualTo(MazePreset.Ship.MinimumOffPathSlots));
            Assert.That(
                LevelPlan.For(LevelPlan.PlateauLevel).MinimumOffPathSlots,
                Is.EqualTo(MazePreset.Ship.MinimumOffPathSlots + LevelPlan.PlateauOffPathDemand));

            for (var level = 2; level <= WellPastThePlateau; level++)
            {
                Assert.That(
                    LevelPlan.For(level).MinimumOffPathSlots,
                    Is.GreaterThanOrEqualTo(LevelPlan.For(level - 1).MinimumOffPathSlots),
                    "Level " + level + " asks the layout for fewer off-path slots than the level before it.");
            }

            Assert.That(
                LevelPlan.For(LevelPlan.PlateauLevel).MinimumOffPathSlots,
                Is.GreaterThan(LevelPlan.For(1).MinimumOffPathSlots));
        }

        [Test]
        public void TheOffPathFloorIsFlatAboveThePlateau()
        {
            for (var level = LevelPlan.PlateauLevel; level <= WellPastThePlateau; level++)
            {
                Assert.That(LevelPlan.OffPathDemandAt(level), Is.EqualTo(LevelPlan.PlateauOffPathDemand));
            }
        }

        [Test]
        public void TheOpeningPlanPutsABoostWhereItLikesAndEveryPlanAboveItDoesNot()
        {
            Assert.That(LevelPlan.For(1).PickupsAskForADetour, Is.False);

            for (var level = 2; level <= WellPastThePlateau; level++)
            {
                Assert.That(
                    LevelPlan.For(level).PickupsAskForADetour,
                    Is.True,
                    "Level " + level + " still hands out a boost on the way past.");
            }
        }

        [Test]
        public void EveryKnobOnTheCurveIsCountedFromTheFirstLevel()
        {
            Assert.That(() => LevelPlan.AdditiveDriftAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => LevelPlan.MultiplierDriftAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => LevelPlan.OffPathDemandAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => LevelPlan.RecipeAt(MazePreset.Ship, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => LevelPlan.RecipeAt(null, 1), Throws.ArgumentNullException);
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
