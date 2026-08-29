using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PlayerLookTests
    {
        [Test]
        public void ALookIsNothingButAFunctionOfPower()
        {
            Assert.That(PlayerLook.Of(37), Is.EqualTo(PlayerLook.Of(37)));
            Assert.That(PlayerLook.Of(37), Is.Not.EqualTo(PlayerLook.Of(370)));
        }

        [Test]
        public void TheLookCarriesTheTierThePowerLandsIn()
        {
            foreach (var power in new[] { 2, 8, 29, 30, 99, 100, 299, 300, 490 })
            {
                Assert.That(PlayerLook.Of(power).Tier, Is.EqualTo(PlayerTier.Of(power)));
            }
        }

        [Test]
        public void EveryPromotionGrowsThePlayerByTheSameFifteenPercent()
        {
            Assert.That(Look(0).Scale, Is.EqualTo(PlayerLook.BaseScale).Within(1e-6f));

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(
                    Look(tier).Scale / Look(tier - 1).Scale,
                    Is.EqualTo(PlayerLook.Growth).Within(1e-5f));
            }
        }

        [Test]
        public void TheRampRunsCoolToWarmWithoutDoublingBackOnItself()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(Look(tier).Tint.Red, Is.GreaterThan(Look(tier - 1).Tint.Red));
                Assert.That(Look(tier).Tint.Blue, Is.LessThan(Look(tier - 1).Tint.Blue));
            }
        }

        [Test]
        public void OneTrophyAccumulatesPerTierAboveTheFirstTwoAndTheTopTierHoldsTheCap()
        {
            Assert.That(Look(0).Trophies, Is.EqualTo(0));
            Assert.That(Look(1).Trophies, Is.EqualTo(0));
            Assert.That(Look(2).Trophies, Is.EqualTo(1));
            Assert.That(Look(3).Trophies, Is.EqualTo(2));
            Assert.That(Look(PlayerTier.Count - 1).Trophies, Is.EqualTo(Trophy.Cap));
        }

        [Test]
        public void ThePromotionIntoTheBossTierStillPlantsAPrimitiveOfItsOwn()
        {
            var top = PlayerTier.Count - 1;

            Assert.That(Look(top).Trophies - Look(top - 1).Trophies, Is.EqualTo(1));
        }

        [Test]
        public void EveryTrophySlotStandsClearOfTheBodyAndOfEveryOtherSlot()
        {
            for (var slot = 0; slot < Trophy.Cap; slot++)
            {
                var position = Trophy.PositionOf(slot);
                Assert.That(
                    Math.Sqrt(position.X * position.X + position.Z * position.Z),
                    Is.EqualTo(Trophy.Reach).Within(1e-4));

                for (var other = 0; other < slot; other++)
                {
                    Assert.That(Trophy.PositionOf(other), Is.Not.EqualTo(position));
                }
            }
        }

        [Test]
        public void AFourthTrophyHasNowhereToHang()
        {
            Assert.That(() => Trophy.PositionOf(Trophy.Cap), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => Trophy.RotationOf(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        static PlayerLook Look(int tier)
        {
            var power = tier == 0 ? 1 : PlayerTier.Thresholds[tier - 1];
            return PlayerLook.Of(power);
        }
    }
}
