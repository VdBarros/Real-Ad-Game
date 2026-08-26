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
