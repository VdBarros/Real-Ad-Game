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
            return LevelGraphFixture.TwoTerraces();
        }

        [Test]
        public void AFlightOpensTightOnTheStartAndEndsOnTheWholeLevel()
        {
            var graph = Graph();
            var flight = CameraFlight.Over(graph);

            Assert.That(flight.Framing, Is.EqualTo(LevelFraming.Opening(graph)));
            Assert.That(flight.Framing.OrthographicSize, Is.EqualTo(LevelFraming.OpeningSize));
            Assert.That(flight.Destination, Is.EqualTo(LevelFraming.Whole(graph)));
        }

        [Test]
        public void TheLastFrameEqualsTheWholeLevelExactlyWithNoSnapIntoTheHold()
        {
            var graph = Graph();
            var whole = LevelFraming.Whole(graph);
            var flight = CameraFlight.Over(graph);

            while (!flight.IsSettled)
            {
                flight = flight.Advanced(Frame);
            }

            Assert.That(flight.Framing, Is.EqualTo(whole));
            Assert.That(flight.Framing.Target, Is.EqualTo(whole.Target));
            Assert.That(flight.Framing.OrthographicSize, Is.EqualTo(whole.OrthographicSize));
        }

        [Test]
        public void TheFlightArrivesOnTheWholeLevelAndHoldsThereBeforeItLetsGo()
        {
            var graph = Graph();
            var whole = LevelFraming.Whole(graph);
            var flight = CameraFlight.Over(graph);
            var held = 0f;

            while (!flight.IsSettled)
            {
                flight = flight.Advanced(Frame);

                if (flight.Framing.Equals(whole) && !flight.IsSettled)
                {
                    held += Frame;
                }
            }

            Assert.That(
                held,
                Is.GreaterThanOrEqualTo(0.3f),
                "The opening never rests on the whole level long enough to be read as a reveal.");
        }

        [Test]
        public void TheWholeLevelIsOnScreenAtTheFrameTheOpeningHoldsOn()
        {
            var graph = Graph();
            var whole = LevelFraming.Whole(graph);
            var acrossHalf = whole.OrthographicSize * ScreenFrame.Width / ScreenFrame.Height;

            foreach (var tile in graph.Tiles.Tiles)
            {
                var point = IsoProjection.Of(tile.Position);
                var apart = new WorldPoint(
                    point.X - whole.Target.X, point.Y - whole.Target.Y, point.Z - whole.Target.Z);

                Assert.That(
                    Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraRight)),
                    Is.LessThanOrEqualTo(acrossHalf),
                    tile.Position + " falls off the side of the frame the reveal holds on.");
                Assert.That(
                    Math.Abs(WorldPoint.Dot(apart, IsoProjection.CameraUp)),
                    Is.LessThanOrEqualTo(whole.OrthographicSize),
                    tile.Position + " falls off the top or bottom of the frame the reveal holds on.");
            }
        }

        [Test]
        public void ATapDuringTheFlightLandsOnTheStateTheFlightWasHeadingFor()
        {
            var graph = Graph();
            var skipped = CameraFlight.Over(graph).Advanced(0.4f).Skipped();

            Assert.That(skipped.IsSettled, Is.True);
            Assert.That(skipped.Framing, Is.EqualTo(LevelFraming.Whole(graph)));
        }

        [Test]
        public void TheFlightEasesInAndOutRatherThanLeavingAtFullSpeed()
        {
            var graph = Graph();
            var opening = LevelFraming.Opening(graph);

            var early = CameraFlight.Over(graph).Advanced(CameraFlight.Seconds * 0.1f).Framing;
            var middle = CameraFlight.Over(graph).Advanced(CameraFlight.Seconds * 0.5f).Framing;

            var travelled = ScreenFrame.PanPixels(opening, early);
            var half = ScreenFrame.PanPixels(opening, middle);

            Assert.That(travelled, Is.LessThan(half * 0.1f));
            Assert.That(middle.OrthographicSize, Is.EqualTo(
                (LevelFraming.OpeningSize + LevelFraming.Whole(graph).OrthographicSize) * 0.5f).Within(1e-4f));
        }

        [Test]
        public void TheSameSeedProducesTheSameFlightFrameForFrame()
        {
            var first = CameraFlight.Over(LevelGraphFixture.TwoTerraces());
            var second = CameraFlight.Over(LevelGraphFixture.TwoTerracesAssembledBackwards());

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
        public void ABeatHoldsStillWhileTheOpeningAndTheFollowBothMove()
        {
            var graph = Graph();
            var staging = CameraStaging.Over(graph);
            var moving = 0;

            var previous = staging.Framing;
            while (!staging.IsSettled)
            {
                staging = staging.Advanced(Frame);
                if (!staging.Framing.Equals(previous))
                {
                    moving++;
                }

                previous = staging.Framing;
            }

            var beating = staging.CutTo(new TilePosition(0, 1, 1))
                .Follows(new WorldPoint(9f, 2f, 9f))
                .Advanced(ZoomBeat.InSeconds);
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
