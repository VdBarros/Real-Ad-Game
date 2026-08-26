using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraStagingTests
    {
        const float Frame = 1f / 60f;

        static readonly TilePosition Multiplier = new TilePosition(0, 2, 1);

        static LevelGraph Graph()
        {
            return LevelGraphFixture.TwoTerraces();
        }

        static CameraStaging Rested(CameraStaging staging)
        {
            for (var step = 0; step < 1200 && !staging.IsSettled; step++)
            {
                staging = staging.Advanced(Frame);
            }

            return staging;
        }

        static CameraStaging Playing(LevelGraph graph)
        {
            return Rested(CameraStaging.Over(graph).Skipped());
        }

        static WorldPoint Up(float metres)
        {
            return Times(IsoProjection.CameraUp, metres);
        }

        static WorldPoint Times(WorldPoint direction, float metres)
        {
            return new WorldPoint(direction.X * metres, direction.Y * metres, direction.Z * metres);
        }

        static IEnumerable<WorldPoint> Compass(float metres)
        {
            var right = IsoProjection.CameraRight;
            var up = IsoProjection.CameraUp;

            for (var step = 0; step < 8; step++)
            {
                var angle = step * Math.PI * 0.25;
                var across = (float)Math.Cos(angle) * metres;
                var along = (float)Math.Sin(angle) * metres;

                yield return new WorldPoint(
                    right.X * across + up.X * along,
                    right.Y * across + up.Y * along,
                    right.Z * across + up.Z * along);
            }
        }

        static bool Shows(LevelGraph graph, CameraFraming framing)
        {
            var acrossHalf = framing.OrthographicSize * ScreenFrame.Width / ScreenFrame.Height;

            foreach (var tile in graph.Tiles.Tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var apart = new WorldPoint(
                    point.X - framing.Target.X, point.Y - framing.Target.Y, point.Z - framing.Target.Z);

                if (Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraRight)) <= acrossHalf
                    && Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraUp)) <= framing.OrthographicSize)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void TheRigIsBusyForTheOpeningAndFreeOnceItLetsGo()
        {
            var staging = CameraStaging.Over(Graph());

            Assert.That(staging.IsBusy, Is.True);

            staging = staging.Advanced(CameraFlight.Duration);

            Assert.That(staging.IsBusy, Is.False);
        }

        [Test]
        public void ATapDuringTheOpeningReturnsControlImmediatelyOnTheWholeLevel()
        {
            var graph = Graph();
            var staging = CameraStaging.Over(graph).Advanced(0.7f).Skipped();

            Assert.That(staging.IsBusy, Is.False);
            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.Whole(graph)));
            Assert.That(staging.Framing, Is.EqualTo(staging.Reveal));
        }

        [Test]
        public void TheOpeningEasesOffTheWholeLevelOntoThePlayerRatherThanCuttingToIt()
        {
            var graph = Graph();
            var player = LevelFraming.Play(LevelFraming.StartPoint(graph));
            var staging = CameraStaging.Over(graph);

            while (staging.IsBusy)
            {
                staging = staging.Advanced(Frame);
                Assert.That(
                    staging.Framing.Target,
                    Is.Not.EqualTo(player.Target),
                    "The opening reached the player before it had let go of the whole level.");
            }

            Assert.That(staging.Framing, Is.EqualTo(staging.Reveal));

            var frames = 0;
            var apart = ScreenFrame.PanPixels(staging.Framing, player);
            while (!staging.IsSettled && frames < 1200)
            {
                staging = staging.Advanced(Frame);
                frames++;

                var closer = ScreenFrame.PanPixels(staging.Framing, player);
                Assert.That(closer, Is.LessThanOrEqualTo(apart));
                apart = closer;
            }

            Assert.That(frames, Is.GreaterThan(12), "The camera cut to the player rather than easing onto them.");
            Assert.That(staging.Framing, Is.EqualTo(player));
        }

        [Test]
        public void PlayFramingFollowsThePlayer()
        {
            var graph = Graph();
            var staging = Playing(graph);
            var walked = new WorldPoint(6f, 2f, 6f);

            Assert.That(staging.Framing.Target, Is.EqualTo(LevelFraming.StartPoint(graph)));

            staging = Rested(staging.Follows(walked));

            Assert.That(staging.Framing.Target, Is.EqualTo(walked));
            Assert.That(staging.Framing.OrthographicSize, Is.EqualTo(IsoProjection.OrthographicSize));
        }

        [Test]
        public void TheFollowMovesTowardsThePlayerRatherThanCuttingToThem()
        {
            var graph = Graph();
            var staging = Playing(graph).Follows(new WorldPoint(6f, 2f, 6f));
            var stepped = staging.Advanced(Frame);

            Assert.That(stepped.Framing, Is.Not.EqualTo(staging.Framing));
            Assert.That(stepped.Framing.Target, Is.Not.EqualTo(stepped.Subject));
            Assert.That(
                ScreenFrame.PanPixels(stepped.Framing, LevelFraming.Play(stepped.Subject)),
                Is.LessThan(ScreenFrame.PanPixels(staging.Framing, LevelFraming.Play(staging.Subject))));
        }

        [Test]
        public void ABeatCutsAwayFromTheFollowAndTakesInputWithIt()
        {
            var graph = Graph();
            var staging = Playing(graph).CutTo(Multiplier);

            Assert.That(staging.IsBusy, Is.True);
            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.CloseUp(Multiplier)));
        }

        [Test]
        public void ABeatOutranksTheFollowAndHandsItBackWhenItIsDone()
        {
            var graph = Graph();
            var standing = IsoProjection.Of(Multiplier);
            var staging = Rested(Playing(graph).Follows(standing)).CutTo(Multiplier);

            for (var step = 0; step < 30; step++)
            {
                staging = staging.Advanced(Frame);
                Assert.That(staging.Framing, Is.EqualTo(LevelFraming.CloseUp(Multiplier)));
            }

            staging = staging.Advanced(ZoomBeat.FloorSeconds).Released();

            Assert.That(staging.IsBusy, Is.False);
            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.Play(standing)));
            Assert.That(staging.Framing, Is.EqualTo(staging.Following));
        }

        [Test]
        public void ATapDuringABeatReturnsControlImmediatelyOnThePlayer()
        {
            var graph = Graph();
            var staging = Playing(graph).CutTo(Multiplier).Advanced(Frame).Skipped();

            Assert.That(staging.IsBusy, Is.False);
            Assert.That(staging.Framing, Is.EqualTo(staging.Following));
        }

        [Test]
        public void EveryCameraStateIsTheFollowOrACutToAKnownTransform()
        {
            var graph = Graph();
            var staging = Playing(graph);

            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.Play(LevelFraming.StartPoint(graph))));

            foreach (var node in graph.Decisions.Nodes)
            {
                var standing = IsoProjection.Of(node.Position);
                var beating = Rested(staging.Follows(standing)).CutTo(node.Position);

                Assert.That(beating.Framing, Is.EqualTo(LevelFraming.CloseUp(node.Position)));
                Assert.That(
                    beating.Advanced(ZoomBeat.CapSeconds).Framing,
                    Is.EqualTo(LevelFraming.Play(standing)));
            }
        }

        [Test]
        public void ABeatCannotFireWhileTheOpeningStillOwnsInput()
        {
            var flying = CameraStaging.Over(Graph()).Advanced(0.5f);

            Assert.That(() => flying.CutTo(Multiplier), Throws.InstanceOf<System.InvalidOperationException>());
            Assert.That(flying.Skipped().CutTo(Multiplier).IsBusy, Is.True);
        }

        [Test]
        public void ADragLooksAwayFromThePlayerAndStaysThereWhileTheFingerIsDown()
        {
            var graph = Graph();
            var staging = Playing(graph);
            var player = staging.Framing;
            var looking = staging.Looks(Up(4f));

            Assert.That(
                ScreenFrame.PanPixels(player, looking.Framing),
                Is.GreaterThan(0f),
                "A drag left the camera where it already was.");

            for (var frame = 0; frame < 120; frame++)
            {
                looking = looking.Advanced(Frame);
                Assert.That(
                    looking.Framing,
                    Is.EqualTo(staging.Looks(Up(4f)).Framing),
                    "The camera crept back to the player while the finger was still down.");
            }
        }

        [Test]
        public void ReleasingADragEasesBackOntoThePlayerRatherThanCuttingToThem()
        {
            var graph = Graph();
            var player = Playing(graph).Framing;
            var staging = Playing(graph).Looks(Up(4f)).LooksBack();

            var frames = 0;
            var apart = ScreenFrame.PanPixels(staging.Framing, player);

            Assert.That(apart, Is.GreaterThan(0f));

            while (!staging.IsSettled && frames < 1200)
            {
                staging = staging.Advanced(Frame);
                frames++;

                var closer = ScreenFrame.PanPixels(staging.Framing, player);
                Assert.That(closer, Is.LessThanOrEqualTo(apart));
                apart = closer;
            }

            Assert.That(frames, Is.GreaterThan(12), "The camera cut back to the player rather than easing.");
            Assert.That(staging.Framing, Is.EqualTo(player));
        }

        [Test]
        public void NoDragCanLoseTheLevelOffTheEdgeOfTheWorld()
        {
            var graph = Graph();
            var staging = Playing(graph);

            foreach (var offset in Compass(1000f))
            {
                var looking = staging.Looks(offset);

                Assert.That(
                    ScreenFrame.PanPixels(staging.Framing, looking.Framing),
                    Is.GreaterThan(0f),
                    "A drag of " + offset + " moved the camera nowhere at all.");
                Assert.That(
                    Shows(graph, looking.Framing),
                    Is.True,
                    "A drag of " + offset + " left no tile of the level on screen.");
            }
        }

        [Test]
        public void ADragTakesTheCameraNoFurtherThanTheLevelAllowsHoweverHardItIsPulled()
        {
            var graph = Graph();
            var staging = Playing(graph);

            foreach (var offset in Compass(1f))
            {
                var far = staging.Looks(Times(offset, 50f));
                var further = staging.Looks(Times(offset, 5000f));

                Assert.That(
                    ScreenFrame.PanPixels(far.Framing, further.Framing),
                    Is.LessThan(1f),
                    "Pulling a hundred times harder in the direction " + offset + " bought more of the world.");
            }
        }

        [Test]
        public void ABeatStillOutranksACameraTheFingerIsHoldingAway()
        {
            var graph = Graph();
            var standing = IsoProjection.Of(Multiplier);
            var staging = Rested(Playing(graph).Follows(standing)).Looks(Up(4f)).CutTo(Multiplier);

            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.CloseUp(Multiplier)));

            staging = staging.Advanced(ZoomBeat.CapSeconds).LooksBack();

            Assert.That(Rested(staging).Framing, Is.EqualTo(LevelFraming.Play(standing)));
        }

        [Test]
        public void TheFollowStaysStillUntilTheOpeningHasLetGo()
        {
            var graph = Graph();
            var staging = CameraStaging.Over(graph).Follows(new WorldPoint(6f, 2f, 6f));

            while (staging.IsBusy)
            {
                Assert.That(staging.Following, Is.EqualTo(staging.Reveal));
                staging = staging.Advanced(Frame);
            }
        }
    }
}
