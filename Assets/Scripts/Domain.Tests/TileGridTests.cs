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
                    new Tile(new TilePosition(elevation: 0, x: 1, y: 1), regionId: 0),
                    new Tile(new TilePosition(elevation: 0, x: 2, y: 1), regionId: 3)
                });

            Assert.That(grid.Contains(new TilePosition(elevation: 0, x: 1, y: 1)), Is.True);
            Assert.That(grid.Contains(new TilePosition(elevation: 0, x: 9, y: 9)), Is.False);
            Assert.That(grid.RegionOf(new TilePosition(elevation: 0, x: 2, y: 1)), Is.EqualTo(3));
        }

        [Test]
        public void NeighboursAreTheFourConnectedTilesThatExist()
        {
            var grid = new TileGrid(
                new[]
                {
                    new Tile(new TilePosition(elevation: 0, x: 1, y: 1), regionId: 0),
                    new Tile(new TilePosition(elevation: 0, x: 2, y: 1), regionId: 0),
                    new Tile(new TilePosition(elevation: 0, x: 3, y: 1), regionId: 0),
                    new Tile(new TilePosition(elevation: 0, x: 2, y: 2), regionId: 0)
                });

            Assert.That(
                grid.Neighbours(new TilePosition(elevation: 0, x: 2, y: 1)),
                Is.EqualTo(new[]
                {
                    new TilePosition(elevation: 0, x: 1, y: 1),
                    new TilePosition(elevation: 0, x: 3, y: 1),
                    new TilePosition(elevation: 0, x: 2, y: 2)
                }));

            Assert.That(
                grid.Neighbours(new TilePosition(elevation: 0, x: 1, y: 1)),
                Is.EqualTo(new[] { new TilePosition(elevation: 0, x: 2, y: 1) }));
        }

        [Test]
        public void AStaircaseTileIsANeighbourBecauseItsPlaceIsAdjacentAndForNoOtherReason()
        {
            var foot = new TilePosition(elevation: 0, x: 3, y: 5);
            var step = new TilePosition(elevation: 1, x: 3, y: 6);
            var head = new TilePosition(elevation: 2, x: 3, y: 7);
            var grid = new TileGrid(
                new[]
                {
                    new Tile(foot, regionId: 0),
                    new Tile(new TilePosition(elevation: 0, x: 4, y: 5), regionId: 0),
                    new Tile(step, regionId: 0),
                    new Tile(head, regionId: 1),
                    new Tile(new TilePosition(elevation: 2, x: 4, y: 7), regionId: 1)
                });

            Assert.That(
                grid.Neighbours(foot),
                Is.EqualTo(new[] { new TilePosition(elevation: 0, x: 4, y: 5), step }));

            Assert.That(grid.Neighbours(step), Is.EqualTo(new[] { foot, head }));

            Assert.That(
                grid.Neighbours(head),
                Is.EqualTo(new[] { step, new TilePosition(elevation: 2, x: 4, y: 7) }));
        }

        [Test]
        public void TwoTilesCannotStandInTheSamePlace()
        {
            Assert.That(
                () => new TileGrid(
                    new[]
                    {
                        new Tile(new TilePosition(elevation: 0, x: 3, y: 5), regionId: 0),
                        new Tile(new TilePosition(elevation: 2, x: 3, y: 5), regionId: 1)
                    }),
                Throws.ArgumentException);
        }

        [Test]
        public void TilesAreHeldInSweepOrderWhateverOrderTheyArrivedIn()
        {
            var grid = new TileGrid(
                new[]
                {
                    new Tile(new TilePosition(elevation: 0, x: 2, y: 1), regionId: 7),
                    new Tile(new TilePosition(elevation: 2, x: 2, y: 8), regionId: 9),
                    new Tile(new TilePosition(elevation: 0, x: 1, y: 0), regionId: 7),
                    new Tile(new TilePosition(elevation: 0, x: 2, y: 0), regionId: 7),
                    new Tile(new TilePosition(elevation: 2, x: 1, y: 8), regionId: 9),
                    new Tile(new TilePosition(elevation: 0, x: 1, y: 1), regionId: 7)
                });

            Assert.That(
                grid.Tiles,
                Is.EqualTo(new[]
                {
                    new Tile(new TilePosition(elevation: 0, x: 1, y: 0), regionId: 7),
                    new Tile(new TilePosition(elevation: 0, x: 2, y: 0), regionId: 7),
                    new Tile(new TilePosition(elevation: 0, x: 1, y: 1), regionId: 7),
                    new Tile(new TilePosition(elevation: 0, x: 2, y: 1), regionId: 7),
                    new Tile(new TilePosition(elevation: 2, x: 1, y: 8), regionId: 9),
                    new Tile(new TilePosition(elevation: 2, x: 2, y: 8), regionId: 9)
                }));
        }
    }
}
