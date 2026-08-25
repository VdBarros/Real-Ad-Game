using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraFlightTests
    {
        const float Frame = 1f / 60f;

        static LevelGraph Graph()
        {
            return LevelGraphFixture.TwoFloors();
        }

        [Test]
        public void AFlightOpensTightOnTheStartAndEndsOnTheConstant()
        {
            var graph = Graph();
            var flight = CameraFlight.Over(graph);

            Assert.That(flight.Framing, Is.EqualTo(LevelFraming.Opening(graph)));
            Assert.That(flight.Framing.OrthographicSize, Is.EqualTo(LevelFraming.OpeningSize));
            Assert.That(flight.Destination, Is.EqualTo(LevelFraming.Play(graph)));
            Assert.That(flight.Destination.OrthographicSize, Is.EqualTo(IsoProjection.OrthographicSize));
        }

        [Test]
        public void TheLastFrameEqualsTheConstantExactlyWithNoSnapIntoPlay()
        {
            var graph = Graph();
            var flight = CameraFlight.Over(graph);

            while (!flight.IsSettled)
            {
                flight = flight.Advanced(Frame);
            }

            Assert.That(flight.Framing, Is.EqualTo(LevelFraming.Play(graph)));
            Assert.That(flight.Framing.Target, Is.EqualTo(LevelFraming.Play(graph).Target));
            Assert.That(flight.Framing.OrthographicSize, Is.EqualTo(IsoProjection.OrthographicSize));
        }

        [Test]
        public void ATapDuringTheFlightLandsOnTheStateTheFlightWasHeadingFor()
        {
            var graph = Graph();
            var skipped = CameraFlight.Over(graph).Advanced(0.4f).Skipped();

            Assert.That(skipped.IsSettled, Is.True);
            Assert.That(skipped.Framing, Is.EqualTo(LevelFraming.Play(graph)));
        }

        [Test]
        public void TheFlightEasesInAndOutRatherThanLeavingAtFullSpeed()
        {
            var graph = Graph();
            var opening = LevelFraming.Opening(graph);

            var early = CameraFlight.Over(graph).Advanced(CameraFlight.Seconds * 0.1f).Framing;
            var middle = CameraFlight.Over(graph).Advanced(CameraFlight.Seconds * 0.5f).Framing;

            var travelled = CameraGeometry.PanPixels(opening, early);
            var half = CameraGeometry.PanPixels(opening, middle);

            Assert.That(travelled, Is.LessThan(half * 0.1f));
            Assert.That(middle.OrthographicSize, Is.EqualTo(
                (LevelFraming.OpeningSize + IsoProjection.OrthographicSize) * 0.5f).Within(1e-4f));
        }

        [Test]
        public void TheSameSeedProducesTheSameFlightFrameForFrame()
        {
            var first = CameraFlight.Over(LevelGraphFixture.TwoFloors());
            var second = CameraFlight.Over(LevelGraphFixture.TwoFloorsAssembledBackwards());

            while (!first.IsSettled)
            {
                Assert.That(second.Framing, Is.EqualTo(first.Framing));
                first = first.Advanced(Frame);
                second = second.Advanced(Frame);
            }

            Assert.That(second.IsSettled, Is.True);
            Assert.That(second.Framing, Is.EqualTo(first.Framing));
        }

        [Test]
        public void AFlightOnlyEverRunsForwards()
        {
            Assert.That(
                () => CameraFlight.Over(Graph()).Advanced(-Frame),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheFlightIsTheOnlyInterpolationInTheRig()
        {
            var graph = Graph();
            var staging = CameraStaging.Over(graph);
            var moving = 0;

            var previous = staging.Framing;
            while (staging.IsBusy)
            {
                staging = staging.Advanced(Frame);
                if (!staging.Framing.Equals(previous))
                {
                    moving++;
                }

                previous = staging.Framing;
            }

            var beating = staging.CutTo(new TilePosition(0, 1, 1));
            var held = beating.Framing;
            for (var step = 0; step < 30; step++)
            {
                beating = beating.Advanced(Frame);
                Assert.That(beating.Framing, Is.EqualTo(held));
            }

            Assert.That(moving, Is.GreaterThan(60));
        }
    }
}
