using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TileFootingTests
    {
        const long Seed = 20250824L;

        [Test]
        public void ATileOneStepAboveALowerNeighbourIsFootedWithAFlight()
        {
            var graph = Ship();
            var flights = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                TileSide down;
                if (!TileFootings.StandsOneStepAbove(graph.Tiles, tile.Position, out down))
                {
                    continue;
                }

                flights++;
                Assert.That(
                    TileFootings.Under(graph.Tiles, tile.Position),
                    Is.EqualTo(TileFooting.Flight),
                    tile.Position.ToString());
            }

            Assert.That(flights, Is.GreaterThan(0));
        }

        [Test]
        public void AClimbingTileLevelWithEveryNeighbourIsFootedWithAPlinth()
        {
            var graph = Ship();
            var plinths = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                TileSide down;
                var stands = TileFootings.StandsOneStepAbove(graph.Tiles, tile.Position, out down);
                var footing = TileFootings.Under(graph.Tiles, tile.Position);

                if (stands)
                {
                    continue;
                }

                plinths++;
                Assert.That(footing, Is.EqualTo(TileFooting.Plinth), tile.Position.ToString());
                Assert.That(TileFootings.IsLevelGround(graph.Tiles, tile.Position), Is.True);
            }

            Assert.That(plinths, Is.GreaterThan(0));
        }

        [Test]
        public void ATerraceTileLevelWithOrBelowEveryNeighbourIsFootedWithNothing()
        {
            var graph = Ship();
            var bare = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                TileSide down;
                if (StaircaseClimb.Climbs(tile.Position)
                    || TileFootings.StandsOneStepAbove(graph.Tiles, tile.Position, out down))
                {
                    continue;
                }

                bare++;
                Assert.That(
                    TileFootings.Under(graph.Tiles, tile.Position),
                    Is.EqualTo(TileFooting.Nothing),
                    tile.Position.ToString());
            }

            Assert.That(bare, Is.GreaterThan(0));
        }

        [Test]
        public void ATerraceTileAtTheHeadOfAClimbIsFootedWithAFlightSoTheClimbTopsOut()
        {
            var graph = Ship();
            var heads = new List<TilePosition>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                if (TileFootings.Under(graph.Tiles, tile.Position) == TileFooting.Flight)
                {
                    heads.Add(tile.Position);
                }
            }

            Assert.That(heads, Is.Not.Empty);

            foreach (var head in heads)
            {
                var ascent = TileFootings.AscentOf(graph.Tiles, head);
                var below = 0;

                foreach (var neighbour in graph.Tiles.Neighbours(head))
                {
                    if (neighbour.Elevation >= head.Elevation)
                    {
                        continue;
                    }

                    if (TileSides.Between(head, neighbour) == TileSides.Opposite(ascent))
                    {
                        below++;
                    }
                }

                Assert.That(below, Is.GreaterThan(0), head.ToString());
                Assert.That(Terraces.IsTerrace(head.Elevation), Is.True, head.ToString());
            }
        }

        [Test]
        public void TheShipLevelFootsTenClimbingTilesAsTwoFlightsAndEightPlinths()
        {
            var graph = Ship();
            var flights = 0;
            var plinths = 0;
            var climbs = 0;

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (!StaircaseClimb.Climbs(tile.Position))
                {
                    continue;
                }

                climbs++;
                switch (TileFootings.Under(graph.Tiles, tile.Position))
                {
                    case TileFooting.Flight:
                        flights++;
                        break;
                    case TileFooting.Plinth:
                        plinths++;
                        break;
                }
            }

            Assert.That(climbs, Is.EqualTo(10));
            Assert.That(flights, Is.EqualTo(2));
            Assert.That(plinths, Is.EqualTo(8));
        }

        [Test]
        public void AFlightIsLaidSoItsClimbLeadsAwayFromTheGroundItStandsAbove()
        {
            var laid = 0;

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                var graph = LevelGenerator.Generate(Seed, preset).Graph;

                foreach (var tile in graph.Tiles.Tiles)
                {
                    TileSide down;
                    if (!TileFootings.StandsOneStepAbove(graph.Tiles, tile.Position, out down))
                    {
                        continue;
                    }

                    laid++;
                    Assert.That(
                        TileFootings.AscentOf(graph.Tiles, tile.Position),
                        Is.EqualTo(TileSides.Opposite(down)),
                        preset + " " + tile.Position);
                }
            }

            Assert.That(laid, Is.GreaterThan(0));
        }

        [Test]
        public void AFootingIsAskedOfTheDomainsOwnGridAndNeverOfBuiltGeometry()
        {
            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                var graph = LevelGenerator.Generate(Seed, preset).Graph;

                foreach (var tile in graph.Tiles.Tiles)
                {
                    var footing = TileFootings.Under(graph.Tiles, tile.Position);

                    Assert.That(
                        footing == TileFooting.Plinth && !StaircaseClimb.Climbs(tile.Position),
                        Is.False,
                        tile.Position.ToString());
                    Assert.That(
                        footing == TileFooting.Nothing && StaircaseClimb.Climbs(tile.Position),
                        Is.False,
                        tile.Position.ToString());
                }
            }
        }

        [Test]
        public void AskingATileThatStandsAboveNothingWhereItsFlightClimbsIsARefusal()
        {
            var graph = Ship();
            var level = new TilePosition(0, 0, 0);

            foreach (var tile in graph.Tiles.Tiles)
            {
                if (TileFootings.Under(graph.Tiles, tile.Position) != TileFooting.Flight)
                {
                    level = tile.Position;
                    break;
                }
            }

            Assert.That(() => TileFootings.AscentOf(graph.Tiles, level), Throws.ArgumentException);
            Assert.That(
                () => TileFootings.Under(null, level),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        static LevelGraph Ship()
        {
            return LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
        }
    }
}
