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
            Assert.That(Journey.Toward(RunFixture.Begin(2), RunFixture.Boss), Is.SameAs(Journey.Nowhere));
            Assert.That(Journey.Nowhere.IsOver, Is.True);
            Assert.That(Journey.Nowhere.Arrival, Is.Null);
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
        public void AJourneyNeedsARunToSetOutFrom()
        {
            Assert.That(() => Journey.Toward(null, 0), Throws.ArgumentNullException);
        }
    }
}
