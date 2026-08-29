using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class EnemyTierTests
    {
        const long OpeningSeed = 7919L;

        const int RunLength = 16;

        const double RarestShare = 0.10;

        static readonly int[] Boundaries = { 12, 50 };

        [Test]
        public void TheMeasuredBoundariesAreTheOnesTheTierFunctionUses()
        {
            Assert.That(EnemyTier.Thresholds, Is.EqualTo(Boundaries));
            Assert.That(EnemyTier.Count, Is.EqualTo(Boundaries.Length + 1));
        }

        [Test]
        public void ThreeBandsIsWhatThePackCanDressAndTheTableAsksForNoMore()
        {
            Assert.That(EnemyTier.Count, Is.EqualTo(3));
        }

        [Test]
        public void EachBoundaryPromotesOnItsOwnValueAndNotOneBelow()
        {
            for (var index = 0; index < Boundaries.Length; index++)
            {
                Assert.That(EnemyTier.Of(Boundaries[index] - 1), Is.EqualTo(index));
                Assert.That(EnemyTier.Of(Boundaries[index]), Is.EqualTo(index + 1));
            }
        }

        [Test]
        public void TheSmallestEnemySitsAtTheBottomTierAndTheLargestAtTheTop()
        {
            Assert.That(EnemyTier.Of(1), Is.EqualTo(0));
            Assert.That(EnemyTier.Of(11), Is.EqualTo(0));
            Assert.That(EnemyTier.Of(408), Is.EqualTo(EnemyTier.Count - 1));
            Assert.That(EnemyTier.Of(490), Is.EqualTo(EnemyTier.Count - 1));
        }

        [Test]
        public void TheTopTierIsOpenEndedSoNoNumberFallsOffTheTable()
        {
            Assert.That(EnemyTier.Of(int.MaxValue), Is.EqualTo(EnemyTier.Count - 1));
        }

        [Test]
        public void TierNeverFallsAsTheNumberRises()
        {
            var previous = 0;
            for (var number = 1; number <= 1000; number++)
            {
                var tier = EnemyTier.Of(number);
                Assert.That(tier, Is.GreaterThanOrEqualTo(previous));
                Assert.That(tier, Is.InRange(0, EnemyTier.Count - 1));
                previous = tier;
            }
        }

        [Test]
        public void ANumberNoEnemyCanHoldIsRefusedRatherThanBanded()
        {
            Assert.That(() => EnemyTier.Of(0), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => EnemyTier.Of(-1), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void EveryTierIsReachedWalkingTheNumbersOneByOne()
        {
            var reached = new HashSet<int>();
            for (var number = PowerTuning.Ship.StartingPower; number <= 490; number++)
            {
                reached.Add(EnemyTier.Of(number));
            }

            Assert.That(reached.Count, Is.EqualTo(EnemyTier.Count));
        }

        [Test]
        public void EveryTierCarriesARealShareOfTheEnemiesATypicalRunMints()
        {
            var minted = RunEnemies();
            var counts = new int[EnemyTier.Count];

            foreach (var value in minted)
            {
                counts[EnemyTier.Of(value)]++;
            }

            Assert.That(minted.Count, Is.GreaterThan(0));

            for (var tier = 0; tier < counts.Length; tier++)
            {
                Assert.That(
                    counts[tier] / (double)minted.Count,
                    Is.GreaterThan(RarestShare),
                    "tier " + tier + " holds " + counts[tier] + " of " + minted.Count
                    + " enemies over levels 1 to " + RunLength);
            }
        }

        [Test]
        public void NoTierEatsTheRunTheWayFiveBandsStretchedOverThreeMeshesDid()
        {
            var minted = RunEnemies();
            var counts = new int[EnemyTier.Count];

            foreach (var value in minted)
            {
                counts[EnemyTier.Of(value)]++;
            }

            Assert.That(counts.Max() / (double)minted.Count, Is.LessThan(0.6));
        }

        [Test]
        public void TheEnemyTableIsItsOwnAndOwesNothingToThePlayerOne()
        {
            Assert.That(EnemyTier.Thresholds, Is.Not.SameAs(PlayerTier.Thresholds));
        }

        static List<int> RunEnemies()
        {
            var minted = new List<int>();

            for (var levelNumber = 1; levelNumber <= RunLength; levelNumber++)
            {
                var plan = LevelPlan.For(levelNumber);
                LevelGenerationReport ignored;
                var placed = LevelGenerator.Generate(
                    LevelSupply.Scattered(OpeningSeed, levelNumber),
                    plan.Preset,
                    plan.Recipe,
                    plan.Tuning,
                    out ignored);

                foreach (var node in placed.Graph.Decisions.Nodes)
                {
                    if (node.Type == NodeType.Enemy)
                    {
                        minted.Add(node.Value);
                    }
                }
            }

            return minted;
        }
    }
}
