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
        public void AWonFightHoldsTheControlsForTheClashAndTheDissolveAndNoLonger()
        {
            var win = Fight.Of(ActionOutcome.Win);

            Assert.That(win.Seconds, Is.EqualTo(VictoryStages.BlockingSeconds).Within(1e-4f));
            Assert.That(win.Seconds, Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(win.Stage, Is.EqualTo(VictoryStage.Clash));
            Assert.That(win.Advanced(VictoryStages.ClashSeconds).Stage, Is.EqualTo(VictoryStage.Dissolve));
            Assert.That(win.Advanced(win.Seconds).Stage, Is.EqualTo(VictoryStage.Done));
            Assert.That(win.Advanced(win.Seconds - 0.001f).IsSettled, Is.False);
            Assert.That(win.Advanced(win.Seconds).IsSettled, Is.True);
            Assert.That(win.Timeline.HasBegun, Is.True);
            Assert.That(Fight.Of(ActionOutcome.Tie).Timeline.HasBegun, Is.False);
            Assert.That(Fight.Of(ActionOutcome.Loss).Timeline.HasBegun, Is.False);
        }

        [Test]
        public void AWonClashIsAnExchangeOfBlowsThrownByBothFightersInTurn()
        {
            var thrown = new List<bool>();
            var lit = new List<float>();
            var fight = Fight.Of(ActionOutcome.Win);

            for (var blow = 0; blow < Fight.Blows; blow++)
            {
                var landing = fight.Advanced(Fight.BlowOpensAt(blow) + Fight.BlowSeconds * 0.5f);

                Assert.That(landing.IsTrading, Is.True, "blow " + blow);
                Assert.That(landing.Spark.IsLit, Is.True, "blow " + blow);
                Assert.That(landing.ThePlayerThrewIt, Is.EqualTo(Fight.BlowIsThePlayers(blow)), "blow " + blow);

                thrown.Add(landing.ThePlayerThrewIt);
                lit.Add(landing.Spark.Sway);
            }

            Assert.That(Fight.Blows, Is.GreaterThanOrEqualTo(3));
            Assert.That(thrown, Does.Contain(true));
            Assert.That(thrown, Does.Contain(false));

            for (var blow = 1; blow < thrown.Count; blow++)
            {
                Assert.That(thrown[blow], Is.Not.EqualTo(thrown[blow - 1]), "blow " + blow);
                Assert.That(lit[blow] * lit[blow - 1], Is.LessThan(0f), "blow " + blow);
            }
        }

        [Test]
        public void TheBlowsFillTheClashAndNoneOfThemOutlivesIt()
        {
            var filled = 0f;

            for (var blow = 0; blow < Fight.Blows; blow++)
            {
                Assert.That(Fight.BlowOpensAt(blow), Is.EqualTo(filled).Within(1e-4f), "blow " + blow);
                Assert.That(Fight.BlowSecondsOf(blow), Is.GreaterThan(Fight.BlowSeconds), "blow " + blow);

                filled += Fight.BlowSecondsOf(blow);
            }

            Assert.That(filled, Is.EqualTo(VictoryStages.ClashSeconds).Within(1e-4f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fight.BlowOpensAt(Fight.Blows));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fight.BlowSecondsOf(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Fight.BlowIsThePlayers(Fight.Blows));
        }

        [Test]
        public void EveryBlowOfTheClashThrowsTheStruckFighterAndBothStandAgainByTheNextOne()
        {
            var fight = Fight.Of(ActionOutcome.Win);

            for (var blow = 0; blow < Fight.Blows; blow++)
            {
                var landing = fight.Advanced(Fight.BlowOpensAt(blow) + Fight.BlowSeconds * 0.5f);
                var thrower = Fight.BlowIsThePlayers(blow);
                var struck = thrower ? landing.Recoil : landing.Shove;
                var lunging = thrower ? landing.Shove : landing.Recoil;

                Assert.That(Math.Abs(struck), Is.GreaterThan(0.4f), "blow " + blow);
                Assert.That(Math.Abs(struck), Is.GreaterThan(Math.Abs(lunging) * 2f), "blow " + blow);
                Assert.That(landing.Recoil - landing.Shove, Is.GreaterThan(0.4f), "blow " + blow);

                var over = fight.Advanced(
                    Fight.BlowOpensAt(blow) + Fight.BlowSecondsOf(blow) - 1e-5f);

                Assert.That(Math.Abs(over.Shove), Is.LessThan(0.01f), "blow " + blow);
                Assert.That(Math.Abs(over.Recoil), Is.LessThan(0.01f), "blow " + blow);
            }

            Assert.That(Peak(Reel(ActionOutcome.Win), frame => frame.Shove), Is.GreaterThan(0.4f));
            Assert.That(Settled(ActionOutcome.Win).Shove, Is.EqualTo(0f));
        }

        [Test]
        public void TheEnemyStaysSolidThroughTheClashAndOnlyFadesOnceTheDissolveOpens()
        {
            var fight = Fight.Of(ActionOutcome.Win);

            for (var at = 0f; at < VictoryStages.ClashSeconds; at += Frame)
            {
                Assert.That(fight.Advanced(at).Fade, Is.EqualTo(1f), at.ToString());
            }

            var opening = fight.Advanced(VictoryStages.ClashSeconds);
            var closing = fight.Advanced(VictoryStages.BlockingSeconds - Frame);

            Assert.That(opening.Fade, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(closing.Fade, Is.LessThan(0.1f));
            Assert.That(closing.Fade, Is.GreaterThan(0f));
            Assert.That(Settled(ActionOutcome.Win).Fade, Is.EqualTo(0f));

            var faded = 1f;
            for (var at = VictoryStages.ClashSeconds; at <= VictoryStages.BlockingSeconds; at += Frame)
            {
                var fade = fight.Advanced(at).Fade;

                Assert.That(fade, Is.LessThanOrEqualTo(faded), at.ToString());
                faded = fade;
            }
        }

        [Test]
        public void TheClashAndTheDissolveAreTheOnlyThingsHoldingTheControls()
        {
            var fight = Fight.Of(ActionOutcome.Win);

            for (var at = 0f; at < VictoryStages.BlockingSeconds; at += Frame)
            {
                var held = fight.Advanced(at);

                Assert.That(held.IsSettled, Is.False, at.ToString());
                Assert.That(held.Timeline.BlocksInput, Is.True, at.ToString());
            }

            Assert.That(fight.Advanced(VictoryStages.BlockingSeconds).Timeline.BlocksInput, Is.False);
        }

        [Test]
        public void ARunOfConsecutiveWonFightsHoldsTheControlsForTheSameSpanEveryTime()
        {
            var clock = 0d;
            var carried = 0f;
            var shortest = float.MaxValue;
            var longest = 0f;
            var runs = 300;

            for (var run = 0; run < runs; run++)
            {
                var contact = clock - carried;
                var fight = Fight.Of(ActionOutcome.Win).Advanced(carried);
                var delta = Frame * (1f + 0.25f * (run % 5 - 2));

                for (var frame = 0; frame < 600 && !fight.IsSettled; frame++)
                {
                    fight = fight.Advanced(delta);
                    clock += delta;
                }

                Assert.That(fight.IsSettled, Is.True, "run " + run);

                var overrun = fight.Timeline.Overrun;
                var span = (float)(clock - overrun - contact);

                shortest = span < shortest ? span : shortest;
                longest = span > longest ? span : longest;
                carried = overrun;
            }

            Assert.That(shortest, Is.EqualTo(Fight.Of(ActionOutcome.Win).Seconds).Within(0.001f));
            Assert.That(longest - shortest, Is.LessThan(0.001f));
        }

        [Test]
        public void BreakingOffAFightSettlesItWhereItStandsAndHandsTheControlsBack()
        {
            foreach (var outcome in Fights)
            {
                var broken = Fight.Of(outcome).Advanced(0.05f).Broken();

                Assert.That(broken.IsSettled, Is.True, outcome.ToString());
                Assert.That(broken.Spark.IsLit, Is.False, outcome.ToString());
                Assert.That(broken.Shove, Is.EqualTo(0f), outcome.ToString());
                Assert.That(broken.Recoil, Is.EqualTo(0f), outcome.ToString());
                Assert.That(broken.Broken(), Is.EqualTo(broken), outcome.ToString());
            }

            Assert.That(Fight.Of(ActionOutcome.Win).Broken().Fade, Is.EqualTo(0f));
            Assert.That(Fight.None.Broken(), Is.EqualTo(Fight.None));
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
