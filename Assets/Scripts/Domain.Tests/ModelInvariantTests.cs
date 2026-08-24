using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ModelInvariantTests
    {
        [Test]
        public void EveryNodeTypeSurvivesBeingNamedAndReadBack()
        {
            foreach (NodeType type in Enum.GetValues(typeof(NodeType)))
            {
                Assert.That(NodeTypeNames.TypeNamed(NodeTypeNames.NameOf(type)), Is.EqualTo(type));
            }
        }

        [Test]
        public void ADecisionGraphBuiltDirectlyStillDemandsSweepOrderedIds()
        {
            var outOfOrder = new[]
            {
                new DecisionNode(0, new TilePosition(floor: 0, x: 2, y: 2), NodeType.Start, 0),
                new DecisionNode(1, new TilePosition(floor: 0, x: 1, y: 1), NodeType.Boss, 5)
            };

            Assert.That(
                () => new DecisionGraph(outOfOrder, Array.Empty<Corridor>()),
                Throws.ArgumentException.With.Message.Contains("sweep"));
        }

        [Test]
        public void ALevelGraphBuiltDirectlyStillDemandsNodesStandOnTiles()
        {
            var grid = new TileGrid(
                new[] { new Tile(new TilePosition(floor: 0, x: 1, y: 0), regionId: 0) },
                Array.Empty<StairLink>());
            var adrift = new DecisionGraph(
                new[] { new DecisionNode(0, new TilePosition(floor: 0, x: 9, y: 9), NodeType.Start, 0) },
                Array.Empty<Corridor>());

            Assert.That(
                () => new LevelGraph(1, "tiny", grid, adrift),
                Throws.ArgumentException.With.Message.Contains("no tile"));
        }

        [Test]
        public void ALevelGraphBuiltDirectlyStillDemandsCanonicalCorridorDirection()
        {
            var graph = LevelGraphFixture.TwoFloors();
            var reversed = graph.Decisions.Corridors
                .Select(corridor => corridor.TilePath.Count > 1
                    ? new Corridor(corridor.LowNodeId, corridor.HighNodeId, Backwards(corridor.TilePath))
                    : corridor)
                .ToList();

            Assert.That(
                () => new LevelGraph(
                    graph.Seed,
                    graph.Preset,
                    graph.Tiles,
                    new DecisionGraph(graph.Decisions.Nodes, reversed)),
                Throws.ArgumentException.With.Message.Contains("broken between"));
        }

        [Test]
        public void ALevelGraphBuiltDirectlyStillRefusesTwoCorridorsBetweenTheSameNodes()
        {
            var graph = LevelGraphFixture.TwoFloors();
            var doubled = new List<Corridor>(graph.Decisions.Corridors) { graph.Decisions.Corridors[0] };

            Assert.That(
                () => new DecisionGraph(graph.Decisions.Nodes, doubled),
                Throws.ArgumentException);
        }

        static IReadOnlyList<TilePosition> Backwards(IReadOnlyList<TilePosition> path)
        {
            var reversed = new List<TilePosition>(path);
            reversed.Reverse();
            return reversed;
        }
    }
}
