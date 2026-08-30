using System;
using System.Collections.Generic;
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
        public void TheRampRunsSmallToLargeWithoutDoublingBackOnItself()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(Look(tier).Scale, Is.GreaterThan(Look(tier - 1).Scale));
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

        [Test]
        public void TheLookCarriesTheGuiseWeaponAndCloakItsTierIsDressedIn()
        {
            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                Assert.That(Look(tier).Guise, Is.EqualTo(PlayerKit.GuiseOf(tier)));
                Assert.That(Look(tier).Weapon, Is.EqualTo(PlayerKit.WeaponOf(tier)));
                Assert.That(Look(tier).Cloak, Is.EqualTo(PlayerKit.CloakedAt(tier)));
            }
        }

        [Test]
        public void TheGuiseIsAPureFunctionOfPowerAndSweepsEveryThresholdBoundary()
        {
            Assert.That(PlayerLook.Of(37).Guise, Is.EqualTo(PlayerLook.Of(37).Guise));

            foreach (var threshold in PlayerTier.Thresholds)
            {
                var below = PlayerLook.Of(threshold - 1);
                var at = PlayerLook.Of(threshold);

                Assert.That(below.Guise, Is.EqualTo(PlayerKit.GuiseOf(below.Tier)));
                Assert.That(at.Guise, Is.EqualTo(PlayerKit.GuiseOf(at.Tier)));
                Assert.That(at.Tier, Is.EqualTo(below.Tier + 1));
            }

            for (var power = 1; power <= 490; power++)
            {
                Assert.That(
                    PlayerLook.Of(power).Guise,
                    Is.EqualTo(PlayerKit.GuiseOf(PlayerTier.Of(power))),
                    "power " + power);
            }
        }

        [Test]
        public void TheGuiseTakesPartInTheLooksEqualityHashAndStringTheWayTheWeaponDoes()
        {
            var seen = new Dictionary<PlayerGuise, PlayerLook>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var look = Look(tier);

                Assert.That(look.ToString(), Does.Contain(look.Guise.ToString()));
                Assert.That(look.ToString(), Does.Contain(look.Weapon.ToString()));
                Assert.That(look.GetHashCode(), Is.EqualTo(Look(tier).GetHashCode()));
                Assert.That(look, Is.EqualTo(Look(tier)));

                seen[look.Guise] = look;
            }

            var swapped = 0;

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                if (Look(tier).Guise == Look(tier - 1).Guise)
                {
                    continue;
                }

                swapped++;
                Assert.That(Look(tier), Is.Not.EqualTo(Look(tier - 1)));
                Assert.That(Look(tier).GetHashCode(), Is.Not.EqualTo(Look(tier - 1).GetHashCode()));
            }

            Assert.That(swapped, Is.EqualTo(seen.Count - 1));
            Assert.That(swapped, Is.GreaterThan(0));
        }

        [Test]
        public void EveryTierWearsAStateNoOtherTierWears()
        {
            var worn = new List<PlayerLook>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var look = Look(tier);

                Assert.That(worn, Has.No.Member(look));
                worn.Add(look);
            }

            Assert.That(worn.Count, Is.EqualTo(PlayerTier.Count));
            Assert.That(worn.Count, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void EveryPromotionSwapsAPropAndNotOnlyTheSize()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var below = Look(tier - 1);
                var here = Look(tier);

                Assert.That(
                    here.Weapon != below.Weapon
                    || here.Cloak != below.Cloak
                    || here.Trophies != below.Trophies,
                    Is.True,
                    "tier " + tier + " is nothing but a larger copy of the tier below it");
            }
        }

        [Test]
        public void EveryStateIsCrossedIntoOnAThresholdOfTheTierTableAndNowhereElse()
        {
            for (var index = 0; index < PlayerTier.Thresholds.Count; index++)
            {
                var threshold = PlayerTier.Thresholds[index];
                var before = PlayerLook.Of(threshold - 1);
                var after = PlayerLook.Of(threshold);

                Assert.That(before, Is.Not.EqualTo(after));
                Assert.That(after.Tier, Is.EqualTo(index + 1));
            }

            var swaps = 0;
            var previous = PlayerLook.Of(1);

            for (var power = 2; power <= 490; power++)
            {
                var look = PlayerLook.Of(power);

                if (!look.Equals(previous))
                {
                    Assert.That(PlayerTier.Thresholds, Has.Member(power));
                    swaps++;
                }

                previous = look;
            }

            Assert.That(swaps, Is.EqualTo(PlayerTier.Thresholds.Count));
        }

        [Test]
        public void TheLookNamesNoColourForAnythingToPaintThePlayerWith()
        {
            foreach (var property in typeof(PlayerLook).GetProperties())
            {
                Assert.That(property.PropertyType, Is.Not.EqualTo(typeof(Tint)));
            }

            var source = SourceTree.Read("Presentation.Pure", "PlayerLook.cs");

            Assert.That(source, Does.Not.Contain("Tint"));
        }

        static PlayerLook Look(int tier)
        {
            var power = tier == 0 ? 1 : PlayerTier.Thresholds[tier - 1];
            return PlayerLook.Of(power);
        }
    }
}
