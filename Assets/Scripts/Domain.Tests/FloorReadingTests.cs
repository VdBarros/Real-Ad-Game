using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FloorReadingTests
    {
        static readonly TilePosition[] ClearedAtTheOpening =
        {
            At(3, 0), At(4, 0), At(3, 1), At(1, 2), At(2, 2), At(3, 2), At(4, 2), At(1, 3), At(1, 4)
        };

        static readonly TilePosition[] CursedAtTheOpening = { At(5, 0), At(6, 0), At(7, 0), At(5, 2) };

        [Test]
        public void TheOpeningClearsEverythingNoEnemyStandsBetween()
        {
            var reading = FloorReading.Of(RunFixture.Begin(3));

            Assert.That(reading.Cleared, Is.EqualTo(Sorted(ClearedAtTheOpening)));
        }

        [Test]
        public void AnUnconsumedEnemyLeavesItsOwnTileCursedTogetherWithWhatItGuards()
        {
            var reading = FloorReading.Of(RunFixture.Begin(3));

            foreach (var position in CursedAtTheOpening)
            {
                Assert.That(reading.IsCleared(position), Is.False, position + " sits behind an enemy still standing.");
            }
        }

        [Test]
        public void DefeatingAnEnemyClearsItsTileAndTheCorridorBehindIt()
        {
            var opening = RunFixture.Begin(3);
            var after = ActionResolver.Resolve(opening, RunFixture.GateEnemy);

            Assert.That(after.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(
                FloorReading.Of(after.State).Since(FloorReading.Of(opening)),
                Is.EqualTo(Sorted(new[] { At(5, 0), At(6, 0) })),
                "The gate enemy tile and the corridor behind it flip together, and the boss room stays cursed.");
        }

        [Test]
        public void AnEnemyGuardingNothingClearsOnlyItsOwnTile()
        {
            var opening = RunFixture.Begin(3);
            var after = ActionResolver.Resolve(opening, RunFixture.DoorstepEnemy);

            Assert.That(after.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(
                FloorReading.Of(after.State).Since(FloorReading.Of(opening)),
                Is.EqualTo(Sorted(new[] { At(5, 2) })));
        }

        [Test]
        public void TheBossRoomStaysCursedUntilTheBossItselfFalls()
        {
            var gateDown = ActionResolver.Resolve(RunFixture.Begin(40), RunFixture.GateEnemy).State;
            var bossDown = ActionResolver.Resolve(gateDown, RunFixture.Boss);

            Assert.That(bossDown.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(bossDown.State.IsLevelComplete, Is.True);
            Assert.That(FloorReading.Of(gateDown).IsCleared(At(7, 0)), Is.False);
            Assert.That(
                FloorReading.Of(bossDown.State).Since(FloorReading.Of(gateDown)),
                Is.EqualTo(Sorted(new[] { At(7, 0) })));
        }

        [Test]
        public void EveryTileIsClearedOnceTheLevelIsDone()
        {
            var gateDown = ActionResolver.Resolve(RunFixture.Begin(40), RunFixture.GateEnemy).State;
            var doorstepDown = ActionResolver.Resolve(gateDown, RunFixture.DoorstepEnemy).State;
            var bossDown = ActionResolver.Resolve(doorstepDown, RunFixture.Boss).State;

            Assert.That(
                FloorReading.Of(bossDown).Cleared.Count,
                Is.EqualTo(bossDown.Level.Tiles.Tiles.Count));
        }

        [Test]
        public void APickupNeverGatesTheFloor()
        {
            var reading = FloorReading.Of(RunFixture.Begin(3));

            Assert.That(reading.IsCleared(At(1, 2)), Is.True, "An unconsumed multiplier is not a door.");
            Assert.That(reading.IsCleared(At(1, 4)), Is.True, "An unconsumed additive is not a door.");
        }

        [Test]
        public void TheReadingFollowsTheConsumedSetAndNotThePlayer()
        {
            var forward = ActionResolver.Resolve(RunFixture.Begin(3), RunFixture.DoorstepEnemy).State;
            var backAtTheStart = ActionResolver.Resolve(forward, RunFixture.Start).State;
            var richer = ActionResolver.Resolve(RunFixture.Begin(40), RunFixture.DoorstepEnemy).State;

            Assert.That(backAtTheStart.PositionNodeId, Is.Not.EqualTo(forward.PositionNodeId));
            Assert.That(richer.Power, Is.Not.EqualTo(forward.Power));
            Assert.That(FloorReading.Of(backAtTheStart).Cleared, Is.EqualTo(FloorReading.Of(forward).Cleared));
            Assert.That(FloorReading.Of(richer).Cleared, Is.EqualTo(FloorReading.Of(forward).Cleared));
        }

        [Test]
        public void EveryClearedTileWalksBackToTheStartThroughClearedTiles()
        {
            foreach (var state in Walks(seeds: 12))
            {
                var reading = FloorReading.Of(state);
                var grid = state.Level.Tiles;
                var start = StartTile(state);
                var seen = new HashSet<TilePosition> { start };
                var queue = new Queue<TilePosition>();
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    foreach (var neighbour in grid.Neighbours(queue.Dequeue()))
                    {
                        if (reading.IsCleared(neighbour) && seen.Add(neighbour))
                        {
                            queue.Enqueue(neighbour);
                        }
                    }
                }

                Assert.That(seen.Count, Is.EqualTo(reading.Cleared.Count), "A cleared island floats off the start.");
            }
        }

        [Test]
        public void ClearedGroundIsNeverGivenBack()
        {
            foreach (var run in Runs(seeds: 12))
            {
                var earlier = FloorReading.Of(run[0]);

                for (var step = 1; step < run.Count; step++)
                {
                    var later = FloorReading.Of(run[step]);

                    foreach (var position in earlier.Cleared)
                    {
                        Assert.That(later.IsCleared(position), Is.True, position + " went back to cursed.");
                    }

                    earlier = later;
                }
            }
        }

        [Test]
        public void AConsumedNodeAlwaysStandsOnClearedGround()
        {
            foreach (var state in Walks(seeds: 12))
            {
                var reading = FloorReading.Of(state);

                foreach (var nodeId in state.ConsumedNodes)
                {
                    Assert.That(
                        reading.IsCleared(state.Level.Decisions.Node(nodeId).Position),
                        Is.True,
                        "Node " + nodeId + " is consumed, so the ground under it is cleared.");
                }
            }
        }

        [Test]
        public void AnEnemyStillStandingKeepsItsOwnTileCursed()
        {
            foreach (var state in Walks(seeds: 12))
            {
                var reading = FloorReading.Of(state);

                foreach (var node in state.Level.Decisions.Nodes)
                {
                    if (!state.BlocksPassage(node.Id))
                    {
                        continue;
                    }

                    Assert.That(reading.IsCleared(node.Position), Is.False, "Node " + node.Id + " is still a door.");
                }
            }
        }

        [Test]
        public void TheReadingIsTheSameEveryTimeItIsTaken()
        {
            var state = RunFixture.Begin(3);

            Assert.That(FloorReading.Of(state).Cleared, Is.EqualTo(FloorReading.Of(state).Cleared));
        }

        [Test]
        public void NothingOutsideTheGridIsEverCleared()
        {
            foreach (var state in Walks(seeds: 6))
            {
                foreach (var position in FloorReading.Of(state).Cleared)
                {
                    Assert.That(state.Level.Tiles.Contains(position), Is.True);
                }
            }
        }

        internal static IEnumerable<RunState> Walks(int seeds)
        {
            foreach (var run in Runs(seeds))
            {
                foreach (var state in run)
                {
                    yield return state;
                }
            }
        }

        internal static IEnumerable<IReadOnlyList<RunState>> Runs(int seeds)
        {
            for (var seed = 0; seed < seeds; seed++)
            {
                yield return GreedyRun(LevelGenerator.Generate(seed, MazePreset.Tiny).Graph);
            }
        }

        internal static IReadOnlyList<RunState> GreedyRun(LevelGraph graph)
        {
            var state = RunState.Begin(graph, PowerTuning.For(MazePreset.Tiny).StartingPower);
            var run = new List<RunState> { state };

            while (!state.IsLevelComplete)
            {
                var next = state;

                foreach (var nodeId in state.ReachableNodes)
                {
                    if (state.IsConsumed(nodeId) || !IsContent(state.Level.Decisions.Node(nodeId).Type))
                    {
                        continue;
                    }

                    var result = ActionResolver.Resolve(state, nodeId);
                    if (!result.State.IsConsumed(nodeId))
                    {
                        continue;
                    }

                    next = result.State;
                    break;
                }

                if (next.ConsumedNodes.Count == state.ConsumedNodes.Count)
                {
                    break;
                }

                state = next;
                run.Add(state);
            }

            return run;
        }

        static bool IsContent(NodeType type)
        {
            return type == NodeType.Enemy
                || type == NodeType.Boss
                || type == NodeType.Additive
                || type == NodeType.Multiplier;
        }

        internal static TilePosition StartTile(RunState state)
        {
            foreach (var node in state.Level.Decisions.Nodes)
            {
                if (node.Type == NodeType.Start)
                {
                    return node.Position;
                }
            }

            throw new AssertionException("A run needs a start to read the floor from.");
        }

        static IReadOnlyList<TilePosition> Sorted(IEnumerable<TilePosition> positions)
        {
            return positions.OrderBy(position => position).ToList();
        }

        static TilePosition At(int x, int y)
        {
            return new TilePosition(elevation: 0, x: x, y: y);
        }
    }
}
