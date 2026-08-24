using System;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class RunStateTests
    {
        [Test]
        public void ARunBeginsStandingOnTheStartNodeHoldingNothingConsumed()
        {
            var state = RunFixture.Begin(startingPower: 2);

            Assert.That(state.PositionNodeId, Is.EqualTo(RunFixture.Start));
            Assert.That(state.Power, Is.EqualTo(2));
            Assert.That(state.ConsumedNodes, Is.Empty);
            Assert.That(state.IsLevelComplete, Is.False);
        }

        [Test]
        public void ARunCannotBeginOnALevelThatHasNoStart()
        {
            var builder = new LevelGraphBuilder(seed: 1, preset: "tiny");
            builder.AddTile(new TilePosition(0, 1, 0), regionId: 0);
            builder.AddTile(new TilePosition(0, 2, 0), regionId: 0);
            builder.AddTile(new TilePosition(0, 3, 0), regionId: 0);
            builder.AddNode(new TilePosition(0, 1, 0), NodeType.Empty);
            builder.AddNode(new TilePosition(0, 3, 0), NodeType.Boss, value: 4);
            builder.Connect(
                new TilePosition(0, 1, 0),
                new TilePosition(0, 3, 0),
                new[] { new TilePosition(0, 2, 0) });

            Assert.That(
                () => RunState.Begin(builder.Build(), startingPower: 1),
                Throws.ArgumentException.With.Message.Contains("exactly one Start"));
        }

        [Test]
        public void ARunCannotBeginWithoutPower()
        {
            Assert.That(
                () => RunState.Begin(RunFixture.Level(), startingPower: 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void AnUnconsumedEnemyIsReachableButWhatStandsBehindItIsNot()
        {
            var state = RunFixture.Begin(startingPower: 2);

            Assert.That(state.IsReachable(RunFixture.GateEnemy), Is.True);
            Assert.That(state.IsReachable(RunFixture.Boss), Is.False);
            Assert.That(
                state.ReachableNodes,
                Is.EqualTo(new[]
                {
                    RunFixture.Additive,
                    RunFixture.GateEnemy,
                    RunFixture.Multiplier,
                    RunFixture.Start,
                    RunFixture.DoorstepEnemy,
                    RunFixture.AdditiveBeyondTheMultiplier
                }));
        }

        [Test]
        public void ReachabilityGrowsWhenTheEnemyInFrontOfItFalls()
        {
            var opened = ActionResolver
                .Resolve(RunFixture.Begin(startingPower: 2), RunFixture.GateEnemy)
                .State;

            Assert.That(opened.IsReachable(RunFixture.Boss), Is.True);
        }

        [Test]
        public void AConsumedNodeIsInertButStillWalkedOver()
        {
            var afterTheGate = ActionResolver
                .Resolve(RunFixture.Begin(startingPower: 2), RunFixture.GateEnemy)
                .State;

            var backHome = ActionResolver.Resolve(afterTheGate, RunFixture.Multiplier);

            Assert.That(
                backHome.Route,
                Is.EqualTo(new[]
                {
                    RunFixture.GateEnemy,
                    RunFixture.Additive,
                    RunFixture.Start,
                    RunFixture.Multiplier
                }));
            Assert.That(
                backHome.State.ConsumedNodes,
                Is.EqualTo(new[] { RunFixture.Additive, RunFixture.GateEnemy, RunFixture.Multiplier }));
        }

        [Test]
        public void TheRouteToAnUnreachableNodeDoesNotExist()
        {
            var state = RunFixture.Begin(startingPower: 2);

            Assert.That(state.RouteTo(RunFixture.Boss), Is.Null);
            Assert.That(state.RouteTo(RunFixture.GateEnemy), Is.EqualTo(new[]
            {
                RunFixture.Start,
                RunFixture.Additive,
                RunFixture.GateEnemy
            }));
        }

        [Test]
        public void TheRouteToThePlayersOwnNodeIsThatNodeAlone()
        {
            var state = RunFixture.Begin(startingPower: 2);

            Assert.That(state.RouteTo(RunFixture.Start), Is.EqualTo(new[] { RunFixture.Start }));
        }

        [Test]
        public void ANodeIdNoNodeCarriesIsRefused()
        {
            var state = RunFixture.Begin(startingPower: 2);

            Assert.That(
                () => state.IsReachable(99),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
