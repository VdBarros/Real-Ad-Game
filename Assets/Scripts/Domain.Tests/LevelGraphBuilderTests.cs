using System.Linq;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class LevelGraphBuilderTests
    {
        [Test]
        public void NodeIdsFollowTheSweepOrderNotTheOrderNodesWereAdded()
        {
            var builder = new LevelGraphBuilder(seed: 1, preset: "tiny");
            builder.AddTile(new TilePosition(floor: 0, x: 1, y: 0), regionId: 0);
            builder.AddTile(new TilePosition(floor: 0, x: 2, y: 0), regionId: 0);
            builder.AddTile(new TilePosition(floor: 0, x: 1, y: 1), regionId: 0);

            builder.AddNode(new TilePosition(floor: 0, x: 1, y: 1), NodeType.Empty);
            builder.AddNode(new TilePosition(floor: 0, x: 2, y: 0), NodeType.Enemy, value: 4);
            builder.AddNode(new TilePosition(floor: 0, x: 1, y: 0), NodeType.Start);

            var graph = builder.Build();

            Assert.That(
                graph.Decisions.Nodes.Select(node => node.Id),
                Is.EqualTo(new[] { 0, 1, 2 }));

            Assert.That(
                graph.Decisions.Nodes.Select(node => node.Position),
                Is.EqualTo(new[]
                {
                    new TilePosition(floor: 0, x: 1, y: 0),
                    new TilePosition(floor: 0, x: 2, y: 0),
                    new TilePosition(floor: 0, x: 1, y: 1)
                }));

            Assert.That(
                graph.Decisions.Nodes.Select(node => node.Type),
                Is.EqualTo(new[] { NodeType.Start, NodeType.Enemy, NodeType.Empty }));
        }

        [Test]
        public void ACorridorsTilePathRunsFromTheLowNodeIdToTheHighOne()
        {
            var builder = FourTilesInARow();
            builder.AddNode(new TilePosition(floor: 0, x: 1, y: 0), NodeType.Start);
            builder.AddNode(new TilePosition(floor: 0, x: 4, y: 0), NodeType.Boss, value: 9);
            builder.Connect(
                new TilePosition(floor: 0, x: 4, y: 0),
                new TilePosition(floor: 0, x: 1, y: 0),
                new[]
                {
                    new TilePosition(floor: 0, x: 3, y: 0),
                    new TilePosition(floor: 0, x: 2, y: 0)
                });

            var corridor = builder.Build().Decisions.Corridors.Single();

            Assert.That(corridor.LowNodeId, Is.EqualTo(0));
            Assert.That(corridor.HighNodeId, Is.EqualTo(1));
            Assert.That(
                corridor.TilePath,
                Is.EqualTo(new[]
                {
                    new TilePosition(floor: 0, x: 2, y: 0),
                    new TilePosition(floor: 0, x: 3, y: 0)
                }));
        }

        [Test]
        public void ACorridorPathMustBeAnUnbrokenRunBetweenItsTwoNodes()
        {
            var builder = FourTilesInARow();
            builder.AddNode(new TilePosition(floor: 0, x: 1, y: 0), NodeType.Start);
            builder.AddNode(new TilePosition(floor: 0, x: 4, y: 0), NodeType.Boss, value: 9);
            builder.Connect(
                new TilePosition(floor: 0, x: 1, y: 0),
                new TilePosition(floor: 0, x: 4, y: 0),
                new[] { new TilePosition(floor: 0, x: 2, y: 0) });

            Assert.That(() => builder.Build(), Throws.ArgumentException);
        }

        [Test]
        public void ACorridorPathMayNotRunThroughANode()
        {
            var builder = FourTilesInARow();
            builder.AddNode(new TilePosition(floor: 0, x: 1, y: 0), NodeType.Start);
            builder.AddNode(new TilePosition(floor: 0, x: 3, y: 0), NodeType.Enemy, value: 2);
            builder.AddNode(new TilePosition(floor: 0, x: 4, y: 0), NodeType.Boss, value: 9);
            builder.Connect(
                new TilePosition(floor: 0, x: 1, y: 0),
                new TilePosition(floor: 0, x: 4, y: 0),
                new[]
                {
                    new TilePosition(floor: 0, x: 2, y: 0),
                    new TilePosition(floor: 0, x: 3, y: 0)
                });

            Assert.That(() => builder.Build(), Throws.ArgumentException);
        }

        [Test]
        public void ATileMayBelongToTheInteriorOfOnlyOneCorridor()
        {
            var builder = FourTilesInARow();
            builder.AddTile(new TilePosition(floor: 0, x: 2, y: 1), regionId: 0);
            builder.AddNode(new TilePosition(floor: 0, x: 1, y: 0), NodeType.Start);
            builder.AddNode(new TilePosition(floor: 0, x: 3, y: 0), NodeType.Empty);
            builder.AddNode(new TilePosition(floor: 0, x: 2, y: 1), NodeType.Boss, value: 9);
            builder.Connect(
                new TilePosition(floor: 0, x: 1, y: 0),
                new TilePosition(floor: 0, x: 3, y: 0),
                new[] { new TilePosition(floor: 0, x: 2, y: 0) });
            builder.Connect(
                new TilePosition(floor: 0, x: 1, y: 0),
                new TilePosition(floor: 0, x: 2, y: 1),
                new[] { new TilePosition(floor: 0, x: 2, y: 0) });

            Assert.That(() => builder.Build(), Throws.ArgumentException);
        }

        static LevelGraphBuilder FourTilesInARow()
        {
            var builder = new LevelGraphBuilder(seed: 3, preset: "tiny");
            for (var x = 1; x <= 4; x++)
            {
                builder.AddTile(new TilePosition(floor: 0, x: x, y: 0), regionId: 0);
            }

            return builder;
        }
    }
}
