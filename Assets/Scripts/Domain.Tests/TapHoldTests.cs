using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TapHoldTests
    {
        [Test]
        public void AFreshHoldOwnsNothingAndAsksForNothing()
        {
            Assert.That(TapHold.Idle.OwnsThePress, Is.False);
            Assert.That(TapHold.Idle.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void AReleaseOfAPressThisHoldNeverSawIsNotATap()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false, locked: false);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(hold.OwnsThePress, Is.False);
        }

        [Test]
        public void AHoldRaisedUnderAFingerThatIsAlreadyDownIgnoresThatWholePress()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: false, isPressed: true, hovers: false, locked: false);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(down.OwnsThePress, Is.False);

            var lifted = down.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false, locked: false);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void APressThisHoldSawAndThenAReleaseIsATap()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false, locked: false);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(down.OwnsThePress, Is.True);

            var lifted = down.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false, locked: false);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
            Assert.That(lifted.OwnsThePress, Is.False);
        }

        [Test]
        public void APressAndReleaseInsideOneFrameIsStillATap()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: true, isPressed: false, hovers: false, locked: false);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void ADragKeepsAimingForAsLongAsTheFingerIsDown()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false, locked: false);

            for (var frame = 0; frame < 5; frame++)
            {
                hold = hold.Reading(
                    pressedNow: false, releasedNow: false, isPressed: true, hovers: false, locked: false);

                Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
                Assert.That(hold.OwnsThePress, Is.True);
            }
        }

        [Test]
        public void AMouseAimsWhereItHoversWithNothingPressed()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: false, isPressed: false, hovers: true, locked: false);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(hold.OwnsThePress, Is.False);
        }

        [Test]
        public void AFingerOffTheGlassAimsAtNothing()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: false, isPressed: false, hovers: false, locked: false);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void AHoldThatIgnoredOnePressStillOwnsTheNextOne()
        {
            var stranger = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false, locked: false);

            var mine = stranger.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false, locked: false);

            Assert.That(mine.OwnsThePress, Is.True);

            Assert.That(
                mine.Reading(
                    pressedNow: false,
                    releasedNow: true,
                    isPressed: false,
                    hovers: false,
                    locked: false).Gesture,
                Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void APressTakenWhileLockedNeverAimsOnceTheLockLifts()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false, locked: true);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(down.OwnsThePress, Is.False);

            var freed = down.Reading(
                pressedNow: false, releasedNow: false, isPressed: true, hovers: false, locked: false);

            Assert.That(freed.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(freed.OwnsThePress, Is.False);

            var lifted = freed.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false, locked: false);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void ATapThatBeginsAndEndsLockedStillReportsItsRelease()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false, locked: true);

            var lifted = down.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false, locked: true);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
            Assert.That(lifted.OwnsThePress, Is.False);
        }

        [Test]
        public void ALockThatFallsPartWayThroughAPressLeavesTheTapItAlreadyOwned()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false, locked: false);

            Assert.That(down.OwnsThePress, Is.True);

            var lifted = down.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false, locked: true);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void AFingerPutDownAndLiftedInOneLockedFrameIsStillARelease()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: true, isPressed: false, hovers: false, locked: true);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void TwoHoldsReadingTheSameFrameAreTheSameHold()
        {
            var one = TapHold.Idle.Reading(true, false, true, false, false);
            var other = TapHold.Idle.Reading(true, false, true, false, false);

            Assert.That(one, Is.EqualTo(other));
            Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(one, Is.Not.EqualTo(TapHold.Idle));
        }
    }
}
