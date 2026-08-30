using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class WeaponFlightTests
    {
        const float Frame = 1f / 60f;

        static readonly WorldPoint Site = new WorldPoint(4f, 0.4f, 7f);

        static readonly WorldPoint Carrier = new WorldPoint(1f, 0.4f, 2f);

        [Test]
        public void NothingIsFlyingUntilAnEnemyDropsSomething()
        {
            Assert.That(WeaponFlight.None.IsSettled, Is.True);
            Assert.That(default(WeaponFlight).IsSettled, Is.True);
        }

        [Test]
        public void AWeaponLeavesTheDeathSiteAndArrivesOnThePlayer()
        {
            var flight = WeaponFlight.From(Site, Carrier);

            Assert.That(flight.Position, Is.EqualTo(Site));

            while (!flight.IsSettled)
            {
                flight = flight.Advanced(Frame);
            }

            Assert.That(flight.Position.X, Is.EqualTo(Carrier.X).Within(1e-5f));
            Assert.That(flight.Position.Y, Is.EqualTo(Carrier.Y).Within(1e-5f));
            Assert.That(flight.Position.Z, Is.EqualTo(Carrier.Z).Within(1e-5f));
        }

        [Test]
        public void TheWeaponArcsOverTheGroundRatherThanSlidingAlongIt()
        {
            var flight = WeaponFlight.From(Site, Carrier).Advanced(WeaponFlight.Seconds * 0.5f);

            Assert.That(flight.Position.Y, Is.EqualTo(Site.Y + WeaponFlight.Arc).Within(1e-4f));
            Assert.That(flight.Spin, Is.GreaterThan(0f));
        }

        [Test]
        public void AFlightLandsOnTheSameBeatAsThePromotionItMergesInto()
        {
            Assert.That(WeaponFlight.Seconds, Is.EqualTo(Promotion.Seconds));
            Assert.That(WeaponFlight.From(Site, Carrier).Advanced(WeaponFlight.Seconds).IsSettled, Is.True);
        }

        [Test]
        public void AFlightOnlyEverRunsForwards()
        {
            Assert.That(
                () => WeaponFlight.From(Site, Carrier).Advanced(-Frame),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheNumberLandingHandsTheDropTheLookTheHeroIsClimbingIntoAndNotTheOneHeIsLeaving()
        {
            var climbed = 0;

            for (var from = 0; from < PlayerTier.Count; from++)
            {
                for (var to = from + 1; to < PlayerTier.Count; to++)
                {
                    var opening = PowerAt(from);
                    var target = PowerAt(to);
                    var beat = PowerBeat.Begin(opening).Toward(target);

                    Assert.That(beat.HasLanded, Is.False, opening + " -> " + target);

                    while (!beat.HasLanded)
                    {
                        beat = beat.Advanced(Frame);
                    }

                    Assert.That(beat.Look, Is.EqualTo(PlayerLook.Of(target)));
                    Assert.That(beat.Look.Weapon, Is.EqualTo(PlayerKit.WeaponOf(to)));
                    Assert.That(beat.Look.Guise, Is.EqualTo(PlayerKit.GuiseOf(to)));

                    if (PlayerKit.WeaponOf(from) != PlayerKit.WeaponOf(to))
                    {
                        Assert.That(beat.Look.Weapon, Is.Not.EqualTo(PlayerKit.WeaponOf(from)));
                        climbed++;
                    }
                }
            }

            Assert.That(climbed, Is.GreaterThan(0));
        }

        [Test]
        public void TheDropAsksTheRampWhatTheTargetGripsRatherThanNamingAWeaponOfItsOwn()
        {
            Assert.That(
                SourceTree.Read("Presentation.Pure", "WeaponDrop.cs"), Does.Not.Contain("PartModel."));
            Assert.That(
                SourceTree.Read("Presentation", "PlayerFigure.cs"), Does.Not.Contain("PartModel."));

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var look = PlayerLook.Of(PowerAt(tier));

                if (!WeaponDrop.CarriesAMesh(look))
                {
                    continue;
                }

                Assert.That(WeaponDrop.ModelOf(look), Is.EqualTo(PlayerKit.ModelOf(look.Weapon)));
            }
        }

        [Test]
        public void AWeaponInTheAirIsTheSizeItWillBeInTheHand()
        {
            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var look = PlayerLook.Of(PowerAt(tier));

                if (!WeaponDrop.CarriesAMesh(look))
                {
                    continue;
                }

                var gripped = look.Scale * FigureFit.ScaleOf(PlayerKit.BodyOf(look.Guise));

                Assert.That(WeaponDrop.ScaleOf(look), Is.EqualTo(gripped).Within(1e-6f));
                Assert.That(
                    WeaponDrop.SpanOf(look),
                    Is.EqualTo(gripped * ImportDiagonalOf(WeaponDrop.ModelOf(look))).Within(1e-5f));
            }
        }

        [Test]
        public void EveryRungTheDropCanCarryReadsAgainstEverySurfaceItFliesOver()
        {
            var carried = 0;

            Console.WriteLine(
                "a dropped weapon needs a span of "
                + WeaponDrop.LeastSpanThatReads.ToString("0.#####")
                + " and a contrast of " + WorldTints.LeastSeparation.ToString("0.##"));

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var look = PlayerLook.Of(PowerAt(tier));

                if (!WeaponDrop.CarriesAMesh(look))
                {
                    Assert.That(
                        () => WeaponDrop.SpanOf(look),
                        Throws.InstanceOf<System.ArgumentOutOfRangeException>());
                    continue;
                }

                carried++;

                Console.WriteLine(
                    "  tier " + tier + " " + look.Guise + " " + look.Weapon
                    + " spans " + WeaponDrop.SpanOf(look).ToString("0.#####")
                    + " at scale " + WeaponDrop.ScaleOf(look).ToString("0.#####"));

                Assert.That(
                    WeaponDrop.IsBigEnoughToRead(look),
                    Is.True,
                    "tier " + tier + " spans " + WeaponDrop.SpanOf(look));

                foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
                {
                    if (WorldTints.LayerOf(style) != PartLayer.Surface)
                    {
                        continue;
                    }

                    var ground = WorldTints.Of(style);

                    Console.WriteLine(
                        "    against " + style + " it reads at "
                        + Tint.Contrast(WeaponDrop.Iron, ground).ToString("0.##"));

                    Assert.That(
                        WeaponDrop.ReadsAgainst(look, ground),
                        Is.True,
                        "tier " + tier + " over " + style);
                }
            }

            Assert.That(carried, Is.EqualTo(PlayerTier.Count - 1));
        }

        [Test]
        public void ARungThatGripsNothingHasNoMeshToDrop()
        {
            var empty = PlayerLook.Of(PowerAt(0));

            Assert.That(empty.Weapon, Is.EqualTo(PlayerWeapon.None));
            Assert.That(WeaponDrop.CarriesAMesh(empty), Is.False);
            Assert.That(
                () => WeaponDrop.ModelOf(empty),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(
                () => WeaponDrop.ScaleOf(empty),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        static float ImportDiagonalOf(PartModel model)
        {
            var width = ArtPacks.WidthOf(model);
            var height = ArtPacks.HeightOf(model);
            var depth = ArtPacks.DepthOf(model);

            return (float)Math.Sqrt(width * width + height * height + depth * depth);
        }

        static int PowerAt(int tier)
        {
            return tier == 0 ? 1 : PlayerTier.Thresholds[tier - 1];
        }
    }
}
