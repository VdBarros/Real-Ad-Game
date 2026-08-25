using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class WalkSweepTests
    {
        const int Seeds = 120;

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        [TestCaseSource(nameof(EveryPreset))]
        public void NoWalkAnywhereInAGeneratedLevelEverCrossesAWall(MazePreset preset)
        {
            for (var seed = 1; seed <= Seeds; seed++)
            {
                var level = LevelGenerator.Generate(seed, preset);
                var state = RunState.Begin(level.Graph, level.StartingPower);

                for (var taken = Step(level.Graph, state); taken != null; taken = Step(level.Graph, state))
                {
                    state = taken;
                }
            }
        }

        static RunState Step(LevelGraph graph, RunState state)
        {
            RunState eaten = null;

            foreach (var nodeId in state.ReachableNodes)
            {
                AssertNoWallIsCrossedTo(graph, state, nodeId);

                if (eaten != null || state.IsConsumed(nodeId))
                {
                    continue;
                }

                var resolved = ActionResolver.Resolve(state, nodeId);
                if (resolved.Outcome != ActionOutcome.Rejected
                    && resolved.State.ConsumedNodes.Count > state.ConsumedNodes.Count)
                {
                    eaten = resolved.State;
                }
            }

            return eaten;
        }

        static void AssertNoWallIsCrossedTo(LevelGraph graph, RunState state, int nodeId)
        {
            var route = state.RouteTo(nodeId);

            Assert.That(route, Is.Not.Null, "Node " + nodeId + " was reachable but had no route.");

            TileRouteTests.AssertNoWallIsCrossed(graph, TileRoute.Of(graph, route));
        }
    }
}
