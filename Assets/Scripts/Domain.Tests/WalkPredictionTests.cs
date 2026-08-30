using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class WalkPredictionTests
    {
        const float Frame = 1f / 60f;

        const long ShipSeed = 20250824L;

        const int Moves = 6;

        static int Furthest(RunState state)
        {
            var navigation = NavigationMap.Of(state);
            var furthest = TapAim.Nothing;
            var doomed = TapAim.Nothing;
            var steps = 0;
            var doomedSteps = 0;

            foreach (var nodeId in TapAim.Aimable(navigation))
            {
                var resolved = ActionResolver.Along(state, navigation.RouteTo(nodeId));
                if (resolved.Outcome == ActionOutcome.Rejected)
                {
                    continue;
                }

                if (resolved.State.ConsumedNodes.Count == state.ConsumedNodes.Count)
                {
                    if (resolved.Route.Count > doomedSteps)
                    {
                        doomed = nodeId;
                        doomedSteps = resolved.Route.Count;
                    }

                    continue;
                }

                if (resolved.Route.Count > steps)
                {
                    furthest = nodeId;
                    steps = resolved.Route.Count;
                }
            }

            return furthest != TapAim.Nothing ? furthest : doomed;
        }

        static Journey Ran(Journey journey)
        {
            for (var frame = 0; frame < 20000 && !journey.IsOver; frame++)
            {
                journey = journey.Advanced(Frame);

                if (journey.IsWaiting && !journey.HoldsForAFight)
                {
                    journey = journey.Resumed();
                }
            }

            Assert.That(journey.IsOver, Is.True, "A walk was still going when the frames ran out.");

            return journey;
        }

        static List<string> GreedyLine(long seed, MazePreset preset, out int walls)
        {
            var graph = LevelGenerator.Generate(seed, preset).Graph;
            var state = RunState.Begin(graph, PowerTuning.For(preset).StartingPower);
            var complaints = new List<string>();
            walls = 0;

            for (var move = 0; move < Moves && !state.IsLevelComplete; move++)
            {
                var target = Furthest(state);
                if (target == TapAim.Nothing)
                {
                    break;
                }

                var before = state;
                var predicted = ActionResolver.Along(before, NavigationMap.Of(before).RouteTo(target));
                var settling = Journey.LeftAlone(predicted);

                if (settling.Power < predicted.State.Power)
                {
                    walls++;
                }

                state = Ran(Journey.Toward(before, target)).State;

                if (!state.Equals(settling))
                {
                    complaints.Add(
                        "move " + move + " to node " + target + " arrived at power " + state.Power
                        + " on node " + state.PositionNodeId + " where a walk nothing breaks off ends"
                        + " at power " + settling.Power + " on node " + settling.PositionNodeId
                        + ", outcome " + predicted.Outcome);
                }

                if (state.ConsumedNodes.Count == before.ConsumedNodes.Count)
                {
                    break;
                }
            }

            return complaints;
        }

        [Test]
        public void TheGreedyLineOnTheShipSeedLandsWhereLeavingItAloneSays()
        {
            int walls;
            var complaints = GreedyLine(ShipSeed, MazePreset.Ship, out walls);

            Assert.That(complaints, Is.Empty, string.Join("\n", complaints.ToArray()));
            Assert.That(
                walls,
                Is.GreaterThan(0),
                "The greedy line on this seed must still walk into a wall, or it proves nothing.");
        }

        [Test]
        public void EveryPresetWalksItsGreedyLineWhereLeavingItAloneSays()
        {
            var complaints = new List<string>();
            var walls = 0;

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                for (var seed = 1L; seed <= 8L; seed++)
                {
                    int met;
                    foreach (var complaint in GreedyLine(seed, preset, out met))
                    {
                        complaints.Add(preset.Name + " seed " + seed + ": " + complaint);
                    }

                    walls += met;
                }
            }

            Assert.That(complaints, Is.Empty, string.Join("\n", complaints.ToArray()));
            Assert.That(
                walls,
                Is.GreaterThan(0),
                "No greedy line across the corpus walked into a wall, so nothing drained.");
        }

        [Test]
        public void LeavingAWonWalkAloneChangesNothingAboutIt()
        {
            var resolved = ActionResolver.Resolve(RunFixture.Begin(3), RunFixture.DoorstepEnemy);

            Assert.That(resolved.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(Journey.LeftAlone(resolved), Is.SameAs(resolved.State));
        }

        [Test]
        public void LeavingALostWalkAloneBleedsItToTheFloorTheResolverNeverSees()
        {
            var opening = RunFixture.Begin(2);
            var resolved = ActionResolver.Along(
                opening, NavigationMap.Of(opening).RouteTo(RunFixture.Boss));
            var settling = Journey.LeftAlone(resolved);

            Assert.That(resolved.Outcome, Is.EqualTo(ActionOutcome.Loss));
            Assert.That(resolved.State.Power, Is.GreaterThan(Drain.Floor));
            Assert.That(settling.Power, Is.EqualTo(Drain.Floor));
            Assert.That(settling.PositionNodeId, Is.EqualTo(resolved.State.PositionNodeId));
            Assert.That(
                settling.ConsumedNodes.Count, Is.EqualTo(resolved.State.ConsumedNodes.Count));
            Assert.That(settling.IsConsumed(RunFixture.Boss), Is.False);
            Assert.That(
                settling.Level.Decisions.Node(RunFixture.Boss).Value,
                Is.EqualTo(RunFixture.BossValue));
        }

        [Test]
        public void LeavingATiedWalkAloneBleedsItJustAsALossDoes()
        {
            var resolved = ActionResolver.Resolve(
                RunFixture.Begin(RunFixture.DoorstepEnemyValue), RunFixture.DoorstepEnemy);

            Assert.That(resolved.Outcome, Is.EqualTo(ActionOutcome.Tie));
            Assert.That(resolved.State.Power, Is.GreaterThan(Drain.Floor));
            Assert.That(Journey.LeftAlone(resolved).Power, Is.EqualTo(Drain.Floor));
        }

        [Test]
        public void AWalkAlreadyOnTheFloorHasNothingLeftToBleed()
        {
            var resolved = ActionResolver.Resolve(RunFixture.Begin(Drain.Floor), RunFixture.DoorstepEnemy);

            Assert.That(resolved.Outcome, Is.EqualTo(ActionOutcome.Loss));
            Assert.That(resolved.State.Power, Is.EqualTo(Drain.Floor));
            Assert.That(Journey.LeftAlone(resolved), Is.SameAs(resolved.State));
        }

        [Test]
        public void LeavingAWalkAloneAgreesWithRunningTheJourneyOut()
        {
            foreach (var wall in new[] { 8, 22, 60 })
            {
                var level = LevelSketch.Branching(additive: 20, gateEnemy: wall).Build();
                var opening = RunState.Begin(level, 2);
                var resolved = ActionResolver.Resolve(opening, LevelSketch.GateEnemyNodeId);
                var settled = Ran(Journey.Toward(opening, LevelSketch.GateEnemyNodeId));

                Assert.That(settled.State, Is.EqualTo(Journey.LeftAlone(resolved)), wall.ToString());
            }
        }

        [Test]
        public void ARejectedResolutionIsLeftExactlyAsItIs()
        {
            var opening = RunFixture.Begin(3);
            var rejected = ActionResolver.Along(opening, null);

            Assert.That(rejected.Outcome, Is.EqualTo(ActionOutcome.Rejected));
            Assert.That(Journey.LeftAlone(rejected), Is.SameAs(opening));
        }
    }
}
