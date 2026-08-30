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
        public void TheCloakGoesOnAtOneThresholdAndOnlyOnAGuiseThatOwnsACape()
        {
            Assert.That(PlayerKit.CloakedAt(0), Is.False);
            Assert.That(PlayerKit.CloakedAt(PlayerKit.CloakFrom - 1), Is.False);

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var guise = PlayerKit.GuiseOf(tier);

                Assert.That(
                    PlayerKit.CloakedAt(tier),
                    Is.EqualTo(tier >= PlayerKit.CloakFrom && PlayerGuises.Drapes(guise)),
                    "tier " + tier + " as the " + guise);
            }
        }

        [Test]
        public void AGuiseThatOwnsNoCapeNeverDrapesAndNeverReportsItselfCloaked()
        {
            Assert.That(PlayerGuises.WearsACape(null), Is.False);
            Assert.That(PlayerGuises.WearsACape(string.Empty), Is.False);

            var bare = 0;

            for (var tier = PlayerKit.CloakFrom; tier < PlayerTier.Count; tier++)
            {
                if (PlayerGuises.Drapes(PlayerKit.GuiseOf(tier)))
                {
                    continue;
                }

                bare++;
                Assert.That(PlayerKit.CloakedAt(tier), Is.False);
                Assert.That(PlayerGuises.WearsACape(PlayerKit.CapeOf(tier)), Is.False);
            }

            Assert.That(bare, Is.EqualTo(0), "every adventurer this pack ships wears a cape of its own");
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
        public void AnEmptyHandHangsNoMeshAndEveryWeaponNamesOneAPackShipsWithTheCast()
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

                Assert.That(ArtPacks.ShipsWithTheCast(model), Is.True, weapon.ToString());
                Assert.That(ArtPacks.IsRiggedCharacter(model), Is.False, weapon.ToString());
                Assert.That(mounted, Has.No.Member(model));
                mounted.Add(model);
            }
        }

        [Test]
        public void NoWeaponHangsTheMeshTheBodyItselfIsCutFrom()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                foreach (var guise in PlayerGuises.All)
                {
                    Assert.That(
                        PlayerKit.ModelOf(PlayerKit.WeaponOf(tier)),
                        Is.Not.EqualTo(PlayerKit.BodyOf(guise)));
                }
            }

            foreach (var guise in PlayerGuises.All)
            {
                var body = PlayerKit.BodyOf(guise);

                Assert.That(ArtPacks.IsRiggedCharacter(body), Is.True, guise.ToString());
                Assert.That(AdventurerPack.Carries(body), Is.True, guise.ToString());
            }
        }

        [Test]
        public void EveryWeaponIsABiggerObjectThanTheOneTheTierBelowSwung()
        {
            Assert.That(PlayerKit.ReachOf(PlayerWeapon.None), Is.EqualTo(0f));

            var smallest = PlayerKit.ReachOf(PlayerKit.WeaponOf(1));
            var reached = 0f;
            var handed = 0f;

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);
                var reach = PlayerKit.ReachOf(weapon);

                if (reach < reached)
                {
                    handed = Math.Max(handed, (reached - reach) / reached);
                }

                reached = reach;
            }

            Assert.That(handed, Is.LessThan(0.02f));
            Assert.That(reached, Is.GreaterThan(smallest * PlayerLook.Growth));
        }

        [Test]
        public void TheTopRungSwingsTheLongestMeshTheRampHandsOutAndTheBodyUnderItDecidesTheRest()
        {
            var top = PlayerKit.ModelOf(PlayerKit.WeaponOf(PlayerTier.Count - 1));
            var longest = Diagonal(top);

            for (var tier = 1; tier < PlayerTier.Count - 1; tier++)
            {
                Assert.That(
                    Diagonal(PlayerKit.ModelOf(PlayerKit.WeaponOf(tier))),
                    Is.LessThan(longest),
                    PlayerKit.WeaponOf(tier).ToString());
            }

            var tallest = PlayerGuise.Knight;

            foreach (var guise in PlayerGuises.All)
            {
                if (ArtPacks.PackHeightOf(PlayerKit.BodyOf(guise))
                    > ArtPacks.PackHeightOf(PlayerKit.BodyOf(tallest)))
                {
                    tallest = guise;
                }
            }

            Assert.That(tallest, Is.EqualTo(PlayerKit.GuiseOf(PlayerTier.Count - 1)));

            foreach (var guise in PlayerGuises.All)
            {
                Assert.That(
                    PlayerKit.StandingPerImportUnitOf(guise),
                    Is.GreaterThanOrEqualTo(PlayerKit.StandingPerImportUnitOf(tallest)),
                    guise.ToString());
            }
        }

        static float Diagonal(PartModel model)
        {
            var width = ArtPacks.PackWidthOf(model);
            var height = ArtPacks.PackHeightOf(model);
            var depth = ArtPacks.PackDepthOf(model);

            return (float)Math.Sqrt(width * width + height * height + depth * depth);
        }

        [Test]
        public void EveryWeaponIsMeasuredOffItsOwnPackFootprintRatherThanAShapeTheKitInvented()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);
                var model = PlayerKit.ModelOf(weapon);
                var width = ArtPacks.WidthOf(model);
                var height = ArtPacks.HeightOf(model);
                var depth = ArtPacks.DepthOf(model);

                Assert.That(
                    PlayerKit.ReachOf(weapon),
                    Is.EqualTo(PlayerKit.StandingPerImportUnitOf(PlayerKit.GuiseOf(tier))
                        * (float)Math.Sqrt(width * width + height * height + depth * depth))
                        .Within(1e-5f),
                    weapon.ToString());
                Assert.That(
                    width,
                    Is.EqualTo(ArtPacks.PackWidthOf(model)
                        * ArtPacks.ImportScaleOf(ArtPacks.Of(model))).Within(1e-6f),
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
                    PlayerKit.TipOf(PlayerKit.WeaponOf(tier)),
                    Is.GreaterThan(PlayerKit.GripHeightOf(PlayerKit.GuiseOf(tier))));
            }
        }

        [Test]
        public void NoWeaponTipsHigherThanTheHeadThePackHangsItBeside()
        {
            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var standing = FigureFit.StandingScalesOf(PlayerKit.BodyOf(PlayerKit.GuiseOf(tier)));

                Assert.That(PlayerKit.TipOf(PlayerKit.WeaponOf(tier)), Is.LessThan(standing));
            }
        }

        [Test]
        public void TheHandTheWeaponsHangFromSitsInsideTheBodyThatSwingsThem()
        {
            foreach (var guise in PlayerGuises.All)
            {
                var standing = FigureFit.StandingScalesOf(PlayerKit.BodyOf(guise));

                Assert.That(PlayerKit.GripHeightOf(guise), Is.GreaterThan(standing / 3f), guise.ToString());
                Assert.That(PlayerKit.GripHeightOf(guise), Is.LessThan(standing * 0.5f), guise.ToString());
            }
        }

        [Test]
        public void EveryGuiseGripsItsWeaponWhereItsOwnBodyCarriesTheHandSlot()
        {
            var rigged = PlayerKit.GripHeightOf(PlayerGuise.Knight)
                * ArtPacks.HeightOf(PlayerKit.BodyOf(PlayerGuise.Knight));

            foreach (var guise in PlayerGuises.All)
            {
                Assert.That(
                    PlayerKit.GripHeightOf(guise) * ArtPacks.HeightOf(PlayerKit.BodyOf(guise)),
                    Is.EqualTo(rigged).Within(1e-4f),
                    guise.ToString());
            }

            Assert.That(
                () => PlayerKit.GripHeightOf((PlayerGuise)99),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheCloakIsTheClothTheGuisesOwnPackAlreadyBoltsToTheBodyItDressses()
        {
            Assert.That(PlayerKit.CapeOf(0), Is.EqualTo(AdventurerPack.KnightCloakNode));

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var cape = PlayerKit.CapeOf(tier);

                Assert.That(cape, Is.EqualTo(PlayerGuises.CapeOf(PlayerKit.GuiseOf(tier))));
                Assert.That(cape, Is.Not.EqualTo(AdventurerPack.SlotNode));

                if (PlayerGuises.WearsACape(cape))
                {
                    Assert.That(cape, Does.StartWith(PlayerKit.BodyOf(PlayerKit.GuiseOf(tier)).ToString()));
                }
            }
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
                var mounted = PlayerKit.ModelOf(PlayerKit.WeaponOf(tier));

                Assert.That(
                    AdventurerPack.Wields(mounted) || WeaponsPack.Wields(mounted),
                    Is.True,
                    mounted.ToString());
            }

            Assert.That(PlayerKit.CapeOf(PlayerKit.CloakFrom), Is.Not.Empty);
        }
    }
}
