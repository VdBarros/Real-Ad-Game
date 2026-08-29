using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class DrainInvariantTests
    {
        const int Seeds = 60;

        const int Moves = 60;

        static readonly float[] Contacts = { 0.05f, 0.21f, 0.44f, 0.9f, 1.6f, Drain.Seconds * 2f };

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        [Test]
        public void ThePowerCeilingStillBoundsARunThatDrains()
        {
            var drains = 0;
            var walls = 0;

            foreach (var preset in EveryPreset())
            {
                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var level = LevelGenerator.Generate(seed, preset);
                    var ceiling = PowerCeiling.Of(level.Graph, level.StartingPower);
                    var state = RunState.Begin(level.Graph, level.StartingPower);
                    var values = ValuesOf(level.Graph);
                    var contact = 0;

                    for (var move = 0; move < Moves; move++)
                    {
                        Bounded(state, ceiling, preset, seed);

                        var wall = WallInFrontOf(state);
                        if (wall >= 0)
                        {
                            walls++;
                            var seconds = Contacts[contact++ % Contacts.Length];
                            var left = Drain.PowerAfter(state.Power, seconds);
                            var drained = state.Drained(left);

                            Assert.That(
                                drained.Power,
                                Is.LessThanOrEqualTo(state.Power),
                                "Seed " + seed + " on " + preset.Name + " gained power off a wall.");
                            Assert.That(
                                ValuesOf(drained.Level),
                                Is.EqualTo(values),
                                "A wall changed value while it was eating the run.");
                            Assert.That(drained.IsConsumed(wall), Is.False);
                            Assert.That(drained.BlocksPassage(wall), Is.True);

                            drains += drained.Power < state.Power ? 1 : 0;
                            state = drained;
                            Bounded(state, ceiling, preset, seed);
                        }

                        var taken = Step(state);
                        if (taken == null)
                        {
                            break;
                        }

                        state = taken;
                    }

                    Bounded(state, ceiling, preset, seed);
                }
            }

            Assert.That(walls, Is.GreaterThan(0), "No run ever met a wall, so nothing drained.");
            Assert.That(drains, Is.GreaterThan(0), "No contact cost the run anything, so nothing was proved.");
        }

        [Test]
        public void ADrainedRunIsBoundedByTheSameCeilingAsTheRunItCameFrom()
        {
            var level = LevelSketch.Solvable().Build();
            var ceiling = PowerCeiling.Of(level, LevelSketch.Tuning.StartingPower);
            var opened = RunState.Begin(level, LevelSketch.Tuning.StartingPower);

            var richest = Richest(opened, ceiling);
            var drained = Richest(opened.Drained(Drain.Floor), ceiling);

            Assert.That(drained, Is.LessThanOrEqualTo(richest));
            Assert.That(richest, Is.LessThanOrEqualTo(ceiling));
            Assert.That(drained, Is.GreaterThanOrEqualTo(Drain.Floor));
        }

        [Test]
        public void ADrainNeverOpensAMoveTheFullRunDidNotHave()
        {
            var level = LevelSketch.Branching(deepEnemy: 21, boss: 39).Build();
            var full = RunState.Begin(level, 20);

            for (var left = Drain.Floor; left <= full.Power; left++)
            {
                var drained = full.Drained(left);

                foreach (var nodeId in drained.ReachableNodes)
                {
                    if (drained.IsConsumed(nodeId))
                    {
                        continue;
                    }

                    var poor = ActionResolver.Resolve(drained, nodeId).Outcome;
                    var rich = ActionResolver.Resolve(full, nodeId).Outcome;

                    if (poor == ActionOutcome.Win)
                    {
                        Assert.That(
                            rich,
                            Is.EqualTo(ActionOutcome.Win),
                            "Node " + nodeId + " fell at power " + left + " and stood at power " + full.Power + ".");
                    }
                }

                Assert.That(
                    drained.ReachableNodes,
                    Is.EqualTo(full.ReachableNodes),
                    "Losing power moved what the run can walk to.");
            }
        }

        [Test]
        public void ARunOnTheFloorStillHoldsPowerAndTheLevelIsNotOver()
        {
            var level = LevelSketch.Solvable().Build();
            var floored = RunState.Begin(level, LevelSketch.Tuning.StartingPower).Drained(Drain.Floor);

            Assert.That(floored.Power, Is.EqualTo(1));
            Assert.That(floored.IsLevelComplete, Is.False);
            Assert.That(floored.ReachableNodes, Is.Not.Empty);
            Assert.That(
                ActionResolver.Resolve(floored, LevelSketch.AdditiveNodeId).Outcome,
                Is.EqualTo(ActionOutcome.Walked));
        }

        [Test]
        public void ARunIsNeverDrainedBelowTheFloorOrBackUpwards()
        {
            var state = RunFixture.Begin(startingPower: 10);

            Assert.That(() => state.Drained(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => state.Drained(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => state.Drained(11), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(state.Drained(10), Is.SameAs(state));
        }

        [Test]
        public void ADrainKeepsThePositionAndEverythingAlreadyTaken()
        {
            var afterTheGate = ActionResolver
                .Resolve(RunFixture.Begin(startingPower: 2), RunFixture.GateEnemy)
                .State;

            var drained = afterTheGate.Drained(Drain.Floor);

            Assert.That(drained.Power, Is.EqualTo(Drain.Floor));
            Assert.That(drained.PositionNodeId, Is.EqualTo(afterTheGate.PositionNodeId));
            Assert.That(drained.ConsumedNodes, Is.EqualTo(afterTheGate.ConsumedNodes));
            Assert.That(drained.IsReachable(RunFixture.Boss), Is.True);
        }

        static int[] ValuesOf(LevelGraph level)
        {
            var nodes = level.Decisions.Nodes;
            var values = new int[nodes.Count];

            for (var index = 0; index < nodes.Count; index++)
            {
                values[index] = nodes[index].Value;
            }

            return values;
        }

        static void Bounded(RunState state, long ceiling, MazePreset preset, int seed)
        {
            Assert.That(
                state.Power,
                Is.GreaterThanOrEqualTo(Drain.Floor),
                "Seed " + seed + " on " + preset.Name + " fell through the floor.");
            Assert.That(
                (long)state.Power,
                Is.LessThanOrEqualTo(ceiling),
                "Seed " + seed + " on " + preset.Name + " broke the power ceiling of " + ceiling + ".");
        }

        static int WallInFrontOf(RunState state)
        {
            foreach (var nodeId in state.ReachableNodes)
            {
                if (state.IsConsumed(nodeId) || !state.BlocksPassage(nodeId))
                {
                    continue;
                }

                if (state.Level.Decisions.Node(nodeId).Value >= state.Power)
                {
                    return nodeId;
                }
            }

            return -1;
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

        static long Richest(RunState from, long ceiling)
        {
            var best = (long)from.Power;
            var state = from;

            for (var move = 0; move < Moves; move++)
            {
                var taken = Step(state);
                if (taken == null)
                {
                    break;
                }

                state = taken;
                if (state.Power > best)
                {
                    best = state.Power;
                }

                Assert.That((long)state.Power, Is.LessThanOrEqualTo(ceiling));
            }

            return best;
        }
    }
}
