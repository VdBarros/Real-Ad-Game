using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PlayerGuiseTests
    {
        [Test]
        public void EveryGuiseResolvesToARiggedBodyOfItsOwn()
        {
            var meshes = new List<PartModel>();

            foreach (var guise in PlayerGuises.All)
            {
                var mesh = PlayerGuises.MeshOf(guise);

                Assert.That(mesh, Is.Not.EqualTo(PartModel.None), guise.ToString());
                Assert.That(ArtPacks.IsRiggedCharacter(mesh), Is.True, guise.ToString());
                Assert.That(ArtPacks.Of(mesh), Is.EqualTo(ArtPack.Adventurers), guise.ToString());
                Assert.That(meshes, Has.No.Member(mesh));
                meshes.Add(mesh);
            }

            Assert.That(meshes.Count, Is.EqualTo(PlayerGuises.Count));
            Assert.That(PlayerGuises.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void EveryGuiseCarriesItsOwnAtlasScaleAndStandingHeight()
        {
            foreach (var guise in PlayerGuises.All)
            {
                var mesh = PlayerGuises.MeshOf(guise);

                Assert.That(
                    FigureFit.StandingScalesOf(mesh),
                    Is.EqualTo(AdventurerPack.StandingScales).Within(1e-6f),
                    guise.ToString());
                Assert.That(
                    ArtPacks.ImportScaleFor(mesh),
                    Is.EqualTo(ArtPacks.CastImportScale).Within(1e-6f),
                    guise.ToString());
                Assert.That(ArtPacks.HeightOf(mesh), Is.GreaterThan(0f), guise.ToString());
            }
        }

        [Test]
        public void ACapeIsAThingAGuiseMayOrMayNotOwnAndDrapingFollowsThatAndNothingElse()
        {
            foreach (var guise in PlayerGuises.All)
            {
                var cape = PlayerGuises.CapeOf(guise);

                Assert.That(
                    PlayerGuises.Drapes(guise),
                    Is.EqualTo(PlayerGuises.WearsACape(cape)),
                    guise.ToString());

                if (PlayerGuises.Drapes(guise))
                {
                    Assert.That(cape, Does.StartWith(PlayerGuises.MeshOf(guise).ToString()));
                    Assert.That(cape, Is.Not.EqualTo(AdventurerPack.SlotNode));
                }
            }

            Assert.That(PlayerGuises.WearsACape(null), Is.False);
            Assert.That(PlayerGuises.WearsACape(string.Empty), Is.False);
            Assert.That(PlayerGuises.WearsACape(AdventurerPack.KnightCloakNode), Is.True);
        }

        [Test]
        public void EveryGuiseFinishesWithABlowOfItsOwnAndAnEmptyHandAlwaysKicks()
        {
            var acts = new List<FigureAct>();

            foreach (var guise in PlayerGuises.All)
            {
                var act = PlayerGuises.FinisherOf(guise);

                Assert.That(act, Is.Not.EqualTo(FigureAct.Kick), guise.ToString());
                Assert.That(AdventurerClips.Loops(act), Is.False, guise.ToString());
                Assert.That(acts, Has.No.Member(act), guise.ToString());
                acts.Add(act);

                Assert.That(
                    FigureCues.FinisherOf(guise, PlayerWeapon.None),
                    Is.EqualTo(FigureAct.Kick),
                    guise.ToString());
            }

            Assert.That(acts.Count, Is.EqualTo(PlayerGuises.Count));
        }

        [Test]
        public void TheFinisherAWeaponSwingsIsTheOneItsOwnGuiseSwings()
        {
            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);
                var guise = PlayerKit.GuiseOf(tier);

                Assert.That(
                    FigureCues.FinisherOf(weapon),
                    Is.EqualTo(FigureCues.FinisherOf(guise, weapon)),
                    "tier " + tier);

                Assert.That(
                    FigureCues.FinisherOf(weapon),
                    Is.EqualTo(weapon == PlayerWeapon.None
                        ? FigureAct.Kick
                        : PlayerGuises.FinisherOf(guise)),
                    "tier " + tier);
            }
        }

        [Test]
        public void NoGuiseOutsideTheCastAnswersAnything()
        {
            var stranger = (PlayerGuise)99;

            Assert.That(PlayerGuises.IsGuise(stranger), Is.False);
            Assert.That(() => PlayerGuises.MeshOf(stranger), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => PlayerGuises.CapeOf(stranger), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => PlayerGuises.FinisherOf(stranger), Throws.InstanceOf<ArgumentOutOfRangeException>());

            foreach (var guise in PlayerGuises.All)
            {
                Assert.That(PlayerGuises.IsGuise(guise), Is.True, guise.ToString());
            }
        }

        [Test]
        public void TheRampChangesGuiseAtTwoPowersAndTheKnightOpensTheRun()
        {
            Assert.That(PlayerKit.GuiseOf(0), Is.EqualTo(PlayerGuise.Knight));
            Assert.That(PlayerKit.GuiseOf(1), Is.EqualTo(PlayerGuise.Knight));
            Assert.That(PlayerKit.GuiseOf(2), Is.EqualTo(PlayerGuise.Barbarian));
            Assert.That(PlayerKit.WeaponOf(2), Is.EqualTo(PlayerWeapon.Axe));
            Assert.That(PlayerKit.GuiseOf(3), Is.EqualTo(PlayerGuise.Rogue));
            Assert.That(PlayerKit.WeaponOf(3), Is.EqualTo(PlayerWeapon.Bow));
            Assert.That(PlayerKit.Body, Is.EqualTo(PlayerGuises.MeshOf(PlayerGuise.Knight)));

            var swaps = new List<int>();

            for (var power = 2; power <= 490; power++)
            {
                if (PlayerLook.Of(power).Guise != PlayerLook.Of(power - 1).Guise)
                {
                    swaps.Add(power);
                }
            }

            Assert.That(swaps.Count, Is.EqualTo(2));
            Assert.That(swaps[0], Is.EqualTo(PlayerTier.Thresholds[1]));
            Assert.That(swaps[0], Is.EqualTo(30));
            Assert.That(swaps[1], Is.EqualTo(PlayerTier.Thresholds[2]));
            Assert.That(swaps[1], Is.EqualTo(100));
        }

        [Test]
        public void NoPromotionEverPutsTheHeroBackIntoABodyItHasAlreadyLeft()
        {
            var worn = new List<PlayerGuise>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var guise = PlayerKit.GuiseOf(tier);

                if (tier > 0 && guise == PlayerKit.GuiseOf(tier - 1))
                {
                    continue;
                }

                Assert.That(worn, Has.No.Member(guise), "tier " + tier);
                worn.Add(guise);
            }

            Assert.That(worn.Count, Is.EqualTo(PlayerGuises.Count));
        }

        [Test]
        public void TheRogueFinishesWithARangedActNoOtherGuiseSwingsAndNoArrowEverLeavesTheBow()
        {
            Assert.That(PlayerGuises.FinisherOf(PlayerGuise.Rogue), Is.EqualTo(FigureAct.Loose));
            Assert.That(AdventurerClips.NameOf(FigureAct.Loose), Is.EqualTo(AdventurerClips.Loose));
            Assert.That(
                AnimationSets.SetOf(FigureAct.Loose), Is.EqualTo(AnimationSets.CombatRanged));

            foreach (var guise in PlayerGuises.All)
            {
                if (guise == PlayerGuise.Rogue)
                {
                    continue;
                }

                Assert.That(
                    PlayerGuises.FinisherOf(guise),
                    Is.Not.EqualTo(FigureAct.Loose),
                    guise.ToString());
                Assert.That(
                    AnimationSets.SetOf(PlayerGuises.FinisherOf(guise)),
                    Is.EqualTo(AnimationSets.CombatMelee),
                    guise.ToString());
            }

            foreach (var outcome in new[] { ActionOutcome.Win, ActionOutcome.Tie, ActionOutcome.Loss })
            {
                var fight = Fight.Of(outcome).Advanced(Fight.Of(outcome).ContactAt);
                var loosed = FigureCues.Striking(fight, PlayerWeapon.Bow);
                var chopped = FigureCues.Striking(fight, PlayerWeapon.Axe);

                Assert.That(loosed.Beat, Is.EqualTo(chopped.Beat), outcome.ToString());
                Assert.That(loosed.Loops, Is.EqualTo(chopped.Loops), outcome.ToString());
                Assert.That(fight.Seconds, Is.EqualTo(Fight.Of(outcome).Seconds), outcome.ToString());
            }

            Assert.That(
                SourceTree.Read("Presentation.Pure", "FigureCues.cs"),
                Does.Not.Contain("Flight"));
            Assert.That(
                SourceTree.Read("Presentation.Pure", "PlayerGuises.cs"),
                Does.Not.Contain("Flight"));
        }

        [Test]
        public void EveryWeaponOnTheRampIsHeldByExactlyOneGuise()
        {
            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);

                Assert.That(
                    PlayerKit.GuiseHolding(weapon),
                    Is.EqualTo(PlayerKit.GuiseOf(tier)),
                    "tier " + tier);
            }

            Assert.That(
                () => PlayerKit.GuiseHolding((PlayerWeapon)99),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheCastListsEveryGuiseTheRampCanDressThePlayerIn()
        {
            var worn = CharacterCast.MeshesOf(PartStyle.Start);

            foreach (var guise in PlayerGuises.All)
            {
                Assert.That(worn, Has.Member(PlayerGuises.MeshOf(guise)), guise.ToString());
            }

            Assert.That(worn.Count, Is.EqualTo(PlayerGuises.Count));
            Assert.That(worn, Has.Member(CharacterCast.MeshOf(PartStyle.Start)));
        }

        [Test]
        public void AGuiseIsNotOneOfTheCastRoles()
        {
            foreach (var role in CharacterCast.Roles)
            {
                Assert.That(Enum.IsDefined(typeof(PartStyle), role), Is.True, role.ToString());
            }

            Assert.That(typeof(PlayerGuise), Is.Not.EqualTo(typeof(PartStyle)));
            Assert.That(CharacterCast.Roles.Count, Is.Not.EqualTo(0));

            var source = SourceTree.Read("Presentation.Pure", "PlayerGuises.cs");

            Assert.That(source, Does.Not.Contain("PartStyle"));
        }
    }
}
