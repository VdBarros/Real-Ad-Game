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
        public void AnEmptyHandHangsNoMeshAndEveryWeaponNamesOneTheAdventurersPackShips()
        {
            Assert.That(
                () => PlayerKit.ModelOf(PlayerWeapon.None),
                Throws.InstanceOf<ArgumentOutOfRangeException>());

            var mounted = new List<PartModel>();

            foreach (PlayerWeapon weapon in Enum.GetValues(typeof(PlayerWeapon)))
            {
                if (weapon == PlayerWeapon.None)
                {
                    continue;
                }

                var model = PlayerKit.ModelOf(weapon);

                Assert.That(AdventurerPack.Wields(model), Is.True, weapon.ToString());
                Assert.That(ArtPacks.Of(model), Is.EqualTo(ArtPack.Adventurers), weapon.ToString());
                Assert.That(mounted, Has.No.Member(model));
                mounted.Add(model);
            }
        }

        [Test]
        public void NoWeaponHangsTheMeshTheBodyItselfIsCutFrom()
        {
            var body = CharacterCast.MeshOf(PartStyle.Start);

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(PlayerKit.ModelOf(PlayerKit.WeaponOf(tier)), Is.Not.EqualTo(body));
            }

            Assert.That(AdventurerPack.Wields(body), Is.False);
            Assert.That(AdventurerPack.Carries(body), Is.True);
        }

        [Test]
        public void EveryWeaponIsABiggerObjectThanTheOneTheTierBelowSwung()
        {
            Assert.That(PlayerKit.ReachOf(PlayerWeapon.None), Is.EqualTo(0f));

            var smallest = PlayerKit.ReachOf(PlayerKit.WeaponOf(1));
            var reached = 0f;

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);
                var reach = PlayerKit.ReachOf(weapon);

                Assert.That(reach, Is.GreaterThan(reached), weapon.ToString());
                reached = reach;
            }

            Assert.That(reached, Is.GreaterThan(smallest * 4f / 3f));
        }

        [Test]
        public void EveryWeaponIsMeasuredOffItsOwnPackFootprintRatherThanAShapeTheKitInvented()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);
                var model = PlayerKit.ModelOf(weapon);
                var width = AdventurerPack.PackWidthOf(model);
                var height = AdventurerPack.PackHeightOf(model);
                var depth = AdventurerPack.PackDepthOf(model);

                Assert.That(
                    PlayerKit.ReachOf(weapon),
                    Is.EqualTo(AdventurerPack.StandingPerPackUnit
                        * (float)Math.Sqrt(width * width + height * height + depth * depth))
                        .Within(1e-5f),
                    weapon.ToString());
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
        public void NoWeaponTipsHigherThanTheHeadThePackHangsItBeside()
        {
            var standing = FigureFit.StandingScalesOf(PartModel.Knight);

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(PlayerKit.TipOf(PlayerKit.WeaponOf(tier)), Is.LessThan(standing));
            }
        }

        [Test]
        public void TheHandTheWeaponsHangFromSitsInsideTheBodyThatSwingsThem()
        {
            var standing = FigureFit.StandingScalesOf(PartModel.Knight);

            Assert.That(PlayerKit.GripHeight, Is.GreaterThan(standing / 3f));
            Assert.That(PlayerKit.GripHeight, Is.LessThan(standing * 0.5f));
        }

        [Test]
        public void TheCloakIsTheClothThePackAlreadyBoltsToTheBodyItDressses()
        {
            Assert.That(PlayerKit.CloakNode, Is.EqualTo(AdventurerPack.CloakNode));
            Assert.That(PlayerKit.CloakNode, Is.Not.Empty);
            Assert.That(PlayerKit.CloakNode, Does.StartWith(PartModel.Knight.ToString()));
            Assert.That(PlayerKit.CloakNode, Is.Not.EqualTo(AdventurerPack.SlotNode));
        }

        [Test]
        public void EveryWeaponSpansAWidthOfItsOwnAndNoneOfThemSpansNothing()
        {
            Assert.That(PlayerKit.BreadthOf(PlayerWeapon.None), Is.EqualTo(0f));

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(PlayerKit.BreadthOf(PlayerKit.WeaponOf(tier)), Is.GreaterThan(0f));
            }
        }

        [Test]
        public void EveryPlantedPropIsNamedSoAStripAndALeakScanBothKnowItIsOurs()
        {
            Assert.That(PartNames.IsWorn(PartNames.Trophy(0)), Is.True);

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
        public void NothingTheTierRampSwapsIsAPrimitiveWithAColourOfItsOwnAnyMore()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                Assert.That(AdventurerPack.Wields(PlayerKit.ModelOf(PlayerKit.WeaponOf(tier))), Is.True);
            }

            Assert.That(PlayerKit.CloakNode, Does.StartWith(PartModel.Knight.ToString()));
        }
    }
}
