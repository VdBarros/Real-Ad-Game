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
            return LevelGraphFixture.TwoFloors();
        }

        static CameraStaging Playing(LevelGraph graph)
        {
            return CameraStaging.Over(graph).Skipped();
        }

        [Test]
        public void TheRigIsBusyForTheFlightAndFreeOnceItLands()
        {
            var staging = CameraStaging.Over(Graph());

            Assert.That(staging.IsBusy, Is.True);

            staging = staging.Advanced(CameraFlight.Seconds);

            Assert.That(staging.IsBusy, Is.False);
        }

        [Test]
        public void ATapDuringTheFlightReturnsControlImmediatelyAtTheConstant()
        {
            var graph = Graph();
            var staging = CameraStaging.Over(graph).Advanced(0.7f).Skipped();

            Assert.That(staging.IsBusy, Is.False);
            Assert.That(staging.Framing, Is.EqualTo(staging.Constant));
            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.Play(graph)));
        }

        [Test]
        public void ABeatCutsAwayFromTheConstantAndTakesInputWithIt()
        {
            var graph = Graph();
            var staging = Playing(graph).CutTo(Multiplier);

            Assert.That(staging.IsBusy, Is.True);
            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.CloseUp(Multiplier)));
        }

        [Test]
        public void ABeatExitsOnTheConstantExactly()
        {
            var graph = Graph();
            var staging = Playing(graph).CutTo(Multiplier);

            staging = staging.Advanced(ZoomBeat.FloorSeconds).Released();

            Assert.That(staging.IsBusy, Is.False);
            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.Play(graph)));
            Assert.That(staging.Framing.Target, Is.EqualTo(staging.Constant.Target));
            Assert.That(staging.Framing.OrthographicSize, Is.EqualTo(staging.Constant.OrthographicSize));
        }

        [Test]
        public void ATapDuringABeatReturnsControlImmediatelyAtTheConstant()
        {
            var graph = Graph();
            var staging = Playing(graph).CutTo(Multiplier).Advanced(Frame).Skipped();

            Assert.That(staging.IsBusy, Is.False);
            Assert.That(staging.Framing, Is.EqualTo(LevelFraming.Play(graph)));
        }

        [Test]
        public void EveryCameraStateIsTheConstantOrACutToAKnownTransform()
        {
            var graph = Graph();
            var constant = LevelFraming.Play(graph);
            var staging = Playing(graph);

            Assert.That(staging.Framing, Is.EqualTo(constant));

            foreach (var node in graph.Decisions.Nodes)
            {
                var beating = staging.CutTo(node.Position);

                Assert.That(beating.Framing, Is.EqualTo(LevelFraming.CloseUp(node.Position)));
                Assert.That(beating.Advanced(ZoomBeat.CapSeconds).Framing, Is.EqualTo(constant));
            }
        }

        [Test]
        public void ABeatCannotFireWhileTheFlightStillOwnsInput()
        {
            var flying = CameraStaging.Over(Graph()).Advanced(0.5f);

            Assert.That(() => flying.CutTo(Multiplier), Throws.InstanceOf<System.InvalidOperationException>());
            Assert.That(flying.Skipped().CutTo(Multiplier).IsBusy, Is.True);
        }
    }
}
