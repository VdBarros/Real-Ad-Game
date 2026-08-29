using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class JourneyTests
    {
        const float Frame = 1f / 60f;

        static Journey Walked(Journey journey, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                journey = journey.Advanced(Frame);
            }

            return journey;
        }

        static Journey Reached(Journey journey)
        {
            for (var frame = 0; frame < 2000 && !journey.IsWaiting && !journey.IsOver; frame++)
            {
                journey = journey.Advanced(Frame);
            }

            return journey;
        }

        static Journey UntilAFight(Journey journey)
        {
            for (var frame = 0; frame < 4000 && !journey.IsOver && !journey.HoldsForAFight; frame++)
            {
                journey = journey.Advanced(Frame);

                if (journey.IsWaiting && !journey.HoldsForAFight)
                {
                    journey = journey.Resumed();
                }
            }

            return journey;
        }

        static Journey Landed(Journey journey, List<int> landings)
        {
            for (var frame = 0; frame < 4000 && !journey.IsOver; frame++)
            {
                journey = journey.Advanced(Frame);

                if (!journey.IsWaiting)
                {
                    continue;
                }

                var arrived = journey.Walk.ArrivedNodeId;
                if (landings.Count == 0 || landings[landings.Count - 1] != arrived)
                {
                    landings.Add(arrived);
                }

                journey = journey.Resumed();
            }

            return journey;
        }

        static Journey Ran(Journey journey, List<int> arrivals)
        {
            for (var frame = 0; frame < 2000 && !journey.IsOver; frame++)
            {
                journey = journey.Advanced(Frame);

                if (!journey.IsWaiting)
                {
                    continue;
                }

                if (arrivals != null)
                {
                    arrivals.Add(journey.Walk.ArrivedNodeId);
                }

                journey = journey.Resumed();
            }

            return journey;
        }

        [Test]
        public void ATapThatResolvesToNothingStartsNoJourney()
        {
            var finished = Ran(Journey.Toward(RunFixture.Begin(40), RunFixture.Boss), null).State;

            Assert.That(finished.IsLevelComplete, Is.True);
            Assert.That(Journey.Toward(finished, RunFixture.Multiplier), Is.SameAs(Journey.Nowhere));
            Assert.That(Journey.Nowhere.IsOver, Is.True);
            Assert.That(Journey.Nowhere.Arrival, Is.Null);
        }

        [Test]
        public void ATapBehindAnUnbeatenEnemyWalksTheRouteAndFightsOnContact()
        {
            var opening = RunFixture.Begin(2);
            var setOut = Journey.Toward(opening, RunFixture.Boss);

            Assert.That(opening.IsReachable(RunFixture.Boss), Is.False);
            Assert.That(setOut, Is.Not.SameAs(Journey.Nowhere));
            Assert.That(
                setOut.Walk.Route.Nodes,
                Is.EqualTo(new[]
                {
                    RunFixture.Start, RunFixture.Additive, RunFixture.GateEnemy, RunFixture.Boss
                }),
                "The tap has to lay a route straight through the enemy standing in the way.");

            var fought = UntilAFight(setOut);

            Assert.That(fought.Walk.ArrivedNodeId, Is.EqualTo(RunFixture.GateEnemy));
            Assert.That(fought.HoldsForAFight, Is.True, "Contact with the enemy has to interrupt the walk.");
            Assert.That(fought.Fight.Outcome, Is.EqualTo(ActionOutcome.Win));
        }

        [Test]
        public void WinningTheFightOnTheWayCarriesTheWalkOnToWhatWasTappedFor()
        {
            var arrivals = new List<int>();
            var journey = Landed(Journey.Toward(RunFixture.Begin(40), RunFixture.Boss), arrivals);

            Assert.That(
                arrivals,
                Is.EqualTo(new[] { RunFixture.Additive, RunFixture.GateEnemy, RunFixture.Boss }));
            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.Boss));
            Assert.That(journey.State.IsLevelComplete, Is.True);
        }

        [Test]
        public void LosingTheFightOnTheWayBouncesBackAndNeverReachesTheFarSide()
        {
            var opening = RunFixture.Begin(1);
            var journey = Ran(Journey.Toward(opening, RunFixture.Boss), null);

            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.Additive));
            Assert.That(journey.State.IsConsumed(RunFixture.GateEnemy), Is.False);
            Assert.That(journey.State.IsConsumed(RunFixture.Boss), Is.False);
            Assert.That(journey.Walk.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 3, 0))));
        }

        [Test]
        public void AJourneyStartsWhereTheRunStandsAndChangesNothingYet()
        {
            var state = RunFixture.Begin(2);
            var journey = Journey.Toward(state, RunFixture.Multiplier);

            Assert.That(journey.State, Is.SameAs(state));
            Assert.That(journey.IsOver, Is.False);
            Assert.That(journey.Arrival, Is.Null);
        }

        [Test]
        public void AMultiHopJourneyResolvesEveryNodeOnTheRouteInOrder()
        {
            var arrivals = new List<int>();
            var journey = Ran(
                Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier), arrivals);

            Assert.That(
                arrivals,
                Is.EqualTo(new[] { RunFixture.Multiplier, RunFixture.AdditiveBeyondTheMultiplier }));

            Assert.That(
                journey.State.Power,
                Is.EqualTo(2 * RunFixture.MultiplierValue + RunFixture.AdditiveBeyondTheMultiplierValue));
            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.AdditiveBeyondTheMultiplier));
        }

        [Test]
        public void PowerMovesOneNodeAtATimeRatherThanAllAtTheEnd()
        {
            var journey = Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier);
            var powers = new List<int>();

            for (var frame = 0; frame < 2000 && !journey.IsOver; frame++)
            {
                journey = journey.Advanced(Frame);

                if (!journey.IsWaiting)
                {
                    continue;
                }

                powers.Add(journey.State.Power);
                journey = journey.Resumed();
            }

            Assert.That(powers, Is.EqualTo(new[] { 6, 10 }));
        }

        [Test]
        public void AMultiplierHoldsTheWalkForABeatAndAnAdditiveDoesNot()
        {
            var journey = Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier);
            var beats = new List<bool>();

            for (var frame = 0; frame < 2000 && !journey.IsOver; frame++)
            {
                journey = journey.Advanced(Frame);

                if (!journey.IsWaiting)
                {
                    continue;
                }

                beats.Add(journey.HoldsForABeat);
                journey = journey.Resumed();
            }

            Assert.That(beats, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void WalkingBackOverASpentMultiplierHoldsForNoBeat()
        {
            var journey = Ran(
                Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier), null);

            Assert.That(journey.State.IsConsumed(RunFixture.Multiplier), Is.True);

            journey = Journey.Toward(journey.State, RunFixture.Start);
            var beats = 0;

            for (var frame = 0; frame < 2000 && !journey.IsOver; frame++)
            {
                journey = journey.Advanced(Frame);

                if (!journey.IsWaiting)
                {
                    continue;
                }

                if (journey.HoldsForABeat)
                {
                    beats++;
                }

                journey = journey.Resumed();
            }

            Assert.That(beats, Is.EqualTo(0));
        }

        [Test]
        public void AWalkWaitingOnABeatGoesNoFurtherUntilItIsResumed()
        {
            var journey = Walked(
                Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier), 200);

            Assert.That(journey.IsWaiting, Is.True);
            Assert.That(journey.Walk.ArrivedNodeId, Is.EqualTo(RunFixture.Multiplier));

            var held = journey.State;
            journey = Walked(journey, 200);

            Assert.That(journey.State, Is.SameAs(held));
            Assert.That(journey.Resumed().IsOver, Is.False);
        }

        [Test]
        public void CancellingMidWalkLeavesTheRunStateUntouchedForTheRestOfTheWalk()
        {
            var journey = Walked(
                Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier), 5);

            Assert.That(journey.IsWaiting, Is.False);

            var untouched = journey.State;
            journey = journey.Cancelled();

            Assert.That(journey.Walk.IsRetreating, Is.True);

            journey = Ran(journey, null);

            Assert.That(journey.IsOver, Is.True);
            Assert.That(journey.State, Is.SameAs(untouched));
            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.Start));
            Assert.That(journey.State.ConsumedNodes.Count, Is.EqualTo(0));
        }

        [Test]
        public void CancellingKeepsWhatTheWalkAlreadyReachedAndAbandonsTheRest()
        {
            var journey = Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier);
            journey = Walked(journey, 200).Resumed();

            Assert.That(journey.State.Power, Is.EqualTo(6));

            journey = Ran(journey.Advanced(Frame).Cancelled(), null);

            Assert.That(journey.State.Power, Is.EqualTo(6));
            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.Multiplier));
        }

        [Test]
        public void CancellingOnTheBeatItselfEndsTheWalkAtTheNodeItReached()
        {
            var journey = Walked(
                Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier), 200);

            Assert.That(journey.IsWaiting, Is.True);

            journey = Ran(journey.Cancelled(), null);

            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.Multiplier));
            Assert.That(journey.State.Power, Is.EqualTo(6));
        }

        [Test]
        public void ALostFightLeavesTheRunStateByteIdenticalAndWalksBack()
        {
            var opening = RunFixture.Begin(RunFixture.DoorstepEnemyValue - 1);
            var journey = Ran(Journey.Toward(opening, RunFixture.DoorstepEnemy), null);

            Assert.That(journey.State, Is.EqualTo(opening));
            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.Start));
            Assert.That(journey.Walk.Position, Is.EqualTo(IsoProjection.Of(new TilePosition(0, 3, 2))));
        }

        [Test]
        public void ATiedFightLeavesTheRunStateByteIdenticalAndWalksBack()
        {
            var opening = RunFixture.Begin(RunFixture.DoorstepEnemyValue);
            var journey = Ran(Journey.Toward(opening, RunFixture.DoorstepEnemy), null);

            Assert.That(journey.State, Is.EqualTo(opening));
            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.Start));
        }

        [Test]
        public void AWonFightAddsWhatTheEnemyWasWorthAndStandsWhereItFell()
        {
            var journey = Ran(Journey.Toward(RunFixture.Begin(3), RunFixture.DoorstepEnemy), null);

            Assert.That(journey.State.Power, Is.EqualTo(3 + RunFixture.DoorstepEnemyValue));
            Assert.That(journey.State.PositionNodeId, Is.EqualTo(RunFixture.DoorstepEnemy));
        }

        [Test]
        public void TheFightIsReadableAtTheMomentTheWalkArrivesOnIt()
        {
            var journey = Walked(Journey.Toward(RunFixture.Begin(3), RunFixture.DoorstepEnemy), 200);

            Assert.That(journey.IsWaiting, Is.True);
            Assert.That(journey.Arrival, Is.Not.Null);
            Assert.That(journey.Arrival.Outcome, Is.EqualTo(ActionOutcome.Win));
        }

        [Test]
        public void ArrivingOnAnEnemyJoinsAFightTheWalkWaitsOut()
        {
            var journey = Reached(Journey.Toward(RunFixture.Begin(3), RunFixture.DoorstepEnemy));

            Assert.That(journey.IsWaiting, Is.True);
            Assert.That(journey.Fight.IsJoined, Is.True);
            Assert.That(journey.Fight.Outcome, Is.EqualTo(ActionOutcome.Win));
            Assert.That(journey.HoldsForAFight, Is.True);
            Assert.That(journey.Resumed(), Is.SameAs(journey));
        }

        [Test]
        public void TheWalkMovesOnOnlyOnceTheFightHasPlayedOut()
        {
            var journey = Reached(Journey.Toward(RunFixture.Begin(3), RunFixture.DoorstepEnemy));
            var frames = (int)(journey.Fight.Seconds / Frame) + 1;

            journey = Walked(journey, frames);

            Assert.That(journey.Fight.IsSettled, Is.True);
            Assert.That(journey.HoldsForAFight, Is.False);
            Assert.That(journey.Resumed(), Is.Not.SameAs(journey));
        }

        [Test]
        public void AnArrivalOnAnythingButAnEnemyJoinsNoFight()
        {
            var journey = Reached(
                Journey.Toward(RunFixture.Begin(2), RunFixture.AdditiveBeyondTheMultiplier));

            Assert.That(journey.Walk.ArrivedNodeId, Is.EqualTo(RunFixture.Multiplier));
            Assert.That(journey.Fight.IsJoined, Is.False);
            Assert.That(journey.HoldsForAFight, Is.False);
        }

        [Test]
        public void WalkingBackOverAFallenEnemyJoinsNoSecondFight()
        {
            var journey = Ran(Journey.Toward(RunFixture.Begin(3), RunFixture.DoorstepEnemy), null);

            Assert.That(journey.State.IsConsumed(RunFixture.DoorstepEnemy), Is.True);

            journey = Reached(Journey.Toward(journey.State, RunFixture.Start));

            Assert.That(journey.Walk.ArrivedNodeId, Is.EqualTo(RunFixture.Start));
            Assert.That(journey.Fight.IsJoined, Is.False);
        }

        [Test]
        public void LettingGoOfTheScreenDoesNotCallOffAFightAlreadyJoined()
        {
            var opening = RunFixture.Begin(RunFixture.DoorstepEnemyValue - 1);
            var journey = Reached(Journey.Toward(opening, RunFixture.DoorstepEnemy));

            Assert.That(journey.Fight.Outcome, Is.EqualTo(ActionOutcome.Loss));

            var cancelled = journey.Cancelled();

            Assert.That(cancelled.HoldsForAFight, Is.True);
            Assert.That(cancelled.Walk.IsRetreating, Is.False);

            var settled = Ran(cancelled, null);

            Assert.That(settled.State, Is.EqualTo(opening));
            Assert.That(settled.State.PositionNodeId, Is.EqualTo(RunFixture.Start));
        }

        [Test]
        public void ATieAndALossRunTheirFightsAndStillLeaveTheRunByteIdentical()
        {
            foreach (var power in new[] { RunFixture.DoorstepEnemyValue, RunFixture.DoorstepEnemyValue - 1 })
            {
                var opening = RunFixture.Begin(power);
                var fought = Ran(Journey.Toward(opening, RunFixture.DoorstepEnemy), null);

                Assert.That(fought.State, Is.EqualTo(opening));
                Assert.That(fought.State.Power, Is.EqualTo(power));
                Assert.That(fought.State.ConsumedNodes.Count, Is.EqualTo(0));
                Assert.That(fought.State.PositionNodeId, Is.EqualTo(RunFixture.Start));
            }
        }

        [Test]
        public void AWonFightAddsTheEnemysValueAndNothingElseTouchesPower()
        {
            var opening = RunFixture.Begin(3);
            var journey = Ran(Journey.Toward(opening, RunFixture.DoorstepEnemy), null);

            Assert.That(journey.State.Power - opening.Power, Is.EqualTo(RunFixture.DoorstepEnemyValue));
            Assert.That(journey.State.ConsumedNodes, Is.EqualTo(new[] { RunFixture.DoorstepEnemy }));
        }

        [Test]
        public void AJourneyNeedsARunToSetOutFrom()
        {
            Assert.That(() => Journey.Toward(null, 0), Throws.ArgumentNullException);
        }
    }
}
