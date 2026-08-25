using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class PowerBeatTests
    {
        const float Frame = 1f / 60f;

        [Test]
        public void ABeatBeginsWearingTheLookItsPowerAlreadyEarns()
        {
            var beat = PowerBeat.Begin(120);

            Assert.That(beat.Shown, Is.EqualTo(120));
            Assert.That(beat.Power, Is.EqualTo(120));
            Assert.That(beat.Look, Is.EqualTo(PlayerLook.Of(120)));
            Assert.That(beat.Scale, Is.EqualTo(PlayerLook.Of(120).Scale).Within(1e-6f));
            Assert.That(beat.IsSettled, Is.True);
        }

        [Test]
        public void TheNumberLandsBeforeTheGrowthStarts()
        {
            var beat = PowerBeat.Begin(2).Toward(9);
            var start = PlayerLook.Of(2);
            var grew = false;

            while (!beat.IsSettled)
            {
                beat = beat.Advanced(Frame);

                if (!beat.HasLanded)
                {
                    Assert.That(beat.Look, Is.EqualTo(start));
                    Assert.That(beat.Scale, Is.EqualTo(start.Scale).Within(1e-6f));
                    continue;
                }

                grew |= beat.Scale > start.Scale;
            }

            Assert.That(beat.Shown, Is.EqualTo(9));
            Assert.That(grew, Is.True);
            Assert.That(beat.Look, Is.EqualTo(PlayerLook.Of(9)));
        }

        [Test]
        public void RapidChangesCollapseSoTheTierIsEvaluatedOnceAndTheBadgeIsNeverStale()
        {
            var beat = PowerBeat.Begin(2).Toward(9).Advanced(CountUp.Seconds * 0.5f);
            Assert.That(beat.HasLanded, Is.False);

            beat = beat.Toward(120);

            var worn = new List<PlayerLook> { beat.Look };
            while (!beat.IsSettled)
            {
                beat = beat.Advanced(Frame);
                if (!beat.Look.Equals(worn[worn.Count - 1]))
                {
                    worn.Add(beat.Look);
                }
            }

            Assert.That(beat.Shown, Is.EqualTo(120));
            Assert.That(worn, Is.EqualTo(new[] { PlayerLook.Of(2), PlayerLook.Of(120) }));
        }

        [Test]
        public void APowerChangeInsideOneTierNeverFiresAPromotion()
        {
            var beat = PowerBeat.Begin(30).Toward(90);
            var scale = beat.Scale;

            while (!beat.IsSettled)
            {
                beat = beat.Advanced(Frame);
                Assert.That(beat.Scale, Is.EqualTo(scale).Within(1e-6f));
            }

            Assert.That(beat.Shown, Is.EqualTo(90));
        }

        [Test]
        public void APromotionAlreadyInFlightKeepsMovingWhileTheNextNumberCountsUp()
        {
            var beat = PowerBeat.Begin(2).Toward(9).Advanced(CountUp.Seconds).Advanced(Frame);

            Assert.That(beat.HasLanded, Is.True);
            Assert.That(beat.Scale, Is.GreaterThan(PlayerLook.Of(2).Scale));

            beat = beat.Toward(400);
            Assert.That(beat.HasLanded, Is.False);

            var previous = beat.Scale;
            var moved = false;
            for (var step = 0; step < 12; step++)
            {
                beat = beat.Advanced(Frame);
                Assert.That(beat.HasLanded, Is.False);
                Assert.That(beat.Look, Is.EqualTo(PlayerLook.Of(9)));
                moved |= beat.Scale != previous;
                previous = beat.Scale;
            }

            Assert.That(moved, Is.True);
        }

        [Test]
        public void RetargetingToThePowerAlreadyBeingCountedTowardsChangesNothing()
        {
            var running = PowerBeat.Begin(2).Toward(120).Advanced(Frame);

            Assert.That(running.Toward(120), Is.EqualTo(running));
        }

        [Test]
        public void ABeatSettlesInsideACountUpFollowedByAPromotion()
        {
            var beat = PowerBeat.Begin(2)
                .Toward(400)
                .Advanced(CountUp.Seconds)
                .Advanced(Promotion.Seconds);

            Assert.That(beat.IsSettled, Is.True);
            Assert.That(beat.Shown, Is.EqualTo(400));
            Assert.That(beat.Look, Is.EqualTo(PlayerLook.Of(400)));
        }

        [Test]
        public void AChangeArrivingDuringThePromotionStillLandsItsOwnNumberAndLook()
        {
            var beat = PowerBeat.Begin(2).Toward(9).Advanced(CountUp.Seconds).Advanced(Frame);

            Assert.That(beat.HasLanded, Is.True);
            Assert.That(beat.IsSettled, Is.False);

            beat = beat.Toward(400);
            Assert.That(beat.HasLanded, Is.False);

            for (var step = 0; step < 1000 && !beat.IsSettled; step++)
            {
                beat = beat.Advanced(Frame);
            }

            Assert.That(beat.Shown, Is.EqualTo(400));
            Assert.That(beat.Look, Is.EqualTo(PlayerLook.Of(400)));
            Assert.That(beat.Scale, Is.EqualTo(PlayerLook.Of(400).Scale).Within(1e-6f));
        }
    }
}
