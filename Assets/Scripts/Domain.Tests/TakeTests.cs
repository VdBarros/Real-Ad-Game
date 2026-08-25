using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TakeTests
    {
        static readonly Tint Gem = new Tint(0.25f, 0.55f, 0.95f);

        const float Tolerance = 1e-5f;

        [Test]
        public void AnUntakenPickupIsTheWholeCubeAndHasNothingToPlay()
        {
            var full = Take.None;

            Assert.That(full.IsSpent, Is.False);
            Assert.That(full.IsSettled, Is.True);
            Assert.That(full.Edge, Is.EqualTo(LevelBlueprintBuilder.PickupScale));
            Assert.That(full.Height, Is.EqualTo(LevelBlueprintBuilder.PickupScale));
            Assert.That(full.Wash(Gem), Is.EqualTo(Gem));
            Assert.That(default(Take).Equals(full), Is.True);
        }

        [Test]
        public void TakingAPickupLeavesAPedestalWiderAndFlatterThanTheCube()
        {
            Assert.That(Take.PedestalEdge, Is.GreaterThan(LevelBlueprintBuilder.PickupScale));
            Assert.That(Take.PedestalHeight, Is.LessThan(LevelBlueprintBuilder.PickupScale));
        }

        [Test]
        public void ATakeCollapsesTheCubeOntoItsPedestal()
        {
            var reel = Take.Begun();

            Assert.That(reel.IsSpent, Is.True);
            Assert.That(reel.IsSettled, Is.False);
            Assert.That(reel.Edge, Is.EqualTo(LevelBlueprintBuilder.PickupScale).Within(Tolerance));
            Assert.That(reel.Height, Is.EqualTo(LevelBlueprintBuilder.PickupScale).Within(Tolerance));

            var landed = reel.Advanced(Take.Seconds);

            Assert.That(landed.IsSettled, Is.True);
            Assert.That(landed.Edge, Is.EqualTo(Take.PedestalEdge).Within(Tolerance));
            Assert.That(landed.Height, Is.EqualTo(Take.PedestalHeight).Within(Tolerance));
        }

        [Test]
        public void TheCubeOnlyEverSpreadsAndFlattens()
        {
            var reel = Take.Begun();
            var edge = reel.Edge;
            var height = reel.Height;

            for (var step = 0; step < 12; step++)
            {
                reel = reel.Advanced(Take.Seconds / 10f);

                Assert.That(reel.Edge, Is.GreaterThanOrEqualTo(edge - Tolerance));
                Assert.That(reel.Height, Is.LessThanOrEqualTo(height + Tolerance));

                edge = reel.Edge;
                height = reel.Height;
            }
        }

        [Test]
        public void ASpentPickupIsAPedestalFromTheFirstFrame()
        {
            var spent = Take.Spent;

            Assert.That(spent.IsSpent, Is.True);
            Assert.That(spent.IsSettled, Is.True);
            Assert.That(spent.Edge, Is.EqualTo(Take.PedestalEdge).Within(Tolerance));
            Assert.That(spent.Height, Is.EqualTo(Take.PedestalHeight).Within(Tolerance));
            Assert.That(spent.Equals(Take.Begun().Advanced(Take.Seconds)), Is.True);
        }

        [Test]
        public void ThePedestalWearsStoneAndTheCubeWearsItsOwnColour()
        {
            var stone = Take.Spent.Wash(Gem);
            var fromWhite = Take.Spent.Wash(new Tint(1f, 1f, 1f));

            Assert.That(stone, Is.Not.EqualTo(Gem));
            Assert.That(Take.Begun().Wash(Gem), Is.EqualTo(Gem));
            Assert.That(fromWhite.Red, Is.EqualTo(stone.Red).Within(Tolerance));
            Assert.That(fromWhite.Green, Is.EqualTo(stone.Green).Within(Tolerance));
            Assert.That(fromWhite.Blue, Is.EqualTo(stone.Blue).Within(Tolerance));
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
