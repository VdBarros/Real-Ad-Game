using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TakeTests
    {
        const float Tolerance = 1e-5f;

        [Test]
        public void AnUntakenPickupIsShutAndSolidAndHasNothingToPlay()
        {
            var full = Take.None;

            Assert.That(full.IsSpent, Is.False);
            Assert.That(full.IsSettled, Is.True);
            Assert.That(full.IsGone, Is.False);
            Assert.That(full.LidSwing, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(full.Opacity, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(default(Take).Equals(full), Is.True);
        }

        [Test]
        public void TheBeatIsStillThreeTenthsOfASecond()
        {
            Assert.That(Take.Seconds, Is.EqualTo(0.30f).Within(Tolerance));
        }

        [Test]
        public void TheLidIsFullyOpenBeforeAnythingStartsToFade()
        {
            Assert.That(Take.LidShare, Is.GreaterThan(0f));
            Assert.That(Take.LidShare, Is.LessThan(1f));

            var opening = Take.Begun().Advanced(Take.Seconds * Take.LidShare);

            Assert.That(opening.LidSwing, Is.EqualTo(Take.LidAngle).Within(Tolerance));
            Assert.That(opening.Opacity, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(opening.IsSettled, Is.False);
            Assert.That(opening.IsGone, Is.False);
        }

        [Test]
        public void ATakeSwingsTheLidOpenAndThenTakesTheWholeChestAway()
        {
            var reel = Take.Begun();

            Assert.That(reel.IsSpent, Is.True);
            Assert.That(reel.IsSettled, Is.False);
            Assert.That(reel.LidSwing, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(reel.Opacity, Is.EqualTo(1f).Within(Tolerance));

            var midway = reel.Advanced(Take.Seconds * (Take.LidShare + 1f) * 0.5f);

            Assert.That(midway.LidSwing, Is.EqualTo(Take.LidAngle).Within(Tolerance));
            Assert.That(midway.Opacity, Is.GreaterThan(0f));
            Assert.That(midway.Opacity, Is.LessThan(1f));

            var gone = reel.Advanced(Take.Seconds);

            Assert.That(gone.IsSettled, Is.True);
            Assert.That(gone.LidSwing, Is.EqualTo(Take.LidAngle).Within(Tolerance));
            Assert.That(gone.Opacity, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(gone.IsGone, Is.True);
        }

        [Test]
        public void TheLidOnlyEverOpensAndTheChestOnlyEverThins()
        {
            var reel = Take.Begun();
            var swing = reel.LidSwing;
            var opacity = reel.Opacity;

            for (var step = 0; step < 12; step++)
            {
                reel = reel.Advanced(Take.Seconds / 10f);

                Assert.That(reel.LidSwing, Is.GreaterThanOrEqualTo(swing - Tolerance));
                Assert.That(reel.LidSwing, Is.LessThanOrEqualTo(Take.LidAngle + Tolerance));
                Assert.That(reel.Opacity, Is.LessThanOrEqualTo(opacity + Tolerance));
                Assert.That(reel.Opacity, Is.GreaterThanOrEqualTo(0f));

                swing = reel.LidSwing;
                opacity = reel.Opacity;
            }
        }

        [Test]
        public void ASpentPickupIsAlreadyGoneOnTheFrameItIsFirstRead()
        {
            var spent = Take.Spent;

            Assert.That(spent.IsSpent, Is.True);
            Assert.That(spent.IsSettled, Is.True);
            Assert.That(spent.IsGone, Is.True);
            Assert.That(spent.Opacity, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(spent.LidSwing, Is.EqualTo(Take.LidAngle).Within(Tolerance));
            Assert.That(spent.Equals(Take.Begun().Advanced(Take.Seconds)), Is.True);
        }

        [Test]
        public void ALidOpensFarEnoughToReadAsOpenWithoutFallingOffItsHinge()
        {
            Assert.That(Take.LidAngle, Is.GreaterThan(60f));
            Assert.That(Take.LidAngle, Is.LessThan(180f));
        }

        [Test]
        public void ASettledTakeStaysWhereItLanded()
        {
            var settled = Take.Begun().Advanced(Take.Seconds * 2f);

            Assert.That(settled.Advanced(1f), Is.EqualTo(settled));
            Assert.That(Take.None.Advanced(1f), Is.EqualTo(Take.None));
        }

        [Test]
        public void ATakeOnlyEverRunsForwards()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Take.Begun().Advanced(-0.01f));
        }
    }
}
