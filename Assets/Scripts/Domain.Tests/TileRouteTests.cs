using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TileRouteTests
    {
        [Test]
        public void ARouteOfOneNodeStandsStill()
        {
            var level = RunFixture.Level();
            var route = TileRoute.Of(level, new[] { RunFixture.Start });

            Assert.That(route.Steps, Is.EqualTo(0));
            Assert.That(route.Tiles.Count, Is.EqualTo(1));
            Assert.That(route.Tiles[0], Is.EqualTo(level.Decisions.Node(RunFixture.Start).Position));
            Assert.That(route.TileOf(0), Is.EqualTo(0));
        }

        [Test]
        public void AHopRunsThroughTheCorridorsInteriorTiles()
        {
            var level = RunFixture.Level();
            var route = TileRoute.Of(level, new[] { RunFixture.Start, RunFixture.Additive });

            Assert.That(
                route.Tiles,
                Is.EqualTo(new[]
                {
                    new TilePosition(0, 3, 2),
                    new TilePosition(0, 3, 1),
                    new TilePosition(0, 3, 0)
                }));

            Assert.That(route.TileOf(0), Is.EqualTo(0));
            Assert.That(route.TileOf(1), Is.EqualTo(2));
        }

        [Test]
        public void ACorridorWalkedAgainstItsStoredOrderIsReversedRatherThanReplayed()
        {
            var level = RunFixture.Level();
            var outward = TileRoute.Of(level, new[] { RunFixture.Start, RunFixture.Multiplier });
            var back = TileRoute.Of(level, new[] { RunFixture.Multiplier, RunFixture.Start });

            var reversed = new List<TilePosition>(outward.Tiles);
            reversed.Reverse();

            Assert.That(back.Tiles, Is.EqualTo(reversed));
        }

        [Test]
        public void AMultiHopRouteJoinsItsCorridorsWithoutRepeatingTheNodesBetweenThem()
        {
            var level = RunFixture.Level();
            var route = TileRoute.Of(
                level, new[] { RunFixture.Start, RunFixture.Multiplier, RunFixture.AdditiveBeyondTheMultiplier });

            Assert.That(
                route.Tiles,
                Is.EqualTo(new[]
                {
                    new TilePosition(0, 3, 2),
                    new TilePosition(0, 2, 2),
                    new TilePosition(0, 1, 2),
                    new TilePosition(0, 1, 3),
                    new TilePosition(0, 1, 4)
                }));

            Assert.That(route.TileOf(1), Is.EqualTo(2));
            Assert.That(route.TileOf(2), Is.EqualTo(4));
        }

        [Test]
        public void EveryStepOfARouteCrossesIntoATileTheGridSaysIsAdjacent()
        {
            var level = RunFixture.Level();
            var route = TileRoute.Of(
                level, new[] { RunFixture.Start, RunFixture.Multiplier, RunFixture.AdditiveBeyondTheMultiplier });

            AssertNoWallIsCrossed(level, route);
        }

        [Test]
        public void ARouteClimbsAStairTheSameWayItWalksACorridor()
        {
            var level = LevelGraphFixture.TwoTerraces();
            var route = TileRoute.Of(level, new[] { 1, 4 });

            Assert.That(route.Tiles, Is.EqualTo(new[] { new TilePosition(0, 5, 0), new TilePosition(2, 5, 0) }));
            AssertNoWallIsCrossed(level, route);
        }

        [Test]
        public void ARouteCutShortEndsAtTheNodeItWasCutAt()
        {
            var level = RunFixture.Level();
            var route = TileRoute.Of(
                level, new[] { RunFixture.Start, RunFixture.Multiplier, RunFixture.AdditiveBeyondTheMultiplier });

            var cut = route.Upto(1);

            Assert.That(cut.Nodes, Is.EqualTo(new[] { RunFixture.Start, RunFixture.Multiplier }));
            Assert.That(cut.Steps, Is.EqualTo(2));
            Assert.That(cut.Tiles[cut.Tiles.Count - 1], Is.EqualTo(new TilePosition(0, 1, 2)));
        }

        [Test]
        public void ARouteCutAtItsLastNodeIsTheRouteItself()
        {
            var level = RunFixture.Level();
            var route = TileRoute.Of(level, new[] { RunFixture.Start, RunFixture.Multiplier });

            Assert.That(route.Upto(1), Is.SameAs(route));
        }

        [Test]
        public void TwoNodesWithNoCorridorBetweenThemAreNotARoute()
        {
            var level = RunFixture.Level();

            Assert.That(
                () => TileRoute.Of(level, new[] { RunFixture.Start, RunFixture.Boss }),
                Throws.ArgumentException);
        }

        [Test]
        public void NothingTappedIsNoRouteAtAll()
        {
            Assert.That(() => TileRoute.Of(RunFixture.Level(), null), Throws.ArgumentNullException);
            Assert.That(() => TileRoute.Of(null, new[] { 0 }), Throws.ArgumentNullException);
            Assert.That(() => TileRoute.Of(RunFixture.Level(), new int[0]), Throws.ArgumentException);
        }

        internal static void AssertNoWallIsCrossed(LevelGraph level, TileRoute route)
        {
            for (var step = 0; step < route.Tiles.Count; step++)
            {
                Assert.That(
                    level.Tiles.Contains(route.Tiles[step]),
                    Is.True,
                    "The walk stepped onto " + route.Tiles[step] + ", where there is no tile.");

                if (step == 0)
                {
                    continue;
                }

                Assert.That(
                    level.Tiles.AreAdjacent(route.Tiles[step - 1], route.Tiles[step]),
                    Is.True,
                    "The walk crossed a wall between "
                    + route.Tiles[step - 1] + " and " + route.Tiles[step] + ".");
            }
        }
    }
}
