using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PlayerKitTests
    {
        [Test]
        public void EveryTierAboveTheFirstWieldsAWeaponOfItsOwnAndTheFirstIsEmptyHanded()
        {
            Assert.That(PlayerKit.WeaponOf(0), Is.EqualTo(PlayerWeapon.None));

            var seen = new List<PlayerWeapon>();

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);

                Assert.That(weapon, Is.Not.EqualTo(PlayerWeapon.None));
                Assert.That(seen, Has.No.Member(weapon));
                seen.Add(weapon);
            }

            Assert.That(seen.Count, Is.EqualTo(PlayerTier.Count - 1));
        }

        [Test]
        public void TheKitHandsOutThreeOrFourSwappableWeaponsAndNoMore()
        {
            var weapons = new List<PlayerWeapon>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);

                if (weapon != PlayerWeapon.None && !weapons.Contains(weapon))
                {
                    weapons.Add(weapon);
                }
            }

            Assert.That(weapons.Count, Is.InRange(3, 4));
        }

        [Test]
        public void TheCloakGoesOnAtOneThresholdAndNeverComesOffAgain()
        {
            Assert.That(PlayerKit.CloakedAt(0), Is.False);

            var thrown = 0;

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(PlayerKit.CloakedAt(tier), Is.True);

                if (!PlayerKit.CloakedAt(tier) && PlayerKit.CloakedAt(tier - 1))
                {
                    thrown++;
                }
            }

            Assert.That(thrown, Is.EqualTo(0));
            Assert.That(PlayerKit.CloakedAt(PlayerKit.CloakFrom), Is.True);
            Assert.That(PlayerKit.CloakedAt(PlayerKit.CloakFrom - 1), Is.False);
        }

        [Test]
        public void NoTierOutsideTheTableIsDressed()
        {
            Assert.That(() => PlayerKit.WeaponOf(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => PlayerKit.WeaponOf(PlayerTier.Count), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => PlayerKit.CloakedAt(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => PlayerKit.CloakedAt(PlayerTier.Count), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void AnEmptyHandCarriesNoLimbsAndEveryWeaponCarriesSome()
        {
            Assert.That(PlayerKit.LimbsOf(PlayerWeapon.None).Count, Is.EqualTo(0));

            foreach (PlayerWeapon weapon in Enum.GetValues(typeof(PlayerWeapon)))
            {
                if (weapon == PlayerWeapon.None)
                {
                    continue;
                }

                Assert.That(PlayerKit.LimbsOf(weapon).Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void NoTwoWeaponsShareBothTheirReachAndTheirBreadth()
        {
            var tips = new List<float>();
            var breadths = new List<float>();

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);
                var tip = PlayerKit.TipOf(weapon);
                var breadth = PlayerKit.BreadthOf(weapon);

                for (var other = 0; other < tips.Count; other++)
                {
                    Assert.That(
                        Math.Abs(tips[other] - tip) > 0.1f || Math.Abs(breadths[other] - breadth) > 0.1f,
                        Is.True,
                        weapon + " reads the same as the weapon of tier " + (other + 1));
                }

                tips.Add(tip);
                breadths.Add(breadth);
            }
        }

        [Test]
        public void EveryWeaponReachesPastTheHandThatHoldsIt()
        {
            Assert.That(PlayerKit.TipOf(PlayerWeapon.None), Is.EqualTo(0f));

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(
                    PlayerKit.TipOf(PlayerKit.WeaponOf(tier)), Is.GreaterThan(PlayerKit.GripHeight));
            }
        }

        [Test]
        public void EveryWeaponReachesFurtherAboveTheHeadThanTheOneTheTierBelowSwungIt()
        {
            var standing = FigureFit.StandingScalesOf(PartModel.Knight);
            var reached = PlayerKit.GripHeight;

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var tip = PlayerKit.TipOf(PlayerKit.WeaponOf(tier));

                Assert.That(tip, Is.GreaterThan(reached));
                Assert.That(tip, Is.GreaterThan(standing));
                reached = tip;
            }
        }

        [Test]
        public void TheHandTheWeaponsHangFromSitsInsideTheBodyThatSwingsThem()
        {
            var standing = FigureFit.StandingScalesOf(PartModel.Knight);

            Assert.That(PlayerKit.GripHeight, Is.GreaterThan(standing * 0.4f));
            Assert.That(PlayerKit.GripHeight, Is.LessThan(standing * 0.8f));
        }

        [Test]
        public void TheCloakHangsBehindTheBodyRatherThanThroughIt()
        {
            var limbs = PlayerKit.CloakLimbs;

            Assert.That(limbs.Count, Is.GreaterThan(0));

            for (var limb = 0; limb < limbs.Count; limb++)
            {
                Assert.That(limbs[limb].Offset.Z, Is.LessThan(0f));
                Assert.That(limbs[limb].Foot, Is.GreaterThan(0f));
                Assert.That(limbs[limb].Top, Is.LessThan(FigureFit.StandingScalesOf(PartModel.Knight)));
            }
        }

        [Test]
        public void ARotatedLimbMeasuresTheBoxItActuallyOccupiesRatherThanTheOneItWasCutFrom()
        {
            var upright = new PropLimb(
                new WorldPoint(0.2f, 2f, 0.2f), new WorldPoint(0f, 1f, 0f), new WorldPoint(0f, 0f, 0f));
            var flat = new PropLimb(
                new WorldPoint(0.2f, 2f, 0.2f), new WorldPoint(0f, 1f, 0f), new WorldPoint(0f, 0f, 90f));

            Assert.That(upright.Top, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(flat.Top, Is.EqualTo(1.1f).Within(1e-4f));
            Assert.That(flat.Reach, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void NoLimbOfAnyPropSinksBelowTheGroundItsFigureStandsOn()
        {
            foreach (PlayerWeapon weapon in Enum.GetValues(typeof(PlayerWeapon)))
            {
                var limbs = PlayerKit.LimbsOf(weapon);

                for (var limb = 0; limb < limbs.Count; limb++)
                {
                    Assert.That(
                        PlayerKit.GripHeight + limbs[limb].Foot,
                        Is.GreaterThan(0f),
                        weapon + " limb " + limb + " is planted underground");
                }
            }
        }

        [Test]
        public void EveryPlantedPropIsNamedSoAStripAndALeakScanBothKnowItIsOurs()
        {
            Assert.That(PartNames.IsWorn(PartNames.Cloak), Is.True);
            Assert.That(PartNames.IsWorn(PartNames.Trophy(0)), Is.True);
            Assert.That(PartNames.IsWorn(PartNames.Limb(PartNames.Cloak, 0)), Is.True);

            var named = new List<string>();

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var name = PartNames.Held(PlayerKit.WeaponOf(tier));

                Assert.That(PartNames.IsWorn(name), Is.True);
                Assert.That(named, Has.No.Member(name));
                named.Add(name);
            }

            Assert.That(PartNames.IsWorn(PartNames.Weapon), Is.False);
            Assert.That(PartNames.IsWorn(PartNames.Node(3)), Is.False);
            Assert.That(PartNames.IsWorn(PartNames.Badge(3)), Is.False);
            Assert.That(PartNames.IsWorn(null), Is.False);
            Assert.That(
                () => PartNames.Held(PlayerWeapon.None), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EveryPropStaysASteelOrClothPrimitiveRatherThanAColourLaidOverTheBody()
        {
            Assert.That(PlayerKit.Steel, Is.EqualTo(Trophy.Steel));
            Assert.That(PlayerKit.Cloth, Is.Not.EqualTo(PlayerKit.Steel));
        }
    }
}
