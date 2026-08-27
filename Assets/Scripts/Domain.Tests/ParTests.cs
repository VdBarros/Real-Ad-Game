using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ParTests
    {
        static IEnumerable<int> EveryPlanOnTheCurve()
        {
            yield return 1;
            yield return 7;
            yield return LevelPlan.PlateauLevel;
            yield return 20;
        }

        [Test]
        public void APlacedLevelCarriesTheParItsNumbersImply()
        {
            var level = LevelGenerator.Generate(1, MazePreset.Ship);

            Assert.That(level.Par, Is.Not.Null);
            Assert.That(level.Par.Floor, Is.EqualTo(level.ShortestPathPower + level.BossPower));
        }

        [Test]
        public void TheSameLevelAlwaysHandsBackTheSamePar()
        {
            var level = LevelGenerator.Generate(3, MazePreset.Ship);

            Assert.That(level.Par, Is.SameAs(level.Par));
        }

        [Test]
        public void TheFloorOfAParIsTheBeelineToTheBossAndTheBossItself()
        {
            var level = LevelGenerator.Generate(5, MazePreset.Ship);

            Assert.That(level.Par.Floor, Is.EqualTo(level.ShortestPathPower + level.BossPower));
        }

        [Test]
        public void TheCeilingOfAParIsTheRichestEntryIntoTheBossesRegionAndTheBossItself()
        {
            var level = LevelGenerator.Generate(5, MazePreset.Ship);
            var bossRegion = level.Graph.RegionOf(level.BossNodeId);
            var entry = -1;

            foreach (var region in level.Envelope.Regions)
            {
                if (region.RegionId == bossRegion)
                {
                    entry = region.RichestEntry;
                }
            }

            Assert.That(entry, Is.GreaterThan(0));
            Assert.That(level.Par.Ceiling, Is.EqualTo(entry + level.BossPower));
        }

        [Test]
        public void NothingAboutAParIsDerivedFromTheBoundInvariantBUses()
        {
            Assert.That(SourceTree.Read("Domain", "Generation", "Par.cs"), Does.Not.Contain("PowerBound"));
            Assert.That(SourceTree.Read("Domain", "Generation", "Stars.cs"), Does.Not.Contain("PowerBound"));
        }

        [Test]
        public void ARunHoldingTheFloorSitsAtTheBottomOfItsPar()
        {
            var par = Par.Between(10, 110);

            Assert.That(par.PositionOf(10), Is.EqualTo(0.0));
        }

        [Test]
        public void ARunHoldingTheCeilingSitsAtTheTopOfItsPar()
        {
            var par = Par.Between(10, 110);

            Assert.That(par.PositionOf(110), Is.EqualTo(1.0));
        }

        [Test]
        public void ARunHalfWayUpAParSitsHalfWayUp()
        {
            var par = Par.Between(10, 110);

            Assert.That(par.PositionOf(60), Is.EqualTo(0.5));
        }

        [Test]
        public void APositionIsHeldInsideTheParItIsReadAgainst()
        {
            var par = Par.Between(10, 110);

            Assert.That(par.PositionOf(1), Is.EqualTo(0.0));
            Assert.That(par.PositionOf(10000), Is.EqualTo(1.0));
        }

        [Test]
        public void AParWhoseWallsMeetRatesNothing()
        {
            var par = Par.Between(40, 40);

            Assert.That(par.IsDegenerate, Is.True);
            Assert.That(par.PositionOf(40), Is.EqualTo(1.0));
        }

        [Test]
        public void AParIsNormalisedByItsSpreadRatherThanByItsCeiling()
        {
            var near = Par.Between(100, 200);
            var far = Par.Between(10000, 10100);

            Assert.That(far.PositionOf(10050), Is.EqualTo(near.PositionOf(150)));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void ARunFinishingAtTheFloorOfItsParScoresTheFewestStars(int levelNumber)
        {
            var par = Par.Between(100, 1100);

            Assert.That(Stars.For(par, par.Floor, levelNumber), Is.EqualTo(Stars.Fewest));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void ARunFinishingAtTheCeilingOfItsParScoresTheMostStars(int levelNumber)
        {
            var par = Par.Between(100, 1100);

            Assert.That(Stars.For(par, par.Ceiling, levelNumber), Is.EqualTo(Stars.Most));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void EveryStarCountBetweenTheFewestAndTheMostIsOnOffer(int levelNumber)
        {
            var par = Par.Between(0, 1000);
            var seen = new List<int>();

            for (var power = 0; power <= 1000; power++)
            {
                var stars = Stars.For(par, power, levelNumber);
                if (!seen.Contains(stars))
                {
                    seen.Add(stars);
                }
            }

            Assert.That(seen.Count, Is.EqualTo(Stars.Most - Stars.Fewest + 1));
        }

        [TestCaseSource(nameof(EveryPlanOnTheCurve))]
        public void AStarCountNeverLeavesTheRangeItIsCountedIn(int levelNumber)
        {
            var par = Par.Between(50, 150);

            for (var power = 0; power <= 300; power++)
            {
                var stars = Stars.For(par, power, levelNumber);
                Assert.That(stars, Is.GreaterThanOrEqualTo(Stars.Fewest));
                Assert.That(stars, Is.LessThanOrEqualTo(Stars.Most));
            }
        }

        [Test]
        public void MorePowerNeverCostsAStar()
        {
            var par = Par.Between(20, 520);

            foreach (var levelNumber in EveryPlanOnTheCurve())
            {
                var held = Stars.Fewest;
                for (var power = 0; power <= 600; power++)
                {
                    var stars = Stars.For(par, power, levelNumber);
                    Assert.That(stars, Is.GreaterThanOrEqualTo(held));
                    held = stars;
                }
            }
        }

        [Test]
        public void TheThirdStarStartsAroundTheTopThirdOfTheOpeningPlan()
        {
            Assert.That(LevelPlan.ThirdStarAt(1), Is.EqualTo(2.0 / 3.0).Within(0.001));
        }

        [Test]
        public void TheThirdStarEndsAroundTheTopTenthOfThePlateau()
        {
            Assert.That(LevelPlan.ThirdStarAt(LevelPlan.PlateauLevel), Is.EqualTo(0.9).Within(0.001));
        }

        [Test]
        public void EveryStarThresholdTightensAsTheLevelNumberRises()
        {
            for (var levelNumber = 1; levelNumber < LevelPlan.PlateauLevel; levelNumber++)
            {
                Assert.That(
                    LevelPlan.ThirdStarAt(levelNumber + 1),
                    Is.GreaterThan(LevelPlan.ThirdStarAt(levelNumber)),
                    "The third star did not tighten between " + levelNumber + " and " + (levelNumber + 1) + ".");

                Assert.That(
                    LevelPlan.SecondStarAt(levelNumber + 1),
                    Is.GreaterThan(LevelPlan.SecondStarAt(levelNumber)),
                    "The second star did not tighten between " + levelNumber + " and " + (levelNumber + 1) + ".");
            }
        }

        [Test]
        public void EveryStarThresholdIsFlatAboveThePlateau()
        {
            for (var levelNumber = LevelPlan.PlateauLevel; levelNumber < LevelPlan.PlateauLevel + 20; levelNumber++)
            {
                Assert.That(
                    LevelPlan.ThirdStarAt(levelNumber),
                    Is.EqualTo(LevelPlan.ThirdStarAt(LevelPlan.PlateauLevel)));

                Assert.That(
                    LevelPlan.SecondStarAt(levelNumber),
                    Is.EqualTo(LevelPlan.SecondStarAt(LevelPlan.PlateauLevel)));
            }
        }

        [Test]
        public void TheSecondStarIsAlwaysEasierThanTheThird()
        {
            for (var levelNumber = 1; levelNumber <= LevelPlan.PlateauLevel + 10; levelNumber++)
            {
                Assert.That(
                    LevelPlan.SecondStarAt(levelNumber),
                    Is.LessThan(LevelPlan.ThirdStarAt(levelNumber)),
                    "Level " + levelNumber + " asked more of two stars than of three.");
            }
        }

        [Test]
        public void NoStarThresholdEverLeavesTheCeilingOutOfReach()
        {
            for (var levelNumber = 1; levelNumber <= LevelPlan.PlateauLevel + 10; levelNumber++)
            {
                Assert.That(
                    LevelPlan.ThirdStarAt(levelNumber),
                    Is.LessThanOrEqualTo(1.0),
                    "Level " + levelNumber + " asked for more than a Par's ceiling.");

                Assert.That(
                    LevelPlan.SecondStarAt(levelNumber),
                    Is.GreaterThan(0.0),
                    "Level " + levelNumber + " handed out two stars at its Par's floor.");
            }
        }

        [Test]
        public void TheSupplyRemembersWhichLevelItLastDrew()
        {
            var supply = new LevelSupply(20260827L, MazePreset.Ship);

            Assert.That(supply.LastLevelNumber, Is.Zero);

            supply.Draw();
            Assert.That(supply.LastLevelNumber, Is.EqualTo(1));

            supply.Draw();
            Assert.That(supply.LastLevelNumber, Is.EqualTo(2));
        }

        [Test]
        public void TheLevelNumberAStarCountReadsComesFromTheSupplyRatherThanTheCycle()
        {
            var supply = new LevelSupply(20260827L, MazePreset.Ship);
            var level = supply.Draw();

            Assert.That(
                Stars.For(level.Par, level.Par.Ceiling, supply.LastLevelNumber),
                Is.EqualTo(Stars.Most));
        }
    }
}
