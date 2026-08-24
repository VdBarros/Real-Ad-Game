using System.Linq;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class DecisionGraphTests
    {
        [Test]
        public void NeighboursOfANodeAreTheNodesItsCorridorsReach()
        {
            var graph = ThreeNodesInALine();

            Assert.That(graph.Decisions.NeighboursOf(0), Is.EqualTo(new[] { 1 }));
            Assert.That(graph.Decisions.NeighboursOf(1), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(graph.Decisions.NeighboursOf(2), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void CorridorsOfANodeCarryTheTilesBetweenIt()
        {
            var graph = ThreeNodesInALine();

            Assert.That(
                graph.Decisions.CorridorsOf(1).Select(corridor => corridor.TilePath.Single()),
                Is.EqualTo(new[]
                {
                    new TilePosition(floor: 0, x: 2, y: 0),
                    new TilePosition(floor: 0, x: 4, y: 0)
                }));
        }

        [Test]
        public void ANodesRegionIsTheRegionOfItsTile()
        {
            var graph = LevelGraphFixture.TwoFloors();

            Assert.That(graph.RegionOf(0), Is.EqualTo(0));
            Assert.That(graph.RegionOf(2), Is.EqualTo(1));
            Assert.That(graph.RegionOf(6), Is.EqualTo(2));
        }

        [Test]
        public void ANodeCanBeFoundByTheTileItStandsOn()
        {
            var graph = LevelGraphFixture.TwoFloors();

            Assert.That(
                graph.Decisions.NodeAt(new TilePosition(floor: 1, x: 6, y: 1)).Type,
                Is.EqualTo(NodeType.Boss));
            Assert.That(graph.Decisions.NodeAt(new TilePosition(floor: 0, x: 3, y: 0)), Is.Null);
        }

        static LevelGraph ThreeNodesInALine()
        {
            var builder = new LevelGraphBuilder(seed: 2, preset: "tiny");
            for (var x = 1; x <= 5; x++)
            {
                builder.AddTile(new TilePosition(floor: 0, x: x, y: 0), regionId: 0);
            }

            builder.AddNode(new TilePosition(floor: 0, x: 1, y: 0), NodeType.Start);
            builder.AddNode(new TilePosition(floor: 0, x: 3, y: 0), NodeType.Empty);
            builder.AddNode(new TilePosition(floor: 0, x: 5, y: 0), NodeType.Boss, value: 10);

            builder.Connect(
                new TilePosition(floor: 0, x: 3, y: 0),
                new TilePosition(floor: 0, x: 5, y: 0),
                new[] { new TilePosition(floor: 0, x: 4, y: 0) });
            builder.Connect(
                new TilePosition(floor: 0, x: 1, y: 0),
                new TilePosition(floor: 0, x: 3, y: 0),
                new[] { new TilePosition(floor: 0, x: 2, y: 0) });

            return builder.Build();
        }
    }
}
