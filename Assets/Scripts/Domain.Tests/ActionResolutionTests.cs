using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ActionResolutionTests
    {
        [Test]
        public void AnEnemyWeakerThanThePlayerFallsAndHandsOverItsPower()
        {
            var before = RunFixture.Begin(startingPower: 3);

            var result = ActionResolver.Resolve(before, RunFixture.DoorstepEnemy);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(result.State.Power, Is.EqualTo(3 + RunFixture.DoorstepEnemyValue));
            Assert.That(result.State.PositionNodeId, Is.EqualTo(RunFixture.DoorstepEnemy));
            Assert.That(result.State.ConsumedNodes, Is.EqualTo(new[] { RunFixture.DoorstepEnemy }));
        }

        [Test]
        public void AnEnemyEqualToThePlayerTiesAndChangesNothing()
        {
            var before = RunFixture.Begin(startingPower: RunFixture.DoorstepEnemyValue);

            var result = ActionResolver.Resolve(before, RunFixture.DoorstepEnemy);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Tie));
            Assert.That(result.State, Is.EqualTo(before));
        }

        [Test]
        public void AnEnemyStrongerThanThePlayerTurnsItBackAndChangesNothing()
        {
            var before = RunFixture.Begin(startingPower: RunFixture.DoorstepEnemyValue - 1);

            var result = ActionResolver.Resolve(before, RunFixture.DoorstepEnemy);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Loss));
            Assert.That(result.State, Is.EqualTo(before));
        }

        [Test]
        public void AnAdditiveAddsItsValueAndIsConsumed()
        {
            var result = ActionResolver.Resolve(RunFixture.Begin(startingPower: 2), RunFixture.Additive);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Walked));
            Assert.That(result.State.Power, Is.EqualTo(2 + RunFixture.AdditiveValue));
            Assert.That(result.State.IsConsumed(RunFixture.Additive), Is.True);
        }

        [Test]
        public void AMultiplierMultipliesPowerAndIsConsumed()
        {
            var result = ActionResolver.Resolve(RunFixture.Begin(startingPower: 2), RunFixture.Multiplier);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Walked));
            Assert.That(result.State.Power, Is.EqualTo(2 * RunFixture.MultiplierValue));
            Assert.That(result.State.IsConsumed(RunFixture.Multiplier), Is.True);
        }

        [Test]
        public void AMultiHopTapResolvesEveryNodeOnTheWayInRouteOrder()
        {
            var before = RunFixture.Begin(startingPower: 2);

            var result = ActionResolver.Resolve(before, RunFixture.GateEnemy);

            Assert.That(
                result.Route,
                Is.EqualTo(new[] { RunFixture.Start, RunFixture.Additive, RunFixture.GateEnemy }));
            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(
                result.State.Power,
                Is.EqualTo(2 + RunFixture.AdditiveValue + RunFixture.GateEnemyValue));
            Assert.That(
                result.State.ConsumedNodes,
                Is.EqualTo(new[] { RunFixture.Additive, RunFixture.GateEnemy }));
        }

        [Test]
        public void ATieAfterAMultiHopWalkLeavesThePlayerOnTheNodeBeforeTheEnemy()
        {
            var result = ActionResolver.Resolve(RunFixture.Begin(startingPower: 1), RunFixture.GateEnemy);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Tie));
            Assert.That(result.State.PositionNodeId, Is.EqualTo(RunFixture.Additive));
            Assert.That(result.State.Power, Is.EqualTo(RunFixture.GateEnemyValue));
            Assert.That(result.State.ConsumedNodes, Is.EqualTo(new[] { RunFixture.Additive }));
            Assert.That(result.State.IsConsumed(RunFixture.GateEnemy), Is.False);
        }

        [Test]
        public void OneRouteAppliesAMultiplierBeforeAnAdditiveItLeadsTo()
        {
            var result = ActionResolver.Resolve(
                RunFixture.Begin(startingPower: 2),
                RunFixture.AdditiveBeyondTheMultiplier);

            Assert.That(
                result.Route,
                Is.EqualTo(new[]
                {
                    RunFixture.Start,
                    RunFixture.Multiplier,
                    RunFixture.AdditiveBeyondTheMultiplier
                }));
            Assert.That(
                result.State.Power,
                Is.EqualTo(2 * RunFixture.MultiplierValue + RunFixture.AdditiveBeyondTheMultiplierValue));
            Assert.That(
                result.State.Power,
                Is.Not.EqualTo((2 + RunFixture.AdditiveBeyondTheMultiplierValue) * RunFixture.MultiplierValue));
        }

        [Test]
        public void TakingTheMultiplierFirstAndTheAdditiveFirstEndOnDifferentPower()
        {
            var multiplyThenAdd = RunFixture.Begin(startingPower: 2);
            multiplyThenAdd = ActionResolver.Resolve(multiplyThenAdd, RunFixture.Multiplier).State;
            multiplyThenAdd = ActionResolver.Resolve(multiplyThenAdd, RunFixture.Additive).State;

            var addThenMultiply = RunFixture.Begin(startingPower: 2);
            addThenMultiply = ActionResolver.Resolve(addThenMultiply, RunFixture.Additive).State;
            addThenMultiply = ActionResolver.Resolve(addThenMultiply, RunFixture.Multiplier).State;

            Assert.That(
                multiplyThenAdd.Power,
                Is.EqualTo(2 * RunFixture.MultiplierValue + RunFixture.AdditiveValue));
            Assert.That(
                addThenMultiply.Power,
                Is.EqualTo((2 + RunFixture.AdditiveValue) * RunFixture.MultiplierValue));
            Assert.That(addThenMultiply.Power, Is.Not.EqualTo(multiplyThenAdd.Power));
            Assert.That(
                addThenMultiply.ConsumedNodes,
                Is.EqualTo(multiplyThenAdd.ConsumedNodes),
                "Both orders spend the same two pickups, so only the order is left to explain the gap.");
        }

        [Test]
        public void ASpentPickupIsWalkedOverAndAppliedNoSecondTime()
        {
            var taken = ActionResolver
                .Resolve(RunFixture.Begin(startingPower: 2), RunFixture.Multiplier)
                .State;

            var again = ActionResolver.Resolve(taken, RunFixture.Multiplier);

            Assert.That(again.Outcome, Is.EqualTo(ActionOutcome.Walked));
            Assert.That(again.State.Power, Is.EqualTo(taken.Power));
            Assert.That(again.State, Is.EqualTo(taken));
        }

        [Test]
        public void AWalkPastASpentPickupCarriesItsValueNoFurther()
        {
            var state = RunFixture.Begin(startingPower: 2);
            state = ActionResolver.Resolve(state, RunFixture.Multiplier).State;
            state = ActionResolver.Resolve(state, RunFixture.Start).State;

            var result = ActionResolver.Resolve(state, RunFixture.AdditiveBeyondTheMultiplier);

            Assert.That(
                result.Route,
                Is.EqualTo(new[]
                {
                    RunFixture.Start,
                    RunFixture.Multiplier,
                    RunFixture.AdditiveBeyondTheMultiplier
                }));
            Assert.That(
                result.State.Power,
                Is.EqualTo(2 * RunFixture.MultiplierValue + RunFixture.AdditiveBeyondTheMultiplierValue));
        }

        [Test]
        public void AnUnreachableTargetIsRejectedAndChangesNothing()
        {
            var before = RunFixture.Begin(startingPower: 2);

            var result = ActionResolver.Resolve(before, RunFixture.Boss);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Rejected));
            Assert.That(result.Route, Is.Empty);
            Assert.That(result.State, Is.SameAs(before));
        }

        [Test]
        public void ANodeIdNoNodeCarriesIsRefused()
        {
            Assert.That(
                () => ActionResolver.Resolve(RunFixture.Begin(startingPower: 2), 99),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void BeatingTheBossEndsTheLevel()
        {
            var state = RunFixture.Begin(startingPower: 2);
            state = ActionResolver.Resolve(state, RunFixture.GateEnemy).State;
            state = ActionResolver.Resolve(state, RunFixture.Multiplier).State;

            var result = ActionResolver.Resolve(state, RunFixture.Boss);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(result.State.IsLevelComplete, Is.True);
            var poweredUp = (2 + RunFixture.AdditiveValue + RunFixture.GateEnemyValue) * RunFixture.MultiplierValue;
            Assert.That(result.State.Power, Is.EqualTo(poweredUp + RunFixture.BossValue));
        }

        [Test]
        public void LosingToTheBossIsAnOrdinaryFightAndEndsNothing()
        {
            var state = ActionResolver
                .Resolve(RunFixture.Begin(startingPower: 2), RunFixture.GateEnemy)
                .State;

            var result = ActionResolver.Resolve(state, RunFixture.Boss);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Loss));
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.State.IsLevelComplete, Is.False);
        }

        [Test]
        public void NothingResolvesOnceTheBossHasFallen()
        {
            var state = RunFixture.Begin(startingPower: 2);
            state = ActionResolver.Resolve(state, RunFixture.GateEnemy).State;
            state = ActionResolver.Resolve(state, RunFixture.Multiplier).State;
            state = ActionResolver.Resolve(state, RunFixture.Boss).State;

            var result = ActionResolver.Resolve(state, RunFixture.DoorstepEnemy);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Rejected));
            Assert.That(result.State, Is.SameAs(state));
        }

        [Test]
        public void TheSameTwoPickupsInOppositeOrdersYieldDifferentPower()
        {
            var additiveFirst = RunFixture.Begin(startingPower: 2);
            additiveFirst = ActionResolver.Resolve(additiveFirst, RunFixture.Additive).State;
            additiveFirst = ActionResolver.Resolve(additiveFirst, RunFixture.Multiplier).State;

            var multiplierFirst = RunFixture.Begin(startingPower: 2);
            multiplierFirst = ActionResolver.Resolve(multiplierFirst, RunFixture.Multiplier).State;
            multiplierFirst = ActionResolver.Resolve(multiplierFirst, RunFixture.Additive).State;

            Assert.That(
                additiveFirst.Power,
                Is.EqualTo((2 + RunFixture.AdditiveValue) * RunFixture.MultiplierValue));
            Assert.That(
                multiplierFirst.Power,
                Is.EqualTo(2 * RunFixture.MultiplierValue + RunFixture.AdditiveValue));
            Assert.That(additiveFirst.Power, Is.Not.EqualTo(multiplierFirst.Power));
        }

        [Test]
        public void ATargetAlreadyConsumedIsWalkedToAndResolvesNothing()
        {
            var state = ActionResolver
                .Resolve(RunFixture.Begin(startingPower: 2), RunFixture.Additive)
                .State;

            var result = ActionResolver.Resolve(state, RunFixture.Additive);

            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Walked));
            Assert.That(result.State.Power, Is.EqualTo(state.Power));
            Assert.That(result.State.ConsumedNodes, Is.EqualTo(state.ConsumedNodes));
        }

        [Test]
        public void AJunctionNodeIsWalkedOverAndNeverConsumed()
        {
            var level = LevelGraphFixture.TwoFloors();
            var junction = level.Decisions.NodeAt(new TilePosition(floor: 0, x: 5, y: 0));
            var state = RunState.Begin(level, startingPower: 5);

            var result = ActionResolver.Resolve(state, junction.Id);

            Assert.That(junction.Type, Is.EqualTo(NodeType.Empty));
            Assert.That(result.Outcome, Is.EqualTo(ActionOutcome.Walked));
            Assert.That(result.State.PositionNodeId, Is.EqualTo(junction.Id));
            Assert.That(result.State.Power, Is.EqualTo(5));
            Assert.That(result.State.ConsumedNodes, Is.Empty);
        }

        [Test]
        public void PowerNeverDecreasesAcrossAWholeRun()
        {
            var state = RunFixture.Begin(startingPower: 2);
            var powers = new List<int> { state.Power };
            var outcomes = new List<ActionOutcome>();

            foreach (var target in new[]
            {
                RunFixture.Boss,
                RunFixture.DoorstepEnemy,
                RunFixture.GateEnemy,
                RunFixture.Boss,
                RunFixture.DoorstepEnemy,
                RunFixture.Multiplier,
                RunFixture.Boss
            })
            {
                var result = ActionResolver.Resolve(state, target);
                state = result.State;
                outcomes.Add(result.Outcome);
                powers.Add(state.Power);
            }

            for (var step = 1; step < powers.Count; step++)
            {
                Assert.That(powers[step], Is.GreaterThanOrEqualTo(powers[step - 1]), "step " + step);
            }

            Assert.That(
                outcomes,
                Is.EqualTo(new[]
                {
                    ActionOutcome.Rejected,
                    ActionOutcome.Tie,
                    ActionOutcome.Win,
                    ActionOutcome.Loss,
                    ActionOutcome.Win,
                    ActionOutcome.Walked,
                    ActionOutcome.Win
                }));
            Assert.That(state.IsLevelComplete, Is.True);
        }
    }
}
