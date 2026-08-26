using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LevelSupplyTests
    {
        const long Opening = 20250825L;

        const long RetryingOpening = 12L;

        const int Cycles = 20;

        static readonly MazePreset Unbuildable =
            new MazePreset("tiny", 4, 3, 1, 2, 0.25, 0, 11, 10000, 3);

        [Test]
        public void ASupplyStartsHoldingNothingButItsSeedAndPreset()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);

            Assert.That(supply.OpeningSeed, Is.EqualTo(Opening));
            Assert.That(supply.Preset, Is.SameAs(MazePreset.Ship));
            Assert.That(supply.LevelsDrawn, Is.Zero);
            Assert.That(supply.SeedsSpent, Is.Zero);
            Assert.That(supply.DrawsFailed, Is.Zero);
            Assert.That(supply.RetriesAbsorbed, Is.Zero);
            Assert.That(supply.LastReport, Is.Null);
        }

        [Test]
        public void ASupplyNeedsAPreset()
        {
            Assert.That(() => new LevelSupply(Opening, null), Throws.ArgumentNullException);
        }

        [Test]
        public void LevelsAreCountedFromTheFirstOne()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);

            Assert.That(() => supply.SeedOf(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => supply.SeedOf(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheSeedOfALevelDoesNotDependOnWhenItIsAskedFor()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);
            var other = new LevelSupply(Opening, MazePreset.Tiny);

            for (var level = 1; level <= Cycles; level++)
            {
                Assert.That(supply.SeedOf(level), Is.EqualTo(other.SeedOf(level)));
                Assert.That(supply.SeedOf(level), Is.EqualTo(supply.SeedOf(level)));
            }
        }

        [Test]
        public void TwentyLevelsAreTwentyDifferentSeeds()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);
            var seen = new HashSet<long>();

            for (var level = 1; level <= Cycles; level++)
            {
                Assert.That(seen.Add(supply.SeedOf(level)), Is.True, "Level " + level + " repeats an earlier seed.");
            }
        }

        [Test]
        public void ADifferentOpeningSeedIsADifferentSequence()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);
            var other = new LevelSupply(Opening + 1, MazePreset.Ship);

            for (var level = 1; level <= Cycles; level++)
            {
                Assert.That(supply.SeedOf(level), Is.Not.EqualTo(other.SeedOf(level)));
            }
        }

        [Test]
        public void ALevelSeedNeverLandsOnAnotherLevelsRetrySeed()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);
            var attempts = new Dictionary<long, int>();

            for (var level = 1; level <= Cycles; level++)
            {
                var seed = supply.SeedOf(level);
                for (var attempt = 0; attempt < LevelGenerator.MaximumAttempts; attempt++)
                {
                    var attemptSeed = MazeLayoutGenerator.SeedOfAttempt(seed, attempt);

                    int owner;
                    Assert.That(
                        attempts.TryGetValue(attemptSeed, out owner),
                        Is.False,
                        "Level " + level + " retries onto a seed level " + owner + " already walked.");

                    attempts.Add(attemptSeed, level);
                }
            }
        }

        [Test]
        public void DrawingALevelAdvancesTheCountAndFilesTheReport()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);
            var first = supply.Draw();

            Assert.That(supply.LevelsDrawn, Is.EqualTo(1));
            Assert.That(supply.LastReport, Is.Not.Null);
            Assert.That(first.AttemptSeed, Is.EqualTo(
                MazeLayoutGenerator.SeedOfAttempt(supply.SeedOf(1), supply.LastReport.Attempts - 1)));

            var second = supply.Draw();

            Assert.That(supply.LevelsDrawn, Is.EqualTo(2));
            Assert.That(second.Graph.Seed, Is.Not.EqualTo(first.Graph.Seed));
        }

        [Test]
        public void ARejectedLayoutIsRetriedSilentlyAndTheDrawStillLands()
        {
            var supply = new LevelSupply(RetryingOpening, MazePreset.Ship);
            var level = supply.Draw();

            Assert.That(supply.LastReport.Attempts, Is.GreaterThan(1),
                "This opening seed no longer rejects, so it has stopped proving anything.");
            Assert.That(supply.LastReport.Rejections, Is.EqualTo(supply.LastReport.Attempts - 1));
            Assert.That(supply.RetriesAbsorbed, Is.EqualTo(supply.LastReport.Attempts - 1));
            Assert.That(level, Is.Not.Null);
            Assert.That(level.Verdict.IsSafe, Is.True);
        }

        [Test]
        public void RetriesNeverRunPastTheValidatorsCap()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);

            for (var level = 1; level <= Cycles; level++)
            {
                supply.Draw();
                Assert.That(supply.LastReport.Attempts, Is.InRange(1, LevelGenerator.MaximumAttempts));
            }

            Assert.That(supply.RetriesAbsorbed, Is.GreaterThan(0),
                "Twenty levels off this seed used to absorb retries, so this guard has gone blind.");
        }

        [Test]
        public void TwentyDrawsAreTwentySolvableLevels()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);

            for (var level = 1; level <= Cycles; level++)
            {
                var placed = supply.Draw();

                Assert.That(placed.Verdict.IsSafe, Is.True);
                Assert.That(placed.Graph.Seed, Is.EqualTo(placed.AttemptSeed));
                Assert.That(placed.BossNodeId, Is.GreaterThanOrEqualTo(0));
            }

            Assert.That(supply.LevelsDrawn, Is.EqualTo(Cycles));
        }

        [Test]
        public void ASeedThatNoLevelSurvivesIsSpentRatherThanDrawnAgain()
        {
            var supply = new LevelSupply(Opening, Unbuildable);

            Assert.That(() => supply.Draw(), Throws.InstanceOf<LevelGenerationException>());
            Assert.That(supply.SeedsSpent, Is.EqualTo(1));
            Assert.That(supply.LevelsDrawn, Is.Zero);
            Assert.That(supply.DrawsFailed, Is.EqualTo(1));

            Assert.That(() => supply.Draw(), Throws.InstanceOf<LevelGenerationException>());
            Assert.That(supply.SeedsSpent, Is.EqualTo(2));
            Assert.That(supply.DrawsFailed, Is.EqualTo(2));
        }

        [Test]
        public void AFailedDrawMovesTheSupplyOntoASeedItHasNotTried()
        {
            var supply = new LevelSupply(Opening, Unbuildable);
            var poisoned = supply.SeedOf(supply.SeedsSpent + 1);

            Assert.That(() => supply.Draw(), Throws.InstanceOf<LevelGenerationException>());

            Assert.That(supply.SeedOf(supply.SeedsSpent), Is.EqualTo(poisoned));
            Assert.That(supply.SeedOf(supply.SeedsSpent + 1), Is.Not.EqualTo(poisoned));
        }

        [Test]
        public void ADrawThatLandsCountsAsBothASeedSpentAndALevelDrawn()
        {
            var supply = new LevelSupply(Opening, MazePreset.Ship);
            supply.Draw();

            Assert.That(supply.SeedsSpent, Is.EqualTo(1));
            Assert.That(supply.LevelsDrawn, Is.EqualTo(1));
            Assert.That(supply.DrawsFailed, Is.Zero);
        }
    }
}
