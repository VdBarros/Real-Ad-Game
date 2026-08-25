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
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(hold.OwnsThePress, Is.False);
        }

        [Test]
        public void AHoldRaisedUnderAFingerThatIsAlreadyDownIgnoresThatWholePress()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: false, isPressed: true, hovers: false);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(down.OwnsThePress, Is.False);

            var lifted = down.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void APressThisHoldSawAndThenAReleaseIsATap()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(down.OwnsThePress, Is.True);

            var lifted = down.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
            Assert.That(lifted.OwnsThePress, Is.False);
        }

        [Test]
        public void APressAndReleaseInsideOneFrameIsStillATap()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: true, isPressed: false, hovers: false);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void ADragKeepsAimingForAsLongAsTheFingerIsDown()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false);

            for (var frame = 0; frame < 5; frame++)
            {
                hold = hold.Reading(
                    pressedNow: false, releasedNow: false, isPressed: true, hovers: false);

                Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
                Assert.That(hold.OwnsThePress, Is.True);
            }
        }

        [Test]
        public void AMouseAimsWhereItHoversWithNothingPressed()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: false, isPressed: false, hovers: true);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(hold.OwnsThePress, Is.False);
        }

        [Test]
        public void AFingerOffTheGlassAimsAtNothing()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: false, isPressed: false, hovers: false);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void AHoldThatIgnoredOnePressStillOwnsTheNextOne()
        {
            var stranger = TapHold.Idle.Reading(
                pressedNow: false, releasedNow: true, isPressed: false, hovers: false);

            var mine = stranger.Reading(
                pressedNow: true, releasedNow: false, isPressed: true, hovers: false);

            Assert.That(mine.OwnsThePress, Is.True);

            Assert.That(
                mine.Reading(pressedNow: false, releasedNow: true, isPressed: false, hovers: false).Gesture,
                Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void TwoHoldsReadingTheSameFrameAreTheSameHold()
        {
            var one = TapHold.Idle.Reading(true, false, true, false);
            var other = TapHold.Idle.Reading(true, false, true, false);

            Assert.That(one, Is.EqualTo(other));
            Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(one, Is.Not.EqualTo(TapHold.Idle));
        }
    }
}
