using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TrailTests
    {
        [Test]
        public void ARouteThatGoesNowhereIsDottedWithNothing()
        {
            var dots = Trail.Along(TileRoute.Of(RunFixture.Level(), new[] { RunFixture.Start }));

            Assert.That(dots.Count, Is.EqualTo(0));
        }

        [Test]
        public void EveryStepCarriesTheSameNumberOfDots()
        {
            var route = RunFixture.PastTheMultiplier();
            var dots = Trail.Along(route);

            Assert.That(dots.Count, Is.EqualTo(route.Steps * Trail.DotsPerStep));
        }

        [Test]
        public void NoDotLandsOnATileACharacterStandsOn()
        {
            foreach (var dot in Trail.Along(RunFixture.PastTheMultiplier()))
            {
                Assert.That(dot.Step, Is.Not.EqualTo((float)(int)dot.Step));
            }
        }

        [Test]
        public void DotsRunInOrderAlongTheRoute()
        {
            var dots = Trail.Along(RunFixture.PastTheMultiplier());

            for (var index = 1; index < dots.Count; index++)
            {
                Assert.That(dots[index].Step, Is.GreaterThan(dots[index - 1].Step));
            }
        }

        [Test]
        public void ADotSitsOnTheLineBetweenTheTwoTilesItSpans()
        {
            var dots = Trail.Along(RunFixture.PastTheMultiplier());

            var from = IsoProjection.Of(new TilePosition(0, 3, 2));
            var to = IsoProjection.Of(new TilePosition(0, 2, 2));

            Assert.That(dots[0].Position.X, Is.EqualTo(from.X + (to.X - from.X) * dots[0].Step).Within(0.0001f));
            Assert.That(dots[0].Position.Z, Is.EqualTo(from.Z + (to.Z - from.Z) * dots[0].Step).Within(0.0001f));
            Assert.That(dots[0].Position.Y, Is.EqualTo(from.Y + Trail.Lift).Within(0.0001f));
        }

        [Test]
        public void ADotIsSpentOnceTheWalkerHasPassedIt()
        {
            var dots = Trail.Along(RunFixture.PastTheMultiplier());

            Assert.That(Trail.IsSpent(dots[0], 0f), Is.False);
            Assert.That(Trail.IsSpent(dots[0], dots[0].Step), Is.True);
            Assert.That(Trail.IsSpent(dots[dots.Count - 1], dots[0].Step), Is.False);
        }

        [Test]
        public void ATrailNeedsARouteToRunAlong()
        {
            Assert.That(() => Trail.Along(null), Throws.ArgumentNullException);
        }
    }
}
