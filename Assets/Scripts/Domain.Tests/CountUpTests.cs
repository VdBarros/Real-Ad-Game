using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class CountUpTests
    {
        const float Frame = 1f / 60f;

        [Test]
        public void ASettledCountUpShowsItsValueAndStaysThere()
        {
            var countUp = CountUp.Settled(7).Advanced(Frame);

            Assert.That(countUp.Display, Is.EqualTo(7));
            Assert.That(countUp.IsSettled, Is.True);
        }

        [Test]
        public void ACountUpLeavesTheOldNumberAndLandsOnTheNewOne()
        {
            var countUp = CountUp.Settled(7).Toward(19);

            Assert.That(countUp.Display, Is.EqualTo(7));
            Assert.That(countUp.IsSettled, Is.False);

            Assert.That(Run(countUp).Display, Is.EqualTo(19));
        }

        [Test]
        public void ACountUpNeverGoesBackwardsAndNeverOvershoots()
        {
            var countUp = CountUp.Settled(3).Toward(408);
            var previous = countUp.Display;

            while (!countUp.IsSettled)
            {
                countUp = countUp.Advanced(Frame);
                Assert.That(countUp.Display, Is.GreaterThanOrEqualTo(previous));
                Assert.That(countUp.Display, Is.LessThanOrEqualTo(408));
                previous = countUp.Display;
            }

            Assert.That(countUp.Display, Is.EqualTo(408));
        }

        [Test]
        public void ASecondChangeCollapsesTheRunningCountUpRatherThanQueueingBehindIt()
        {
            var countUp = CountUp.Settled(10).Toward(100).Advanced(CountUp.Seconds * 0.5f);
            var midway = countUp.Display;

            Assert.That(midway, Is.GreaterThan(10));
            Assert.That(midway, Is.LessThan(100));

            var collapsed = countUp.Toward(250);

            Assert.That(collapsed.Display, Is.EqualTo(midway));
            Assert.That(collapsed.Target, Is.EqualTo(250));
            Assert.That(Run(collapsed).Display, Is.EqualTo(250));
        }

        [Test]
        public void RetargetingToTheValueAlreadyBeingCountedTowardsChangesNothing()
        {
            var running = CountUp.Settled(1).Toward(50).Advanced(Frame);

            Assert.That(running.Toward(50), Is.EqualTo(running));
        }

        [Test]
        public void ACountUpSettlesInsideItsOwnDuration()
        {
            var countUp = CountUp.Settled(1).Toward(600).Advanced(CountUp.Seconds);

            Assert.That(countUp.IsSettled, Is.True);
            Assert.That(countUp.Display, Is.EqualTo(600));
        }

        [Test]
        public void ASettledCountUpMeasuresExactlyTheDigitsItShows()
        {
            foreach (var value in new[] { 0, 1, 9, 10, 99, 100, 4096 })
            {
                Assert.That(
                    CountUp.Settled(value).Digits,
                    Is.EqualTo(BadgeText.Digits(value)),
                    value.ToString());
            }

            Assert.That(Run(CountUp.Settled(7).Toward(1000)).Digits, Is.EqualTo(4f));
        }

        [Test]
        public void TheDigitCountClimbsAlongsideTheNumberRatherThanSnappingOnTheCarry()
        {
            var countUp = CountUp.Settled(9).Toward(1000);
            var previous = countUp.Digits;
            var biggestStep = 0f;

            Assert.That(previous, Is.EqualTo(1f));

            while (!countUp.IsSettled)
            {
                countUp = countUp.Advanced(Frame);

                var step = countUp.Digits - previous;
                Assert.That(step, Is.GreaterThanOrEqualTo(0f), countUp.ToString());
                biggestStep = step > biggestStep ? step : biggestStep;
                previous = countUp.Digits;
            }

            Assert.That(countUp.Digits, Is.EqualTo(4f));
            Assert.That(biggestStep, Is.LessThan(0.5f));
        }

        [Test]
        public void TheDigitCountDoesNotJumpWhenTheCountUpIsRetargetedMidFlight()
        {
            var running = CountUp.Settled(1).Toward(500).Advanced(CountUp.Seconds * 0.5f);
            var before = running.Digits;

            Assert.That(before, Is.GreaterThan(1f));
            Assert.That(before, Is.LessThan(3f));

            var collapsed = running.Toward(5000);

            Assert.That(collapsed.Digits, Is.EqualTo(before));
            Assert.That(Run(collapsed).Digits, Is.EqualTo(4f));
        }

        [Test]
        public void ACountDownNarrowsAsSmoothlyAsACountUpWidens()
        {
            var countUp = CountUp.Settled(1000).Toward(4);
            var previous = countUp.Digits;

            while (!countUp.IsSettled)
            {
                countUp = countUp.Advanced(Frame);

                Assert.That(countUp.Digits, Is.LessThanOrEqualTo(previous), countUp.ToString());
                Assert.That(previous - countUp.Digits, Is.LessThan(0.5f), countUp.ToString());
                previous = countUp.Digits;
            }

            Assert.That(countUp.Digits, Is.EqualTo(1f));
        }

        static CountUp Run(CountUp countUp)
        {
            for (var step = 0; step < 1000 && !countUp.IsSettled; step++)
            {
                countUp = countUp.Advanced(Frame);
            }

            return countUp;
        }
    }
}
