using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FightTests
    {
        const float Frame = 1f / 60f;

        static readonly ActionOutcome[] Fights =
        {
            ActionOutcome.Win, ActionOutcome.Tie, ActionOutcome.Loss
        };

        static List<Fight> Reel(ActionOutcome outcome)
        {
            var frames = new List<Fight>();
            var fight = Fight.Of(outcome);

            while (!fight.IsSettled)
            {
                frames.Add(fight);
                fight = fight.Advanced(Frame);
            }

            frames.Add(fight);
            return frames;
        }

        static Fight Settled(ActionOutcome outcome)
        {
            var reel = Reel(outcome);
            return reel[reel.Count - 1];
        }

        static float Peak(List<Fight> reel, Func<Fight, float> channel)
        {
            var peak = 0f;
            foreach (var frame in reel)
            {
                var reading = Math.Abs(channel(frame));
                if (reading > peak)
                {
                    peak = reading;
                }
            }

            return peak;
        }

        static float Apart(Tint first, Tint second)
        {
            return Math.Abs(first.Red - second.Red)
                + Math.Abs(first.Green - second.Green)
                + Math.Abs(first.Blue - second.Blue);
        }

        [Test]
        public void AnArrivalThatIsNotAnEncounterJoinsNoFight()
        {
            Assert.That(Fight.Of(ActionOutcome.Walked), Is.EqualTo(Fight.None));
            Assert.That(Fight.Of(ActionOutcome.Rejected), Is.EqualTo(Fight.None));
            Assert.That(Fight.None.IsJoined, Is.False);
            Assert.That(Fight.None.IsSettled, Is.True);
            Assert.That(Fight.None.Seconds, Is.EqualTo(0f));
        }

        [Test]
        public void EveryEncounterJoinsAFightThatHasToBePlayedOut()
        {
            foreach (var outcome in Fights)
            {
                var fight = Fight.Of(outcome);

                Assert.That(fight.IsJoined, Is.True, outcome.ToString());
                Assert.That(fight.Outcome, Is.EqualTo(outcome));
                Assert.That(fight.IsSettled, Is.False, outcome.ToString());
                Assert.That(fight.Seconds, Is.GreaterThan(0f), outcome.ToString());
            }
        }

        [Test]
        public void AFightSettlesWhenItsReelRunsOutAndNotBefore()
        {
            foreach (var outcome in Fights)
            {
                var fight = Fight.Of(outcome);

                Assert.That(fight.Advanced(fight.Seconds - 0.001f).IsSettled, Is.False, outcome.ToString());
                Assert.That(fight.Advanced(fight.Seconds).IsSettled, Is.True, outcome.ToString());
            }
        }

        [Test]
        public void ASettledFightLeavesBothFightersStandingWhereTheyStood()
        {
            foreach (var outcome in Fights)
            {
                var settled = Settled(outcome);

                Assert.That(settled.Shove, Is.EqualTo(0f), outcome.ToString());
                Assert.That(settled.Recoil, Is.EqualTo(0f), outcome.ToString());
                Assert.That(settled.Spark.IsLit, Is.False, outcome.ToString());
            }
        }

        [Test]
        public void OnlyAWinTakesTheEnemyOffTheBoard()
        {
            Assert.That(Fight.Of(ActionOutcome.Win).Dissolves, Is.True);
            Assert.That(Fight.Of(ActionOutcome.Tie).Dissolves, Is.False);
            Assert.That(Fight.Of(ActionOutcome.Loss).Dissolves, Is.False);
            Assert.That(Fight.None.Dissolves, Is.False);
            Assert.That(Settled(ActionOutcome.Win).Fade, Is.EqualTo(0f));

            foreach (var frame in Reel(ActionOutcome.Tie))
            {
                Assert.That(frame.Fade, Is.EqualTo(1f));
            }

            foreach (var frame in Reel(ActionOutcome.Loss))
            {
                Assert.That(frame.Fade, Is.EqualTo(1f));
            }
        }

        [Test]
        public void TheBlowLandsWithDaylightBetweenTheFighters()
        {
            foreach (var outcome in Fights)
            {
                var struck = Fight.Of(outcome).Advanced(Fight.BlowSeconds * 0.5f);

                Assert.That(struck.Spark.IsLit, Is.True, outcome.ToString());
                Assert.That(struck.Recoil - struck.Shove, Is.GreaterThan(0.4f), outcome.ToString());
            }
        }

        [Test]
        public void AWinStepsBackFromTheBlowAndThenTakesTheTile()
        {
            var win = Reel(ActionOutcome.Win);

            Assert.That(Peak(win, frame => frame.Shove), Is.GreaterThan(0.4f));
            Assert.That(Settled(ActionOutcome.Win).Shove, Is.EqualTo(0f));
        }

        [Test]
        public void ATieShovesBothFightersAndALossOnlyEverShovesThePlayer()
        {
            var tie = Reel(ActionOutcome.Tie);
            var loss = Reel(ActionOutcome.Loss);

            Assert.That(Peak(tie, frame => frame.Recoil), Is.GreaterThan(0f));
            Assert.That(Peak(loss, frame => frame.Recoil), Is.EqualTo(0f));
            Assert.That(Peak(tie, frame => frame.Shove), Is.GreaterThan(0f));
            Assert.That(Peak(loss, frame => frame.Shove), Is.GreaterThan(0f));
        }

        [Test]
        public void ALossThrowsThePlayerFurtherAndForLongerThanATieDoes()
        {
            var tie = Reel(ActionOutcome.Tie);
            var loss = Reel(ActionOutcome.Loss);

            Assert.That(
                Peak(loss, frame => frame.Shove),
                Is.GreaterThan(Peak(tie, frame => frame.Shove) * 3f));

            Assert.That(
                Fight.Of(ActionOutcome.Loss).Seconds,
                Is.GreaterThan(Fight.Of(ActionOutcome.Tie).Seconds));
        }

        [Test]
        public void ATieAndALossReadDifferentlyForMostOfTheirLength()
        {
            var tie = Fight.Of(ActionOutcome.Tie);
            var loss = Fight.Of(ActionOutcome.Loss);

            Assert.That(tie.Spark.Sway, Is.Not.EqualTo(loss.Spark.Sway));
            Assert.That(Apart(tie.Spark.Tint, loss.Spark.Tint), Is.GreaterThan(0.5f));

            var told = 0;
            var frames = 0;

            while (!tie.IsSettled || !loss.IsSettled)
            {
                if (!tie.Shove.Equals(loss.Shove)
                    || !tie.Recoil.Equals(loss.Recoil)
                    || tie.Spark.IsLit != loss.Spark.IsLit)
                {
                    told++;
                }

                frames++;
                tie = tie.Advanced(Frame);
                loss = loss.Advanced(Frame);
            }

            Assert.That(told, Is.GreaterThan(frames / 2));
        }

        [Test]
        public void TheSparkMarksWhoTookTheBlow()
        {
            Assert.That(Fight.Of(ActionOutcome.Win).Spark.Sway, Is.GreaterThan(0f));
            Assert.That(Fight.Of(ActionOutcome.Tie).Spark.Sway, Is.EqualTo(0f));
            Assert.That(Fight.Of(ActionOutcome.Loss).Spark.Sway, Is.LessThan(0f));

            foreach (var outcome in Fights)
            {
                foreach (var other in Fights)
                {
                    if (outcome == other)
                    {
                        continue;
                    }

                    Assert.That(
                        Apart(Fight.Of(outcome).Spark.Tint, Fight.Of(other).Spark.Tint),
                        Is.GreaterThan(0.3f),
                        outcome + " against " + other);
                }
            }
        }

        [Test]
        public void ASparkIsLitForTheBlowAndForNothingElse()
        {
            Assert.That(Fight.None.Spark.IsLit, Is.False);

            foreach (var outcome in Fights)
            {
                var fight = Fight.Of(outcome);

                Assert.That(fight.Spark.IsLit, Is.False, outcome.ToString());
                Assert.That(fight.Advanced(Fight.BlowSeconds * 0.5f).Spark.IsLit, Is.True, outcome.ToString());
                Assert.That(fight.Advanced(Fight.BlowSeconds).Spark.IsLit, Is.False, outcome.ToString());
            }
        }

        [Test]
        public void AFightOnlyEverRunsForwards()
        {
            Assert.That(
                () => Fight.Of(ActionOutcome.Win).Advanced(-Frame),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void AdvancingASettledFightChangesNothing()
        {
            foreach (var outcome in Fights)
            {
                var settled = Fight.Of(outcome).Advanced(10f);

                Assert.That(settled.Advanced(10f), Is.EqualTo(settled), outcome.ToString());
            }

            Assert.That(Fight.None.Advanced(10f), Is.EqualTo(Fight.None));
        }

        [Test]
        public void TwoFightsAreTheSameFightWhenTheyShowTheSameThing()
        {
            var fight = Fight.Of(ActionOutcome.Tie).Advanced(Frame);

            Assert.That(fight, Is.EqualTo(Fight.Of(ActionOutcome.Tie).Advanced(Frame)));
            Assert.That(
                fight.GetHashCode(),
                Is.EqualTo(Fight.Of(ActionOutcome.Tie).Advanced(Frame).GetHashCode()));
            Assert.That(fight, Is.Not.EqualTo(Fight.Of(ActionOutcome.Loss).Advanced(Frame)));
        }
    }
}
