using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class WeaponFlightTests
    {
        const float Frame = 1f / 60f;

        static readonly WorldPoint Site = new WorldPoint(4f, 0.4f, 7f);

        static readonly WorldPoint Carrier = new WorldPoint(1f, 0.4f, 2f);

        [Test]
        public void NothingIsFlyingUntilAnEnemyDropsSomething()
        {
            Assert.That(WeaponFlight.None.IsSettled, Is.True);
            Assert.That(default(WeaponFlight).IsSettled, Is.True);
        }

        [Test]
        public void AWeaponLeavesTheDeathSiteAndArrivesOnThePlayer()
        {
            var flight = WeaponFlight.From(Site, Carrier);

            Assert.That(flight.Position, Is.EqualTo(Site));

            while (!flight.IsSettled)
            {
                flight = flight.Advanced(Frame);
            }

            Assert.That(flight.Position.X, Is.EqualTo(Carrier.X).Within(1e-5f));
            Assert.That(flight.Position.Y, Is.EqualTo(Carrier.Y).Within(1e-5f));
            Assert.That(flight.Position.Z, Is.EqualTo(Carrier.Z).Within(1e-5f));
        }

        [Test]
        public void TheWeaponArcsOverTheGroundRatherThanSlidingAlongIt()
        {
            var flight = WeaponFlight.From(Site, Carrier).Advanced(WeaponFlight.Seconds * 0.5f);

            Assert.That(flight.Position.Y, Is.EqualTo(Site.Y + WeaponFlight.Arc).Within(1e-4f));
            Assert.That(flight.Spin, Is.GreaterThan(0f));
        }

        [Test]
        public void AFlightLandsOnTheSameBeatAsThePromotionItMergesInto()
        {
            Assert.That(WeaponFlight.Seconds, Is.EqualTo(Promotion.Seconds));
            Assert.That(WeaponFlight.From(Site, Carrier).Advanced(WeaponFlight.Seconds).IsSettled, Is.True);
        }

        [Test]
        public void AFlightOnlyEverRunsForwards()
        {
            Assert.That(
                () => WeaponFlight.From(Site, Carrier).Advanced(-Frame),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }
    }
}
