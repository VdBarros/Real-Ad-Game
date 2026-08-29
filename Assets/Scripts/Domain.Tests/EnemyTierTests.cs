using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class EnemyTierTests
    {
        static readonly int[] Boundaries = { 8, 30, 100, 300 };

        [Test]
        public void TheMeasuredBoundariesAreTheOnesTheTierFunctionUses()
        {
            Assert.That(EnemyTier.Thresholds, Is.EqualTo(Boundaries));
            Assert.That(EnemyTier.Count, Is.EqualTo(Boundaries.Length + 1));
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
            Assert.That(EnemyTier.Of(2), Is.EqualTo(0));
            Assert.That(EnemyTier.Of(7), Is.EqualTo(0));
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
        public void EveryTierIsReachedOnTheOnlyCurvePresentationEverBuilds()
        {
            var reached = new HashSet<int>();
            foreach (var number in Walk(PowerTuning.Ship.StartingPower))
            {
                reached.Add(EnemyTier.Of(number));
            }

            Assert.That(reached.Count, Is.EqualTo(EnemyTier.Count));
        }

        [Test]
        public void TheEnemyTableIsItsOwnAndOwesNothingToThePlayerOne()
        {
            Assert.That(EnemyTier.Thresholds, Is.Not.SameAs(PlayerTier.Thresholds));
        }

        static IEnumerable<int> Walk(int startingPower)
        {
            for (var number = startingPower; number <= 490; number++)
            {
                yield return number;
            }
        }
    }
}
