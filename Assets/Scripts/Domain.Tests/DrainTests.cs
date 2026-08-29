using System;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class DrainTests
    {
        const float Frame = 1f / 60f;

        static int PowerAfter(int power, float seconds)
        {
            return Drain.PowerAfter(power, seconds);
        }

        [Test]
        public void ADrainStartsOnTheWholeOfTheNumberItIsAboutToEat()
        {
            var drain = Drain.Against(54);

            Assert.That(drain.IsHeld, Is.True);
            Assert.That(drain.From, Is.EqualTo(54));
            Assert.That(drain.Power, Is.EqualTo(54));
            Assert.That(drain.Lost, Is.EqualTo(0));
            Assert.That(drain.IsRunning, Is.True);
            Assert.That(drain.IsEmpty, Is.False);
        }

        [Test]
        public void NoContactIsNoDrain()
        {
            Assert.That(Drain.None.IsHeld, Is.False);
            Assert.That(Drain.None.IsRunning, Is.False);
            Assert.That(Drain.None.IsEmpty, Is.False);
            Assert.That(Drain.None.Lost, Is.EqualTo(0));
            Assert.That(Drain.None.Advanced(10f), Is.EqualTo(Drain.None));
        }

        [Test]
        public void TheDrainStopsAtOneAndTheRunGoesOn()
        {
            Assert.That(PowerAfter(54, Drain.Seconds), Is.EqualTo(Drain.Floor));
            Assert.That(PowerAfter(54, Drain.Seconds * 10f), Is.EqualTo(Drain.Floor));
            Assert.That(PowerAfter(99999999, Drain.Seconds), Is.EqualTo(Drain.Floor));
            Assert.That(Drain.Floor, Is.EqualTo(1));
        }

        [Test]
        public void ADrainNeverTakesAPowerOfOneAnyLower()
        {
            var drain = Drain.Against(1);

            Assert.That(drain.Power, Is.EqualTo(1));
            Assert.That(drain.IsEmpty, Is.True);
            Assert.That(drain.IsRunning, Is.False);
            Assert.That(drain.Advanced(10f).Power, Is.EqualTo(1));
        }

        [Test]
        public void APowerBelowTheFloorIsNotAPowerADrainCanRunOn()
        {
            Assert.That(() => Drain.Against(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => Drain.PowerAfter(0, 1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => Drain.PowerAfter(10, -1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => Drain.Against(10).Advanced(-Frame),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ADrainOnlyEverFalls()
        {
            var seen = 54;

            for (var elapsed = 0f; elapsed <= Drain.Seconds + 0.5f; elapsed += Frame)
            {
                var now = PowerAfter(54, elapsed);

                Assert.That(now, Is.LessThanOrEqualTo(seen), "at " + elapsed + "s");
                Assert.That(now, Is.GreaterThanOrEqualTo(Drain.Floor), "at " + elapsed + "s");
                seen = now;
            }

            Assert.That(seen, Is.EqualTo(Drain.Floor));
        }

        [Test]
        public void TheRampMakesTheFirstThirdOfASecondCostLessThanTheSecondThird()
        {
            var ramped = 54 - PowerAfter(54, Drain.RampSeconds);
            var after = PowerAfter(54, Drain.RampSeconds) - PowerAfter(54, Drain.RampSeconds * 2f);

            Assert.That(ramped, Is.LessThan(after));
            Assert.That(Drain.Spent(Drain.RampSeconds * 0.5f), Is.LessThan(Drain.Spent(Drain.RampSeconds) * 0.5f));
        }

        [Test]
        public void PastTheRampTheFallIsLinear()
        {
            var first = Drain.Spent(1.0f) - Drain.Spent(0.5f);
            var second = Drain.Spent(1.5f) - Drain.Spent(1.0f);

            Assert.That(first, Is.EqualTo(second).Within(0.0001f));
        }

        [Test]
        public void AShortBrushCostsVisiblyLessThanALongOne()
        {
            var brushed = 54 - PowerAfter(54, 0.4f);
            var leaned = 54 - PowerAfter(54, 1.2f);

            Assert.That(brushed, Is.GreaterThan(0));
            Assert.That(brushed, Is.LessThan(leaned / 3));
            Assert.That(PowerAfter(54, 0.4f), Is.GreaterThan(PowerAfter(54, 1.2f)));
        }

        [Test]
        public void TheReferenceBossEatsFiftyFourInAboutTwoSeconds()
        {
            Assert.That(PowerAfter(54, 0f), Is.EqualTo(54));
            Assert.That(PowerAfter(54, 0.5f), Is.InRange(40, 50));
            Assert.That(PowerAfter(54, 1.0f), Is.InRange(26, 36));
            Assert.That(PowerAfter(54, 1.5f), Is.InRange(11, 21));
            Assert.That(PowerAfter(54, 2.0f), Is.EqualTo(1));
        }

        [Test]
        public void LettingGoFreezesWhateverIsLeft()
        {
            var held = Drain.Against(54).Advanced(0.8f);
            var letGo = held.Stopped();

            Assert.That(letGo.Power, Is.EqualTo(held.Power));
            Assert.That(letGo.HasLetGo, Is.True);
            Assert.That(letGo.IsRunning, Is.False);
            Assert.That(letGo.Advanced(10f), Is.EqualTo(letGo));
            Assert.That(letGo.Advanced(10f).Power, Is.EqualTo(held.Power));
        }

        [Test]
        public void LettingGoOfNothingIsStillNothing()
        {
            Assert.That(Drain.None.Stopped(), Is.EqualTo(Drain.None));
        }

        [Test]
        public void ADrainRunFrameByFrameLandsWhereOneLongStepDoes()
        {
            var stepped = Drain.Against(54);
            var elapsed = 0f;

            while (elapsed < 1f)
            {
                stepped = stepped.Advanced(Frame);
                elapsed += Frame;
            }

            Assert.That(stepped.Power, Is.EqualTo(PowerAfter(54, elapsed)));
        }

        [Test]
        public void TwoDrainsAreTheSameDrainWhenTheyHaveEatenTheSame()
        {
            var drain = Drain.Against(54).Advanced(0.5f);

            Assert.That(drain, Is.EqualTo(Drain.Against(54).Advanced(0.5f)));
            Assert.That(drain.GetHashCode(), Is.EqualTo(Drain.Against(54).Advanced(0.5f).GetHashCode()));
            Assert.That(drain, Is.Not.EqualTo(Drain.Against(53).Advanced(0.5f)));
            Assert.That(drain, Is.Not.EqualTo(drain.Stopped()));
            Assert.That(Drain.None.ToString(), Is.EqualTo("no drain"));
        }
    }
}
