using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class VisualTierTests
    {
        static readonly int[] Boundaries = { 8, 30, 100, 300 };

        [Test]
        public void TheMeasuredBoundariesAreTheOnesTheTierFunctionUses()
        {
            Assert.That(VisualTier.Thresholds, Is.EqualTo(Boundaries));
            Assert.That(VisualTier.Count, Is.EqualTo(Boundaries.Length + 1));
        }

        [Test]
        public void EachBoundaryPromotesOnItsOwnValueAndNotOneBelow()
        {
            for (var index = 0; index < Boundaries.Length; index++)
            {
                Assert.That(VisualTier.Of(Boundaries[index] - 1), Is.EqualTo(index));
                Assert.That(VisualTier.Of(Boundaries[index]), Is.EqualTo(index + 1));
            }
        }

        [Test]
        public void TheStartingPowerSitsAtTheBottomTierAndABeatenBossAtTheTop()
        {
            Assert.That(VisualTier.Of(2), Is.EqualTo(0));
            Assert.That(VisualTier.Of(7), Is.EqualTo(0));
            Assert.That(VisualTier.Of(408), Is.EqualTo(VisualTier.Count - 1));
            Assert.That(VisualTier.Of(490), Is.EqualTo(VisualTier.Count - 1));
        }

        [Test]
        public void TheTopTierIsOpenEndedSoNoPowerFallsOffTheTable()
        {
            Assert.That(VisualTier.Of(int.MaxValue), Is.EqualTo(VisualTier.Count - 1));
        }

        [Test]
        public void TierNeverFallsAsPowerRises()
        {
            var previous = 0;
            for (var power = 1; power <= 1000; power++)
            {
                var tier = VisualTier.Of(power);
                Assert.That(tier, Is.GreaterThanOrEqualTo(previous));
                Assert.That(tier, Is.InRange(0, VisualTier.Count - 1));
                previous = tier;
            }
        }

        [Test]
        public void APowerNoRunCanHoldIsRefusedRatherThanBanded()
        {
            Assert.That(() => VisualTier.Of(0), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => VisualTier.Of(-1), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void EveryTierIsReachedOnTheOnlyCurvePresentationEverBuilds()
        {
            var reached = new HashSet<int>();
            foreach (var power in Walk(PowerTuning.Ship.StartingPower))
            {
                reached.Add(VisualTier.Of(power));
            }

            Assert.That(reached.Count, Is.EqualTo(VisualTier.Count));
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
