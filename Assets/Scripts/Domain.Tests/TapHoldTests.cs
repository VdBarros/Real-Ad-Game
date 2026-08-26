using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TapHoldTests
    {
        const float Reach = 70f;

        static readonly ScreenPoint Origin = new ScreenPoint(300f, 900f);

        static ScreenPoint Away(float pixels)
        {
            return new ScreenPoint(Origin.X + pixels, Origin.Y);
        }

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
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(hold.OwnsThePress, Is.False);
        }

        [Test]
        public void AHoldRaisedUnderAFingerThatIsAlreadyDownIgnoresThatWholePress()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: false,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(down.OwnsThePress, Is.False);

            var lifted = down.Reading(
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void APressThisHoldSawAndThenAReleaseIsATap()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(down.OwnsThePress, Is.True);

            var lifted = down.Reading(
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
            Assert.That(lifted.OwnsThePress, Is.False);
        }

        [Test]
        public void APressAndReleaseInsideOneFrameIsStillATap()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void ADragKeepsAimingForAsLongAsTheFingerIsDown()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            for (var frame = 0; frame < 5; frame++)
            {
                hold = hold.Reading(
                    pressedNow: false,
                    releasedNow: false,
                    isPressed: true,
                    hovers: false,
                    locked: false,
                    finger: Origin,
                    reach: Reach);

                Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
                Assert.That(hold.OwnsThePress, Is.True);
            }
        }

        [Test]
        public void AMouseAimsWhereItHoversWithNothingPressed()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: false,
                releasedNow: false,
                isPressed: false,
                hovers: true,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(hold.OwnsThePress, Is.False);
        }

        [Test]
        public void AFingerOffTheGlassAimsAtNothing()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: false,
                releasedNow: false,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void AHoldThatIgnoredOnePressStillOwnsTheNextOne()
        {
            var stranger = TapHold.Idle.Reading(
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            var mine = stranger.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(mine.OwnsThePress, Is.True);

            Assert.That(
                mine.Reading(
                    pressedNow: false,
                    releasedNow: true,
                    isPressed: false,
                    hovers: false,
                    locked: false,
                    finger: Origin,
                    reach: Reach).Gesture,
                Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void APressTakenWhileLockedNeverAimsOnceTheLockLifts()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: true,
                finger: Origin,
                reach: Reach);

            Assert.That(down.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(down.OwnsThePress, Is.False);

            var freed = down.Reading(
                pressedNow: false,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(freed.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(freed.OwnsThePress, Is.False);

            var lifted = freed.Reading(
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        [Test]
        public void ATapThatBeginsAndEndsLockedStillReportsItsRelease()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: true,
                finger: Origin,
                reach: Reach);

            var lifted = down.Reading(
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: true,
                finger: Origin,
                reach: Reach);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
            Assert.That(lifted.OwnsThePress, Is.False);
        }

        [Test]
        public void ALockThatFallsPartWayThroughAPressLeavesTheTapItAlreadyOwned()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);

            Assert.That(down.OwnsThePress, Is.True);

            var lifted = down.Reading(
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: true,
                finger: Origin,
                reach: Reach);

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void AFingerPutDownAndLiftedInOneLockedFrameIsStillARelease()
        {
            var hold = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: true,
                finger: Origin,
                reach: Reach);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void TwoHoldsReadingTheSameFrameAreTheSameHold()
        {
            var one = TapHold.Idle.Reading(true, false, true, false, false, Origin, Reach);
            var other = TapHold.Idle.Reading(true, false, true, false, false, Origin, Reach);

            Assert.That(one, Is.EqualTo(other));
            Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(one, Is.Not.EqualTo(TapHold.Idle));
        }

        [Test]
        public void APressThatSlidesLessThanTheReachStillAimsAndStillCommits()
        {
            var hold = Pressed();

            hold = Slid(hold, Away(Reach * 0.5f));

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(hold.OwnsThePress, Is.True);

            var lifted = Lifted(hold, Away(Reach));

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void APressThatTravelsBeyondTheReachBecomesAPan()
        {
            var hold = Slid(Pressed(), Away(Reach + 1f));

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Pan));
            Assert.That(hold.HoldsAPress, Is.True);
        }

        [Test]
        public void APressThatBecameAPanForfeitsItsTap()
        {
            var lifted = Lifted(Slid(Pressed(), Away(Reach + 1f)), Away(Reach + 1f));

            Assert.That(lifted.Gesture, Is.EqualTo(TapGesture.Pan));
            Assert.That(lifted.Gesture, Is.Not.EqualTo(TapGesture.Release));
            Assert.That(lifted.HoldsAPress, Is.False);
        }

        [Test]
        public void APressThatStrayedAndCameBackHasStillForfeitedItsTap()
        {
            var hold = Slid(Slid(Pressed(), Away(Reach * 3f)), Origin);

            Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Pan));
            Assert.That(Lifted(hold, Origin).Gesture, Is.EqualTo(TapGesture.Pan));
        }

        [Test]
        public void TravelIsMeasuredFromWhereThisPressBeganAndNotFromTheLastOne()
        {
            var strayed = Lifted(Slid(Pressed(), Away(Reach * 4f)), Away(Reach * 4f));

            var again = strayed.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Away(Reach * 4f),
                reach: Reach);

            Assert.That(again.Gesture, Is.EqualTo(TapGesture.Aim));
            Assert.That(
                Lifted(again, Away(Reach * 4f)).Gesture,
                Is.EqualTo(TapGesture.Release));
        }

        [Test]
        public void AMouseRangingAcrossTheGlassWithNothingPressedNeverPans()
        {
            var hold = TapHold.Idle;

            for (var step = 0; step < 5; step++)
            {
                hold = hold.Reading(
                    pressedNow: false,
                    releasedNow: false,
                    isPressed: false,
                    hovers: true,
                    locked: false,
                    finger: Away(step * Reach * 4f),
                    reach: Reach);

                Assert.That(hold.Gesture, Is.EqualTo(TapGesture.Aim));
            }
        }

        [Test]
        public void APressTakenWhileLockedPansNoMoreThanItAims()
        {
            var down = TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: true,
                finger: Origin,
                reach: Reach);

            var strayed = Slid(down, Away(Reach * 4f));

            Assert.That(strayed.Gesture, Is.EqualTo(TapGesture.Ignore));
            Assert.That(Lifted(strayed, Away(Reach * 4f)).Gesture, Is.EqualTo(TapGesture.Ignore));
        }

        static TapHold Pressed()
        {
            return TapHold.Idle.Reading(
                pressedNow: true,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: Origin,
                reach: Reach);
        }

        static TapHold Slid(TapHold hold, ScreenPoint finger)
        {
            return hold.Reading(
                pressedNow: false,
                releasedNow: false,
                isPressed: true,
                hovers: false,
                locked: false,
                finger: finger,
                reach: Reach);
        }

        static TapHold Lifted(TapHold hold, ScreenPoint finger)
        {
            return hold.Reading(
                pressedNow: false,
                releasedNow: true,
                isPressed: false,
                hovers: false,
                locked: false,
                finger: finger,
                reach: Reach);
        }
    }
}
