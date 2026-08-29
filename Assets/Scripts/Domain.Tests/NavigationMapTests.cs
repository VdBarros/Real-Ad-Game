using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class NavigationMapTests
    {
        const int Seeds = 60;

        const int Moves = 8;

        static IEnumerable<MazePreset> EveryPreset()
        {
            yield return MazePreset.Tiny;
            yield return MazePreset.Ship;
        }

        [Test]
        public void ANavigationMapIsBuiltFromARun()
        {
            Assert.Throws<ArgumentNullException>(() => NavigationMap.Of(null));
        }

        [Test]
        public void NoNodeCarriesAnIdOutsideTheGraph()
        {
            var navigation = NavigationMap.Of(RunFixture.Begin(2));

            Assert.Throws<ArgumentOutOfRangeException>(() => navigation.IsNavigable(99));
            Assert.Throws<ArgumentOutOfRangeException>(() => navigation.RouteTo(-1));
        }

        [Test]
        public void WhatSitsBehindAnUnbeatenEnemyIsNavigableEvenThoughItIsNotReachable()
        {
            var state = RunFixture.Begin(1);
            var navigation = NavigationMap.Of(state);

            Assert.That(state.IsReachable(RunFixture.Boss), Is.False);
            Assert.That(navigation.IsNavigable(RunFixture.Boss), Is.True);
            Assert.That(
                navigation.RouteTo(RunFixture.Boss),
                Is.EqualTo(new[]
                {
                    RunFixture.Start, RunFixture.Additive, RunFixture.GateEnemy, RunFixture.Boss
                }));
            Assert.That(navigation.FightsOnTheWayTo(RunFixture.Boss), Is.EqualTo(1));
        }

        [Test]
        public void EveryNodeOfAConnectedLevelIsNavigableSoEveryTapProducesARoute()
        {
            var navigation = NavigationMap.Of(RunFixture.Begin(1));

            foreach (var node in RunFixture.Level().Decisions.Nodes)
            {
                Assert.That(
                    navigation.IsNavigable(node.Id),
                    Is.True,
                    "Node " + node.Id + " answers the navigation question with a no.");
            }
        }

        [Test]
        public void TheNavigableSetIsAStrictSupersetOfTheReachableSetWhenAnUnbeatenEnemyCutsTheLevel()
        {
            var state = RunFixture.Begin(1);
            var navigation = NavigationMap.Of(state);

            foreach (var nodeId in state.ReachableNodes)
            {
                Assert.That(navigation.IsNavigable(nodeId), Is.True);
            }

            Assert.That(state.BlocksPassage(RunFixture.GateEnemy), Is.True);
            Assert.That(
                navigation.NavigableNodes.Count,
                Is.GreaterThan(state.ReachableNodes.Count),
                "An unbeaten enemy cuts this level in two, so navigation has to reach further than passage.");
        }

        [Test]
        public void NavigationIsNeverNarrowerThanPassageAnywhereInTheSeedCorpus()
        {
            var states = 0;
            var cut = 0;
            var whole = 0;

            foreach (var state in Corpus())
            {
                var navigation = NavigationMap.Of(state);
                states++;

                foreach (var nodeId in state.ReachableNodes)
                {
                    Assert.That(
                        navigation.IsNavigable(nodeId),
                        Is.True,
                        "Node " + nodeId + " is reachable but navigation refuses it.");
                    Assert.That(
                        navigation.FightsOnTheWayTo(nodeId),
                        Is.EqualTo(0),
                        "Node " + nodeId + " is reachable, so navigation must not route a fight into it.");
                }

                var barring = Barring(state);

                if (navigation.NavigableNodes.Count > state.ReachableNodes.Count)
                {
                    cut++;

                    Assert.That(
                        barring,
                        Is.GreaterThan(0),
                        "Navigation reached further than passage with no unbeaten enemy to explain it.");

                    foreach (var nodeId in navigation.NavigableNodes)
                    {
                        if (state.IsReachable(nodeId))
                        {
                            continue;
                        }

                        Assert.That(
                            navigation.FightsOnTheWayTo(nodeId),
                            Is.GreaterThan(0),
                            "Node " + nodeId + " is out of passage yet navigation walks in for free.");
                    }

                    continue;
                }

                whole++;

                Assert.That(
                    navigation.NavigableNodes,
                    Is.EqualTo(state.ReachableNodes),
                    "With nothing cut off, the two questions have to give the same answer.");
            }

            Console.WriteLine(
                "navigation against passage over " + states + " states: " + cut
                + " cut by an unbeaten enemy, " + whole + " whole");

            Assert.That(cut, Is.GreaterThan(200), "The sweep never met a level an enemy had cut in two.");
            Assert.That(whole, Is.GreaterThan(0), "The sweep never met a level with nothing cut off.");
        }

        [Test]
        public void ANavigationRouteToAReachableNodeIsTheRoutePassageAlreadyGave()
        {
            var routes = 0;

            foreach (var state in Corpus())
            {
                var navigation = NavigationMap.Of(state);

                foreach (var nodeId in state.ReachableNodes)
                {
                    Assert.That(
                        navigation.RouteTo(nodeId),
                        Is.EqualTo(state.RouteTo(nodeId)),
                        "Navigation walks node " + nodeId + " a different way than passage does, so "
                        + "every tap that worked before now works differently.");

                    routes++;
                }
            }

            Console.WriteLine("navigation routes matched against passage routes: " + routes);

            Assert.That(routes, Is.GreaterThan(1000));
        }

        [Test]
        public void NoNavigationRouteAnywhereInTheSeedCorpusEverCrossesAWall()
        {
            foreach (var state in Corpus())
            {
                var graph = state.Level;
                var navigation = NavigationMap.Of(state);

                foreach (var nodeId in navigation.NavigableNodes)
                {
                    TileRouteTests.AssertNoWallIsCrossed(
                        graph, TileRoute.Of(graph, navigation.RouteTo(nodeId)));
                }
            }
        }

        [Test]
        public void ANavigationRouteCrossesNoMoreUnbeatenEnemiesThanItCountsAsFights()
        {
            foreach (var state in Corpus())
            {
                var navigation = NavigationMap.Of(state);

                foreach (var nodeId in navigation.NavigableNodes)
                {
                    var route = navigation.RouteTo(nodeId);
                    var fights = 0;

                    for (var step = 1; step < route.Count - 1; step++)
                    {
                        if (state.BlocksPassage(route[step]))
                        {
                            fights++;
                        }
                    }

                    Assert.That(
                        fights,
                        Is.EqualTo(navigation.FightsOnTheWayTo(nodeId)),
                        "The route to node " + nodeId + " crosses " + fights
                        + " unbeaten enemies but the map counted "
                        + navigation.FightsOnTheWayTo(nodeId) + ".");
                }
            }
        }

        [Test]
        public void NavigationTakesTheLineThatFightsLeastAndNotMerelyTheShortestOne()
        {
            var weighed = 0;

            foreach (var state in Corpus())
            {
                var navigation = NavigationMap.Of(state);
                var cheapest = Cheapest(state);

                foreach (var nodeId in navigation.NavigableNodes)
                {
                    Assert.That(
                        navigation.FightsOnTheWayTo(nodeId),
                        Is.EqualTo(cheapest[nodeId]),
                        "Node " + nodeId + " can be walked to through fewer fights than navigation found.");

                    weighed++;
                }
            }

            Console.WriteLine("navigation lines weighed against an independent search: " + weighed);

            Assert.That(weighed, Is.GreaterThan(1000));
        }

        static int Barring(RunState state)
        {
            var barring = 0;

            foreach (var node in state.Level.Decisions.Nodes)
            {
                if (node.Id != state.PositionNodeId && state.BlocksPassage(node.Id))
                {
                    barring++;
                }
            }

            return barring;
        }

        static int[] Cheapest(RunState state)
        {
            var decisions = state.Level.Decisions;
            var toll = new int[decisions.Nodes.Count];

            for (var nodeId = 0; nodeId < toll.Length; nodeId++)
            {
                toll[nodeId] = int.MaxValue;
            }

            toll[state.PositionNodeId] = 0;

            for (var pass = 0; pass < toll.Length; pass++)
            {
                foreach (var corridor in decisions.Corridors)
                {
                    Relax(state, toll, corridor.LowNodeId, corridor.HighNodeId);
                    Relax(state, toll, corridor.HighNodeId, corridor.LowNodeId);
                }
            }

            for (var nodeId = 0; nodeId < toll.Length; nodeId++)
            {
                if (toll[nodeId] == int.MaxValue)
                {
                    toll[nodeId] = 0;
                }
            }

            return toll;
        }

        static void Relax(RunState state, int[] toll, int from, int to)
        {
            if (toll[from] == int.MaxValue)
            {
                return;
            }

            var paid = toll[from]
                + (from != state.PositionNodeId && state.BlocksPassage(from) ? 1 : 0);

            if (paid < toll[to])
            {
                toll[to] = paid;
            }
        }

        static IEnumerable<RunState> Corpus()
        {
            foreach (var preset in EveryPreset())
            {
                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var level = LevelGenerator.Generate(seed, preset);
                    var state = RunState.Begin(level.Graph, level.StartingPower);

                    for (var move = 0; move <= Moves; move++)
                    {
                        yield return state;

                        var taken = Step(state);
                        if (taken == null)
                        {
                            break;
                        }

                        state = taken;
                    }
                }
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
    }
}
