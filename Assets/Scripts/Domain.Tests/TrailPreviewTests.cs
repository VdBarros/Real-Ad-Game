using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class TrailPreviewTests
    {
        const int Corridor = 0;

        const int CorridorEnemy = 1;

        const int CorridorPrize = 2;

        const int EnemyValue = 9;

        const int PrizeValue = 3;

        static LevelGraph Corridored()
        {
            var builder = new LevelGraphBuilder(RunFixture.Seed, RunFixture.Preset);

            for (var x = 1; x <= 5; x++)
            {
                builder.AddTile(At(x), regionId: 0);
            }

            builder.AddNode(At(1), NodeType.Start);
            builder.AddNode(At(3), NodeType.Enemy, EnemyValue);
            builder.AddNode(At(5), NodeType.Additive, PrizeValue);

            builder.Connect(At(1), At(3), new[] { At(2) });
            builder.Connect(At(3), At(5), new[] { At(4) });

            return builder.Build();
        }

        static TilePosition At(int x)
        {
            return new TilePosition(elevation: 0, x: x, y: 0);
        }

        [Test]
        public void ARouteTheWalkCanFinishIsSafe()
        {
            var preview = TargetPreview.Of(RunState.Begin(Corridored(), EnemyValue + 1), CorridorPrize);

            Assert.That(preview.IsDangerous, Is.False);
            Assert.That(preview.BlockedByNodeId, Is.EqualTo(TapAim.Nothing));
            Assert.That(Trail.MoodOf(preview), Is.EqualTo(TrailMood.Safe));
        }

        [Test]
        public void APrizeReachedThroughAnEnemyTheRunCannotBeatIsDangerousDespiteTheHarmlessDestination()
        {
            var state = RunState.Begin(Corridored(), EnemyValue - 1);
            var preview = TargetPreview.Of(state, CorridorPrize);

            Assert.That(
                state.Level.Decisions.Node(CorridorPrize).Type,
                Is.EqualTo(NodeType.Additive),
                "The destination has to look harmless, or the test proves nothing about the corridor.");
            Assert.That(preview.IsDangerous, Is.True);
            Assert.That(preview.BlockedByNodeId, Is.EqualTo(CorridorEnemy));
            Assert.That(
                preview.BlockedByNodeId,
                Is.Not.EqualTo(preview.NodeId),
                "The danger is read off the corridor, not off what the finger is on.");
            Assert.That(Trail.MoodOf(preview), Is.EqualTo(TrailMood.Dangerous));
        }

        [Test]
        public void AnEnemyMatchedExactlyBlocksTheRouteBecauseATieIsADefeat()
        {
            var preview = TargetPreview.Of(RunState.Begin(Corridored(), EnemyValue), CorridorPrize);

            Assert.That(preview.IsDangerous, Is.True);
            Assert.That(preview.BlockedByNodeId, Is.EqualTo(CorridorEnemy));
        }

        [Test]
        public void AnEnemyStandingOnTheDestinationItselfBlocksItsOwnRoute()
        {
            var preview = TargetPreview.Of(RunState.Begin(Corridored(), EnemyValue - 1), CorridorEnemy);

            Assert.That(preview.IsDangerous, Is.True);
            Assert.That(preview.BlockedByNodeId, Is.EqualTo(CorridorEnemy));
            Assert.That(preview.FightsOnTheWay, Is.EqualTo(0), "Nothing stands between the run and its doorstep.");
        }

        [Test]
        public void TheFightsOnTheWayAreTheOnesTheNavigationMapCounted()
        {
            var state = RunState.Begin(Corridored(), 2);
            var navigation = NavigationMap.Of(state);

            Assert.That(
                TargetPreview.Of(state, CorridorPrize).FightsOnTheWay,
                Is.EqualTo(navigation.FightsOnTheWayTo(CorridorPrize)));
            Assert.That(navigation.FightsOnTheWayTo(CorridorPrize), Is.EqualTo(1));
            Assert.That(navigation.RouteTo(CorridorPrize), Is.EqualTo(new[] { Corridor, CorridorEnemy, CorridorPrize }));
        }

        [Test]
        public void ATrailWornByNothingAimedAtIsSafeRatherThanDangerous()
        {
            Assert.That(TargetPreview.None.IsDangerous, Is.False);
            Assert.That(TargetPreview.None.BlockedByNodeId, Is.EqualTo(TapAim.Nothing));
            Assert.That(Trail.MoodOf(TargetPreview.None), Is.EqualTo(TrailMood.Safe));
        }

        [Test]
        public void ATrailNeedsAPreviewToTakeItsMoodFrom()
        {
            Assert.That(() => Trail.MoodOf(null), Throws.ArgumentNullException);
        }

        [Test]
        public void TheTwoMoodsAreToldApartByBothColourAndSize()
        {
            var safe = Trail.Look(TrailMood.Safe);
            var dangerous = Trail.Look(TrailMood.Dangerous);

            Assert.That(dangerous.Tint, Is.Not.EqualTo(safe.Tint));
            Assert.That(dangerous.Size, Is.GreaterThan(safe.Size));
            Assert.That(
                dangerous.Tint.Red - dangerous.Tint.Green,
                Is.GreaterThan(0.5f),
                "A dangerous trail reads red, not as a shade of the safe one.");
            Assert.That(safe.Size, Is.EqualTo(Trail.Size));
        }

        [Test]
        public void ThereIsNoLookForAMoodThatDoesNotExist()
        {
            Assert.That(() => Trail.Look((TrailMood)7), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void DangerIsExactlyTheWalkFallingShortOfWhereTheFingerPointsOnEveryGeneratedLevel()
        {
            var dangerous = 0;
            var blockedShortOfTheDestination = 0;
            var safe = 0;

            foreach (var preset in new[] { MazePreset.Tiny, MazePreset.Ship, MazePreset.Stress })
            {
                for (var seed = 1L; seed <= 30L; seed++)
                {
                    var graph = LevelGenerator.Generate(seed, preset).Graph;
                    var state = RunState.Begin(graph, PowerTuning.For(preset).StartingPower);

                    for (var move = 0; move < 5 && !state.IsLevelComplete; move++)
                    {
                        var navigation = NavigationMap.Of(state);
                        var stepped = TapAim.Nothing;

                        foreach (var nodeId in TapAim.Aimable(navigation))
                        {
                            var preview = TargetPreview.Of(navigation, nodeId);
                            var route = preview.Route;
                            var arrived = route.Count != 0 && route[route.Count - 1] == nodeId;
                            var walked = ActionResolver.Along(state, navigation.RouteTo(nodeId));

                            Assert.That(
                                preview.IsDangerous,
                                Is.EqualTo(walked.State.PositionNodeId != nodeId),
                                "Seed " + seed + " of " + preset + " reads node " + nodeId
                                + " as dangerous=" + preview.IsDangerous + " where the walk lands on node "
                                + walked.State.PositionNodeId + ".");
                            Assert.That(
                                preview.FightsOnTheWay,
                                Is.EqualTo(navigation.FightsOnTheWayTo(nodeId)),
                                "The preview counted its own fights rather than the navigation map's.");
                            Assert.That(arrived, Is.True);

                            if (!preview.IsDangerous)
                            {
                                safe++;
                                Assert.That(preview.BlockedByNodeId, Is.EqualTo(TapAim.Nothing));
                                continue;
                            }

                            dangerous++;
                            Assert.That(
                                route,
                                Has.Member(preview.BlockedByNodeId),
                                "The blocker has to stand on the route it blocks.");
                            Assert.That(
                                state.BlocksPassage(preview.BlockedByNodeId),
                                Is.True,
                                "Node " + preview.BlockedByNodeId + " blocks a route without being a fight.");

                            if (preview.BlockedByNodeId == nodeId)
                            {
                                continue;
                            }

                            blockedShortOfTheDestination++;
                            Assert.That(
                                preview.FightsOnTheWay,
                                Is.GreaterThan(0),
                                "A route blocked short of its destination crosses a fight the map never counted.");
                            Assert.That(
                                IndexOf(route, preview.BlockedByNodeId),
                                Is.LessThan(route.Count - 1),
                                "The first blocker sits before the destination, so the destination is not the signal.");
                        }

                        foreach (var nodeId in TapAim.Aimable(navigation))
                        {
                            if (!TargetPreview.Of(navigation, nodeId).IsDangerous)
                            {
                                stepped = nodeId;
                                break;
                            }
                        }

                        if (stepped == TapAim.Nothing)
                        {
                            break;
                        }

                        state = ActionResolver.Along(state, navigation.RouteTo(stepped)).State;
                    }
                }
            }

            Console.WriteLine(
                "previews swept: " + safe + " safe, " + dangerous + " dangerous, of which "
                + blockedShortOfTheDestination + " blocked short of the destination");

            Assert.That(safe, Is.GreaterThan(500));
            Assert.That(
                blockedShortOfTheDestination,
                Is.GreaterThan(50),
                "The sweep never met a route whose danger stood before its destination.");
        }

        static int IndexOf(IReadOnlyList<int> route, int nodeId)
        {
            for (var step = 0; step < route.Count; step++)
            {
                if (route[step] == nodeId)
                {
                    return step;
                }
            }

            return -1;
        }
    }
}
