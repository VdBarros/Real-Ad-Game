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
