using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PlayerTierTests
    {
        static readonly int[] Boundaries = { 8, 30, 100, 300 };

        [Test]
        public void TheMeasuredBoundariesAreTheOnesTheTierFunctionUses()
        {
            Assert.That(PlayerTier.Thresholds, Is.EqualTo(Boundaries));
            Assert.That(PlayerTier.Count, Is.EqualTo(Boundaries.Length + 1));
        }

        [Test]
        public void EachBoundaryPromotesOnItsOwnValueAndNotOneBelow()
        {
            for (var index = 0; index < Boundaries.Length; index++)
            {
                Assert.That(PlayerTier.Of(Boundaries[index] - 1), Is.EqualTo(index));
                Assert.That(PlayerTier.Of(Boundaries[index]), Is.EqualTo(index + 1));
            }
        }

        [Test]
        public void TheStartingPowerSitsAtTheBottomTierAndABeatenBossAtTheTop()
        {
            Assert.That(PlayerTier.Of(2), Is.EqualTo(0));
            Assert.That(PlayerTier.Of(7), Is.EqualTo(0));
            Assert.That(PlayerTier.Of(408), Is.EqualTo(PlayerTier.Count - 1));
            Assert.That(PlayerTier.Of(490), Is.EqualTo(PlayerTier.Count - 1));
        }

        [Test]
        public void TheTopTierIsOpenEndedSoNoPowerFallsOffTheTable()
        {
            Assert.That(PlayerTier.Of(int.MaxValue), Is.EqualTo(PlayerTier.Count - 1));
        }

        [Test]
        public void TierNeverFallsAsPowerRises()
        {
            var previous = 0;
            for (var power = 1; power <= 1000; power++)
            {
                var tier = PlayerTier.Of(power);
                Assert.That(tier, Is.GreaterThanOrEqualTo(previous));
                Assert.That(tier, Is.InRange(0, PlayerTier.Count - 1));
                previous = tier;
            }
        }

        [Test]
        public void APowerNoRunCanHoldIsRefusedRatherThanBanded()
        {
            Assert.That(() => PlayerTier.Of(0), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => PlayerTier.Of(-1), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void EveryTierIsReachedOnTheOnlyCurvePresentationEverBuilds()
        {
            var reached = new HashSet<int>();
            foreach (var power in Walk(PowerTuning.Ship.StartingPower))
            {
                reached.Add(PlayerTier.Of(power));
            }

            Assert.That(reached.Count, Is.EqualTo(PlayerTier.Count));
        }

        [Test]
        public void ThePlayerTableIsItsOwnAndOwesNothingToTheEnemyOne()
        {
            Assert.That(PlayerTier.Thresholds, Is.Not.SameAs(EnemyTier.Thresholds));
        }

        static IEnumerable<int> Walk(int startingPower)
        {
            for (var power = startingPower; power <= 490; power++)
            {
                yield return power;
            }
        }
    }
}
