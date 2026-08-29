using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class SemanticPassageFreezeTests
    {
        const int FrozenSeeds = 120;

        const int FrozenMoves = 40;

        const long FrozenFingerprint = -5327736069986208670L;

        const int FrozenStates = 3527;

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        [Test]
        public void TheSemanticReachableSetIsByteForByteTheOneItAlwaysWas()
        {
            var states = 0;
            var fingerprint = Fingerprint(out states);

            Console.WriteLine(
                "semantic passage fingerprint over " + FrozenSeeds + " seeds of every preset, "
                + states + " states: " + fingerprint);

            Assert.That(
                states,
                Is.EqualTo(FrozenStates),
                "The corpus walk visited a different number of states, so the fingerprint below is "
                + "comparing two different sweeps.");

            Assert.That(
                fingerprint,
                Is.EqualTo(FrozenFingerprint),
                "The semantic reachable set moved. Navigation has leaked into the domain and every "
                + "invariant proof in the backlog rests on it, so the change is wrong, not the number.");
        }

        static long Fingerprint(out int states)
        {
            unchecked
            {
                var hash = 1469598103934665603L;
                states = 0;

                foreach (var preset in EveryPreset())
                {
                    for (var seed = 1; seed <= FrozenSeeds; seed++)
                    {
                        var level = LevelGenerator.Generate(seed, preset);
                        var state = RunState.Begin(level.Graph, level.StartingPower);

                        for (var move = 0; move <= FrozenMoves; move++)
                        {
                            hash = Folded(hash, state);
                            states++;

                            var taken = Step(state);
                            if (taken == null)
                            {
                                break;
                            }

                            state = taken;
                        }
                    }
                }

                return hash;
            }
        }

        static RunState Step(RunState state)
        {
            foreach (var nodeId in state.ReachableNodes)
            {
                if (state.IsConsumed(nodeId))
                {
                    continue;
                }

                var resolved = ActionResolver.Resolve(state, nodeId);
                if (resolved.Outcome != ActionOutcome.Rejected
                    && resolved.State.ConsumedNodes.Count > state.ConsumedNodes.Count)
                {
                    return resolved.State;
                }
            }

            return null;
        }

        static long Folded(long hash, RunState state)
        {
            unchecked
            {
                hash = Folded(hash, state.PositionNodeId);
                hash = Folded(hash, state.Power);
                hash = Folded(hash, state.IsLevelComplete ? 1 : 0);

                foreach (var nodeId in state.ConsumedNodes)
                {
                    hash = Folded(hash, nodeId);
                }

                hash = Folded(hash, state.ReachableNodes.Count);
                foreach (var nodeId in state.ReachableNodes)
                {
                    hash = Folded(hash, nodeId);

                    var route = state.RouteTo(nodeId);
                    hash = Folded(hash, route.Count);
                    foreach (var step in route)
                    {
                        hash = Folded(hash, step);
                    }
                }

                for (var nodeId = 0; nodeId < state.Level.Decisions.Nodes.Count; nodeId++)
                {
                    hash = Folded(hash, state.BlocksPassage(nodeId) ? 1 : 0);
                    hash = Folded(hash, state.IsReachable(nodeId) ? 1 : 0);
                }

                return hash;
            }
        }

        static long Folded(long hash, int value)
        {
            unchecked
            {
                return (hash ^ value) * 1099511628211L;
            }
        }
    }
}
