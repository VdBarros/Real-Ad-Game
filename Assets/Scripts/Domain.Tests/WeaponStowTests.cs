using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class WeaponStowTests
    {
        const float Frame = 1f / 60f;

        static Walk Stepped(Walk walk)
        {
            var moved = walk.Advanced(Frame);

            return moved.IsWaiting ? moved.Resumed() : moved;
        }

        [Test]
        public void TheWeaponGoesAwayWithinATileOfAGateAndRidesTheHandEverywhereElse()
        {
            var level = RunFixture.Level();
            var route = RunFixture.PastTheMultiplier();
            var gate = route.TileOf(1);
            var walk = Walk.Along(route);
            var away = 0;
            var drawn = 0;

            for (var frame = 0; frame < 2000 && !walk.IsSettled; frame++)
            {
                var stowed = WeaponStow.Away(level, walk, Fight.None);

                Assert.That(
                    stowed,
                    Is.EqualTo(Math.Abs(walk.Travelled - gate) <= WeaponStow.Tiles),
                    "At " + walk.Travelled + " tiles the stow disagreed with the distance to the gate.");

                if (stowed)
                {
                    away++;
                }
                else
                {
                    drawn++;
                }

                walk = Stepped(walk);
            }

            Assert.That(walk.IsSettled, Is.True);
            Assert.That(away, Is.GreaterThan(0));
            Assert.That(drawn, Is.GreaterThan(0));
        }

        [Test]
        public void ARouteThatPassesNoGateNeverPutsTheWeaponAway()
        {
            var level = RunFixture.Level();
            var walk = Walk.Along(
                TileRoute.Of(level, new[] { RunFixture.Start, RunFixture.DoorstepEnemy }));

            for (var frame = 0; frame < 2000 && !walk.IsSettled; frame++)
            {
                Assert.That(WeaponStow.Away(level, walk, Fight.None), Is.False);
                Assert.That(WeaponStow.StepsToAGate(level, walk), Is.EqualTo(float.MaxValue));

                walk = Stepped(walk);
            }

            Assert.That(walk.IsSettled, Is.True);
        }

        [Test]
        public void AFightThatOpensUnderTheArchItselfDrawsTheWeaponBackIntoTheHand()
        {
            var level = RunFixture.Level();
            var route = RunFixture.PastTheMultiplier();
            var walk = Walk.Along(route);

            for (var frame = 0; frame < 2000 && walk.Travelled < route.TileOf(1); frame++)
            {
                walk = Stepped(walk);
            }

            Assert.That(WeaponStow.StepsToAGate(level, walk), Is.LessThan(WeaponStow.Tiles));
            Assert.That(WeaponStow.Away(level, walk, Fight.None), Is.True);

            foreach (var outcome in new[] { ActionOutcome.Win, ActionOutcome.Tie, ActionOutcome.Loss })
            {
                var fight = Fight.Of(outcome);

                Assert.That(fight.IsJoined, Is.True);
                Assert.That(WeaponStow.Away(level, walk, fight), Is.False, outcome + " kept it stowed.");
                Assert.That(
                    FigureCues.FinisherOf(PlayerKit.WeaponOf(PlayerTier.Count - 1)),
                    Is.EqualTo(PlayerGuises.FinisherOf(PlayerKit.GuiseOf(PlayerTier.Count - 1))));
            }
        }

        [Test]
        public void NothingToWalkAndNoLevelToWalkItOnLeavesTheWeaponInTheHand()
        {
            Assert.That(WeaponStow.Away(null), Is.False);
            Assert.That(WeaponStow.Away(Journey.Nowhere), Is.False);
            Assert.That(WeaponStow.Away(null, Walk.Nowhere, Fight.None), Is.False);
            Assert.That(
                WeaponStow.StepsToAGate(RunFixture.Level(), Walk.Nowhere),
                Is.EqualTo(float.MaxValue));
        }

        [Test]
        public void AJourneyReadsTheSameStowItsOwnLevelAndWalkAndFightRead()
        {
            var journey = Journey.Toward(
                RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier);
            var agreed = 0;

            for (var frame = 0; frame < 4000 && !journey.IsOver; frame++)
            {
                Assert.That(
                    WeaponStow.Away(journey),
                    Is.EqualTo(WeaponStow.Away(journey.State.Level, journey.Walk, journey.Fight)));

                agreed++;
                journey = journey.IsWaiting ? journey.Resumed() : journey.Advanced(Frame);
            }

            Assert.That(agreed, Is.GreaterThan(0));
        }

        [Test]
        public void EveryWeaponTheRampHandsOutHangsClearOfTheArchOnceItIsStowed()
        {
            var hung = new List<PlayerWeapon>();

            for (var tier = 0; tier < PlayerTier.Count; tier++)
            {
                var weapon = PlayerKit.WeaponOf(tier);

                Assert.That(WeaponStow.ClearsTheArch(weapon), Is.True, weapon + " does not clear the arch.");

                if (weapon == PlayerWeapon.None)
                {
                    continue;
                }

                hung.Add(weapon);

                Assert.That(WeaponStow.FootOf(weapon), Is.GreaterThan(0f));
                Assert.That(WeaponStow.CrestOf(weapon), Is.LessThan(WeaponStow.Lintel));
                Assert.That(WeaponStow.AcrossOf(weapon), Is.LessThan(WeaponStow.Shoulders));
                Assert.That(
                    WeaponStow.AcrossOf(weapon), Is.LessThan(PlayerKit.BreadthOf(weapon)),
                    weapon + " is no narrower stowed than it was held out sideways.");
            }

            Assert.That(hung.Count, Is.EqualTo(PlayerTier.Count - 1));
        }

        [Test]
        public void NoWeaponTheRampHangsIsLongerThanTheHeroItHangsOn()
        {
            var body = ArtPacks.HeightOf(PlayerKit.Body);

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var model = PlayerKit.ModelOf(PlayerKit.WeaponOf(tier));

                Assert.That(ArtPacks.HeightOf(model), Is.LessThan(body), model.ToString());
            }
        }

        [Test]
        public void TheOneMeshThePackAuthorsLyingFlatOnlyClearsTheWalkwayStoodUpright()
        {
            var flat = 0;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!WeaponsPack.Carries(model))
                {
                    continue;
                }

                var authored = ArtPacks.WidthOf(model) * PlayerKit.StandingPerImportUnit;
                var mounted = ArtPacks.MountedWidthOf(model) * PlayerKit.StandingPerImportUnit;

                if (!WeaponsPack.LiesFlat(model))
                {
                    Assert.That(WeaponsPack.MountRollOf(model), Is.EqualTo(0f), model.ToString());
                    Assert.That(WeaponsPack.MountTurnOf(model), Is.EqualTo(0f), model.ToString());
                    Assert.That(authored, Is.LessThan(WeaponStow.Shoulders), model.ToString());
                    Assert.That(
                        ArtPacks.MountedWidthOf(model),
                        Is.EqualTo(ArtPacks.WidthOf(model)),
                        model.ToString());
                    Assert.That(
                        ArtPacks.MountedHeightOf(model),
                        Is.EqualTo(ArtPacks.HeightOf(model)),
                        model.ToString());
                    Assert.That(
                        ArtPacks.MountedDepthOf(model),
                        Is.EqualTo(ArtPacks.DepthOf(model)),
                        model.ToString());
                    Assert.That(
                        ArtPacks.MountedBaseOf(model),
                        Is.EqualTo(ArtPacks.BaseOf(model)),
                        model.ToString());
                    Assert.That(
                        () => WeaponsPack.PackLeftOf(model),
                        Throws.InstanceOf<ArgumentOutOfRangeException>(),
                        model.ToString());
                    continue;
                }

                flat++;

                Assert.That(WeaponsPack.MountRollOf(model), Is.EqualTo(WeaponsPack.UprightRoll));
                Assert.That(WeaponsPack.MountTurnOf(model), Is.EqualTo(WeaponsPack.UprightTurn));
                Assert.That(authored, Is.GreaterThan(WeaponStow.Shoulders), model.ToString());
                Assert.That(mounted, Is.LessThan(WeaponStow.Shoulders), model.ToString());
                Assert.That(
                    ArtPacks.HeightOf(model), Is.LessThan(ArtPacks.WidthOf(model)), model.ToString());

                Assert.That(ArtPacks.MountedHeightOf(model), Is.EqualTo(ArtPacks.WidthOf(model)));
                Assert.That(ArtPacks.MountedWidthOf(model), Is.EqualTo(ArtPacks.DepthOf(model)));
                Assert.That(ArtPacks.MountedDepthOf(model), Is.EqualTo(ArtPacks.HeightOf(model)));
                Assert.That(
                    ArtPacks.MountedBaseOf(model),
                    Is.EqualTo(WeaponsPack.PackLeftOf(model) * WeaponsPack.ImportScale).Within(1e-6f));
            }

            Assert.That(flat, Is.EqualTo(1));
        }

        [Test]
        public void TheMeshThePackAuthorsLyingFlatIsTheOnlyOneAMountTurnsAtAll()
        {
            var turned = 0;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None || !ArtPacks.ShipsWithTheCast(model))
                {
                    continue;
                }

                if (ArtPacks.MountRollOf(model) == 0f && ArtPacks.MountTurnOf(model) == 0f)
                {
                    continue;
                }

                turned++;
                Assert.That(WeaponsPack.LiesFlat(model), Is.True, model.ToString());
            }

            Assert.That(turned, Is.EqualTo(1));

            for (var tier = 1; tier < PlayerTier.Count; tier++)
            {
                var model = PlayerKit.ModelOf(PlayerKit.WeaponOf(tier));
                var diagonal = ArtPacks.MountedWidthOf(model) * ArtPacks.MountedWidthOf(model)
                    + ArtPacks.MountedHeightOf(model) * ArtPacks.MountedHeightOf(model)
                    + ArtPacks.MountedDepthOf(model) * ArtPacks.MountedDepthOf(model);
                var authored = ArtPacks.WidthOf(model) * ArtPacks.WidthOf(model)
                    + ArtPacks.HeightOf(model) * ArtPacks.HeightOf(model)
                    + ArtPacks.DepthOf(model) * ArtPacks.DepthOf(model);

                Assert.That(diagonal, Is.EqualTo(authored).Within(1e-6f), model.ToString());
            }
        }

        [Test]
        public void AStowedWeaponRidesTheSpineAtHalfTheFigureAndPointsStraightUp()
        {
            foreach (var weapon in PlayerKit.Weapons)
            {
                if (weapon == PlayerWeapon.None)
                {
                    continue;
                }

                var middle = (WeaponStow.CrestOf(weapon) + WeaponStow.FootOf(weapon)) * 0.5f;

                Assert.That(middle, Is.EqualTo(WeaponStow.Ride).Within(1e-4f));
            }

            foreach (var restYaw in new[] { 0f, 45f, 90f, 225f, 315f })
            {
                var pose = WeaponStow.PoseOf(PlayerWeapon.Greatsword, restYaw);
                var reach = (float)Math.Sqrt(pose.X * pose.X + pose.Z * pose.Z);

                Assert.That(reach, Is.EqualTo(WeaponStow.Back).Within(1e-4f));
                Assert.That(pose.Y, Is.EqualTo(WeaponStow.LiftOf(PlayerWeapon.Greatsword)).Within(1e-6f));
            }
        }

        [Test]
        public void TheTrophiesPullInsideTheShouldersTheyOtherwiseStandOutside()
        {
            for (var slot = 0; slot < Trophy.Cap; slot++)
            {
                var carried = Trophy.PositionOf(slot);
                var tucked = WeaponStow.TrophyOf(slot);
                var out0 = (float)Math.Sqrt(carried.X * carried.X + carried.Z * carried.Z);
                var in0 = (float)Math.Sqrt(tucked.X * tucked.X + tucked.Z * tucked.Z);

                Assert.That(in0, Is.EqualTo(WeaponStow.Tuck).Within(1e-4f));
                Assert.That(in0, Is.LessThan(out0));
                Assert.That(tucked.Y, Is.EqualTo(carried.Y));
                Assert.That(2f * (in0 + Trophy.Thickness * 0.5f), Is.LessThan(WeaponStow.Shoulders));
                Assert.That(2f * (out0 + Trophy.Thickness * 0.5f), Is.GreaterThan(WeaponStow.Shoulders));
            }
        }
    }
}
