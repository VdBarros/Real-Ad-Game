using System;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TileGridTests
    {
        [Test]
        public void GridHoldsTheTilesItWasGivenAndNothingElse()
        {
            var grid = new TileGrid(
                new[]
                {
                    new Tile(new TilePosition(floor: 0, x: 1, y: 1), regionId: 0),
                    new Tile(new TilePosition(floor: 0, x: 2, y: 1), regionId: 3)
                },
                Array.Empty<StairLink>());

            Assert.That(grid.Contains(new TilePosition(floor: 0, x: 1, y: 1)), Is.True);
            Assert.That(grid.Contains(new TilePosition(floor: 0, x: 9, y: 9)), Is.False);
            Assert.That(grid.RegionOf(new TilePosition(floor: 0, x: 2, y: 1)), Is.EqualTo(3));
        }

        [Test]
        public void NeighboursAreTheFourConnectedTilesThatExist()
        {
            var grid = new TileGrid(
                new[]
                {
                    new Tile(new TilePosition(floor: 0, x: 1, y: 1), regionId: 0),
                    new Tile(new TilePosition(floor: 0, x: 2, y: 1), regionId: 0),
                    new Tile(new TilePosition(floor: 0, x: 3, y: 1), regionId: 0),
                    new Tile(new TilePosition(floor: 0, x: 2, y: 2), regionId: 0)
                },
                Array.Empty<StairLink>());

            Assert.That(
                grid.Neighbours(new TilePosition(floor: 0, x: 2, y: 1)),
                Is.EqualTo(new[]
                {
                    new TilePosition(floor: 0, x: 1, y: 1),
                    new TilePosition(floor: 0, x: 3, y: 1),
                    new TilePosition(floor: 0, x: 2, y: 2)
                }));

            Assert.That(
                grid.Neighbours(new TilePosition(floor: 0, x: 1, y: 1)),
                Is.EqualTo(new[] { new TilePosition(floor: 0, x: 2, y: 1) }));
        }

        [Test]
        public void OnlyAStairJoinsTheSameColumnOnAdjacentFloors()
        {
            var lower = new TilePosition(floor: 0, x: 3, y: 5);
            var upper = new TilePosition(floor: 1, x: 3, y: 5);
            var grid = new TileGrid(
                new[]
                {
                    new Tile(lower, regionId: 0),
                    new Tile(new TilePosition(floor: 0, x: 4, y: 5), regionId: 0),
                    new Tile(upper, regionId: 1),
                    new Tile(new TilePosition(floor: 1, x: 4, y: 5), regionId: 1)
                },
                new[] { StairLink.Between(lower, upper) });

            Assert.That(
                grid.Neighbours(lower),
                Is.EqualTo(new[] { new TilePosition(floor: 0, x: 4, y: 5), upper }));

            Assert.That(
                grid.Neighbours(upper),
                Is.EqualTo(new[] { lower, new TilePosition(floor: 1, x: 4, y: 5) }));

            Assert.That(
                grid.Neighbours(new TilePosition(floor: 0, x: 4, y: 5)),
                Is.EqualTo(new[] { lower }));
        }

        [Test]
        public void AStairMustJoinTwoTilesThatExist()
        {
            var lower = new TilePosition(floor: 0, x: 3, y: 5);

            Assert.That(
                () => new TileGrid(
                    new[] { new Tile(lower, regionId: 0) },
                    new[] { new StairLink(lower) }),
                Throws.ArgumentException);
        }

        [Test]
        public void TilesAndStairsAreHeldInSweepOrderWhateverOrderTheyArrivedIn()
        {
            var grid = new TileGrid(
                new[]
                {
                    new Tile(new TilePosition(floor: 0, x: 2, y: 1), regionId: 7),
                    new Tile(new TilePosition(floor: 1, x: 2, y: 0), regionId: 9),
                    new Tile(new TilePosition(floor: 0, x: 1, y: 0), regionId: 7),
                    new Tile(new TilePosition(floor: 0, x: 2, y: 0), regionId: 7),
                    new Tile(new TilePosition(floor: 1, x: 1, y: 0), regionId: 9),
                    new Tile(new TilePosition(floor: 0, x: 1, y: 1), regionId: 7)
                },
                new[]
                {
                    new StairLink(new TilePosition(floor: 0, x: 2, y: 0)),
                    new StairLink(new TilePosition(floor: 0, x: 1, y: 0))
                });

            Assert.That(
                grid.Tiles,
                Is.EqualTo(new[]
                {
                    new Tile(new TilePosition(floor: 0, x: 1, y: 0), regionId: 7),
                    new Tile(new TilePosition(floor: 0, x: 2, y: 0), regionId: 7),
                    new Tile(new TilePosition(floor: 0, x: 1, y: 1), regionId: 7),
                    new Tile(new TilePosition(floor: 0, x: 2, y: 1), regionId: 7),
                    new Tile(new TilePosition(floor: 1, x: 1, y: 0), regionId: 9),
                    new Tile(new TilePosition(floor: 1, x: 2, y: 0), regionId: 9)
                }));

            Assert.That(
                grid.Stairs,
                Is.EqualTo(new[]
                {
                    new StairLink(new TilePosition(floor: 0, x: 1, y: 0)),
                    new StairLink(new TilePosition(floor: 0, x: 2, y: 0))
                }));
        }
    }
}
