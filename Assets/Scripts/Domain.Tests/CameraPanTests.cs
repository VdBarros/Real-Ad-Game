using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CameraPanTests
    {
        const float Frame = 1f / 60f;

        static readonly TilePosition Multiplier = new TilePosition(0, 2, 1);

        static LevelGraph Graph()
        {
            return LevelGraphFixture.TwoTerraces();
        }

        static CameraPan Pan()
        {
            return CameraPan.Around(Graph());
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

        static WorldPoint Origin()
        {
            return LevelFraming.StartPoint(Graph());
        }

        [Test]
        public void APanStartsOnTheSubjectWithNothingToGiveBack()
        {
            var pan = Pan();

            Assert.That(pan.IsResting, Is.True);
            Assert.That(pan.IsAway, Is.False);
            Assert.That(pan.HoldsTheCamera, Is.False);
        }

        [Test]
        public void ADragTakesTheCameraOffTheSubjectAndKeepsItWhereTheFingerLetGo()
        {
            var dragged = Pan().Dragged(Origin(), Up(4f));
            var look = dragged.Look;

            Assert.That(dragged.HoldsTheCamera, Is.True);
            Assert.That(dragged.IsAway, Is.True);

            var held = dragged.LetGo();

            Assert.That(held.Look, Is.EqualTo(look));
            Assert.That(held.HoldsTheCamera, Is.True);
            Assert.That(held.IsAway, Is.True);
            Assert.That(held.LetGo(), Is.EqualTo(held));
        }

        [Test]
        public void OneGestureCarriesOnFromWhereTheLastOneStopped()
        {
            var first = Pan().Dragged(Origin(), Up(2f)).LetGo();
            var second = first.Dragged(first.Look, Up(2f));

            Assert.That(
                second.Look,
                Is.EqualTo(Pan().Dragged(Origin(), Up(4f)).Look),
                "A second drag started over from the player instead of from the standing pan.");
        }

        [Test]
        public void OneGestureKeepsItsAnchorHoweverFarTheFingerWanders()
        {
            var pan = Pan();
            var out4 = pan.Dragged(Origin(), Up(4f));
            var out2 = out4.Dragged(out4.Look, Up(2f));

            Assert.That(
                out2.Look,
                Is.EqualTo(pan.Dragged(Origin(), Up(2f)).Look),
                "A drag still under the finger re-anchored and doubled itself.");
        }

        [Test]
        public void NoDragPastTheEdgeOfTheLevelSpringsBackWhenTheFingerLetsGo()
        {
            var far = Pan().Dragged(Origin(), Up(400f));
            var further = Pan().Dragged(Origin(), Up(40000f));

            Assert.That(
                ScreenFrame.PanPixels(LevelFraming.Play(far.Look), LevelFraming.Play(further.Look)),
                Is.LessThan(1f),
                "Pulling a hundred times harder bought more of the world.");
            Assert.That(far.LetGo().Look, Is.EqualTo(far.Look), "Letting go outside the bound sprang inward.");
        }

        [Test]
        public void ARecallLeavesThePanOnItsWayBackUntilItArrives()
        {
            var recalled = Pan().Dragged(Origin(), Up(4f)).LetGo().Recalled();

            Assert.That(recalled.HoldsTheCamera, Is.False);
            Assert.That(recalled.IsAway, Is.True);
            Assert.That(recalled.Recalled(), Is.EqualTo(recalled));
            Assert.That(recalled.Arrived().IsResting, Is.True);
            Assert.That(recalled.Arrived().IsAway, Is.False);
        }

        [Test]
        public void ARestingPanHasNothingToRecallOrArriveAt()
        {
            var pan = Pan();

            Assert.That(pan.Recalled(), Is.EqualTo(pan));
            Assert.That(pan.Arrived(), Is.EqualTo(pan));
            Assert.That(pan.Dropped(), Is.EqualTo(pan));
        }

        [Test]
        public void AHeldPanOnlyArrivesOnceItHasBeenRecalled()
        {
            var held = Pan().Dragged(Origin(), Up(4f)).LetGo();

            Assert.That(held.Arrived(), Is.EqualTo(held));
            Assert.That(held.Dropped().IsResting, Is.True);
        }

        [Test]
        public void OnlyAPanThatHoldsTheCameraHasALookToApply()
        {
            Assert.That(() => Pan().Look, Throws.InstanceOf<System.InvalidOperationException>());
            Assert.That(
                () => Pan().Dragged(Origin(), Up(4f)).LetGo().Recalled().Look,
                Throws.InstanceOf<System.InvalidOperationException>());
        }

        [Test]
        public void ADragLeavesTheCameraExactlyWhereTheFingerLetGoOfIt()
        {
            var graph = Graph();
            var staging = Playing(graph).Looks(Up(4f)).LookHeld();
            var parked = staging.Framing;

            Assert.That(
                ScreenFrame.PanPixels(Playing(graph).Framing, parked),
                Is.GreaterThan(0f),
                "The drag never left the player.");

            for (var frame = 0; frame < 240; frame++)
            {
                staging = staging.Advanced(Frame);

                Assert.That(
                    staging.Framing,
                    Is.EqualTo(parked),
                    "The camera drifted off the spot the finger left it on after " + frame + " frames.");
            }

            Assert.That(staging.IsAway, Is.True);
        }

        [Test]
        public void ThePlayerWalkingDoesNotDragAHeldCameraAlong()
        {
            var graph = Graph();
            var staging = Playing(graph).Looks(Up(4f)).LookHeld();
            var parked = staging.Framing;

            staging = staging.Follows(IsoProjection.Of(Multiplier));

            for (var frame = 0; frame < 240; frame++)
            {
                staging = staging.Advanced(Frame);

                Assert.That(
                    staging.Framing,
                    Is.EqualTo(parked),
                    "A walking player towed the held camera after " + frame + " frames.");
            }
        }

        [Test]
        public void AHeldCameraStopsAtTheEdgeOfTheLevelAndStaysStopped()
        {
            var graph = Graph();
            var far = Playing(graph).Looks(Up(400f));
            var held = far.LookHeld();
            var parked = held.Framing;

            Assert.That(parked, Is.EqualTo(far.Framing), "Letting go at the bound moved the camera.");

            for (var frame = 0; frame < 240; frame++)
            {
                held = held.Advanced(Frame);

                Assert.That(
                    held.Framing,
                    Is.EqualTo(parked),
                    "The camera rubber-banded off the bound after " + frame + " frames.");
            }
        }

        [Test]
        public void ARecallEasesAHeldCameraBackOntoThePlayerRatherThanCuttingToIt()
        {
            var graph = Graph();
            var player = Playing(graph).Framing;
            var staging = Playing(graph).Looks(Up(4f)).LookHeld().LooksBack();

            var apart = ScreenFrame.PanPixels(staging.Framing, player);
            var frames = 0;

            Assert.That(apart, Is.GreaterThan(0f));
            Assert.That(staging.IsAway, Is.True);

            while (!staging.IsSettled && frames < 1200)
            {
                staging = staging.Advanced(Frame);
                frames++;

                var closer = ScreenFrame.PanPixels(staging.Framing, player);
                Assert.That(closer, Is.LessThanOrEqualTo(apart));
                apart = closer;
            }

            Assert.That(frames, Is.GreaterThan(12), "The recall cut back to the player rather than easing.");
            Assert.That(staging.Framing, Is.EqualTo(player));
            Assert.That(staging.IsAway, Is.False, "The camera arrived and still reads as away from the player.");
        }

        [Test]
        public void NoPanCanFightTheOpeningFlight()
        {
            var graph = Graph();
            var flying = CameraStaging.Over(graph).Advanced(0.2f);

            Assert.That(flying.IsBusy, Is.True);
            Assert.That(flying.Looks(Up(4f)), Is.EqualTo(flying), "A drag stole the camera from the opening.");
            Assert.That(flying.Looks(Up(4f)).IsAway, Is.False);
        }

        [Test]
        public void APanInProgressLosesTheCameraToABeat()
        {
            var graph = Graph();
            var standing = IsoProjection.Of(Multiplier);
            var staging = Rested(Playing(graph).Follows(standing)).Looks(Up(4f)).LookHeld();

            Assert.That(staging.IsAway, Is.True);

            var beating = staging.CutTo(Multiplier);

            Assert.That(beating.Framing, Is.EqualTo(LevelFraming.CloseUp(Multiplier)));
            Assert.That(beating.IsAway, Is.False, "A beat took the camera and the pan still claims it.");
            Assert.That(beating.Looks(Up(4f)), Is.EqualTo(beating), "A drag fought the beat for the camera.");

            var after = Rested(beating.Advanced(ZoomBeat.CapSeconds).Released());

            Assert.That(after.Framing, Is.EqualTo(LevelFraming.Play(standing)));
            Assert.That(after.IsAway, Is.False);
        }

        [Test]
        public void AHeldLookIsOnlyGivenUpWhenItIsAskedFor()
        {
            var graph = Graph();
            var staging = Playing(graph).Looks(Up(4f));

            Assert.That(staging.LookHeld().LookHeld(), Is.EqualTo(staging.LookHeld()));
            Assert.That(staging.LookHeld().IsAway, Is.True);
            Assert.That(Rested(staging.LookHeld()).IsAway, Is.True, "Time alone gave the pan back.");
        }
    }
}
