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

        const float Tolerance = 1e-4f;

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

        static Fight AtContact(ActionOutcome outcome)
        {
            var fight = Fight.Of(outcome);

            return fight.Advanced(fight.ContactAt + Fight.ThrowSeconds);
        }

        [Test]
        public void AnArrivalThatIsNotAnEncounterJoinsNoFight()
        {
            Assert.That(Fight.Of(ActionOutcome.Walked), Is.EqualTo(Fight.None));
            Assert.That(Fight.Of(ActionOutcome.Rejected), Is.EqualTo(Fight.None));
            Assert.That(Fight.None.IsJoined, Is.False);
            Assert.That(Fight.None.IsSettled, Is.True);
            Assert.That(Fight.None.Seconds, Is.EqualTo(0f));
            Assert.That(Fight.None.BlowBeat, Is.EqualTo(0f));
            Assert.That(Fight.None.FallBeat, Is.EqualTo(0f));
            Assert.That(Fight.None.Impact, Is.EqualTo(0f));
            Assert.That(Fight.None.HasStruck, Is.False);
            Assert.That(Fight.None.IsExecuting, Is.False);
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
                Assert.That(settled.Impact, Is.EqualTo(0f), outcome.ToString());
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
                var fight = Fight.Of(outcome);
                var widest = 0f;
                var widestAt = 0f;
                var at = 0f;

                foreach (var frame in Reel(outcome))
                {
                    var apart = frame.Recoil - frame.Shove;
                    if (apart > widest)
                    {
                        widest = apart;
                        widestAt = at;
                    }

                    at += Frame;
                }

                Assert.That(AtContact(outcome).HasStruck, Is.True, outcome.ToString());
                Assert.That(widest, Is.GreaterThan(0.4f), outcome.ToString());
                Assert.That(
                    widestAt, Is.GreaterThanOrEqualTo(fight.ContactAt - Frame), outcome.ToString());
            }
        }

        [Test]
        public void AWonFightHoldsTheControlsForTheClashAndTheDissolveAndNoLonger()
        {
            var win = Fight.Of(ActionOutcome.Win);

            Assert.That(win.Seconds, Is.EqualTo(VictoryStages.BlockingSeconds).Within(Tolerance));
            Assert.That(win.Seconds, Is.EqualTo(1.2f).Within(Tolerance));
            Assert.That(win.Stage, Is.EqualTo(VictoryStage.Clash));
            Assert.That(win.Advanced(VictoryStages.ClashSeconds).Stage, Is.EqualTo(VictoryStage.Dissolve));
            Assert.That(win.Advanced(win.Seconds).Stage, Is.EqualTo(VictoryStage.OrbFlight));
            Assert.That(win.Advanced(win.Seconds - 0.001f).IsSettled, Is.False);
            Assert.That(win.Advanced(win.Seconds).IsSettled, Is.True);
            Assert.That(win.Timeline.HasBegun, Is.True);
            Assert.That(Fight.Of(ActionOutcome.Tie).Timeline.HasBegun, Is.False);
            Assert.That(Fight.Of(ActionOutcome.Loss).Timeline.HasBegun, Is.False);
        }

        [Test]
        public void AWonClashIsOneBlowThePlayerThrowsAndTheEnemyNeverAnswers()
        {
            var win = Fight.Of(ActionOutcome.Win);

            Assert.That(win.ContactAt, Is.EqualTo(Fight.ExecutionAt).Within(Tolerance));
            Assert.That(win.ContactAt, Is.LessThan(VictoryStages.ClashSeconds));

            var struck = 0;
            var lunging = 0;
            var thrown = 0;

            foreach (var frame in Reel(ActionOutcome.Win))
            {
                if (frame.Impact > 0f)
                {
                    struck++;
                }

                if (frame.Shove > 0f)
                {
                    lunging++;
                }

                if (frame.Recoil > 0f)
                {
                    thrown++;
                }

                Assert.That(frame.Shove, Is.GreaterThanOrEqualTo(0f), frame.ToString());
                Assert.That(frame.Recoil, Is.GreaterThanOrEqualTo(0f), frame.ToString());
            }

            Assert.That(struck, Is.GreaterThan(0));
            Assert.That(lunging, Is.GreaterThan(0));
            Assert.That(thrown, Is.GreaterThan(0));
            Assert.That(
                Peak(Reel(ActionOutcome.Win), frame => frame.Recoil),
                Is.EqualTo(Fight.BlowTiles).Within(0.01f));
            Assert.That(
                Peak(Reel(ActionOutcome.Win), frame => frame.Shove),
                Is.LessThan(Fight.BlowTiles * 0.5f));
        }

        [Test]
        public void TheWinnersBlowRunsTheWholeClashAndTheLosersFallRunsToTheEndOfTheDissolve()
        {
            var win = Fight.Of(ActionOutcome.Win);

            Assert.That(win.BlowBeat, Is.EqualTo(VictoryStages.ClashSeconds).Within(Tolerance));
            Assert.That(
                win.FallBeat,
                Is.EqualTo(VictoryStages.BlockingSeconds - Fight.ExecutionAt).Within(Tolerance));
            Assert.That(win.ContactAt + win.FallBeat, Is.EqualTo(win.Seconds).Within(Tolerance));

            var loss = Fight.Of(ActionOutcome.Loss);

            Assert.That(loss.BlowBeat, Is.EqualTo(loss.Seconds).Within(Tolerance));
            Assert.That(loss.FallBeat, Is.EqualTo(Fight.LossSeconds).Within(Tolerance));
            Assert.That(loss.ContactAt + loss.FallBeat, Is.EqualTo(loss.Seconds).Within(Tolerance));

            var tie = Fight.Of(ActionOutcome.Tie);

            Assert.That(tie.BlowBeat, Is.EqualTo(tie.Seconds).Within(Tolerance));
            Assert.That(tie.FallBeat, Is.EqualTo(0f));
        }

        [Test]
        public void NobodyHasBeenStruckBeforeContactAndEverybodyHasAfterIt()
        {
            foreach (var outcome in Fights)
            {
                var fight = Fight.Of(outcome);

                Assert.That(fight.HasStruck, Is.False, outcome.ToString());
                Assert.That(
                    fight.Advanced(fight.ContactAt - Frame).HasStruck, Is.False, outcome.ToString());
                Assert.That(fight.Advanced(fight.ContactAt).HasStruck, Is.True, outcome.ToString());
                Assert.That(
                    fight.Advanced(fight.Seconds).HasStruck, Is.False, outcome.ToString());
            }
        }

        [Test]
        public void OnlyAWinIsAnExecutionAndOnlyForAsLongAsTheClashRuns()
        {
            var win = Fight.Of(ActionOutcome.Win);

            Assert.That(win.IsExecuting, Is.True);
            Assert.That(win.Advanced(VictoryStages.ClashSeconds - Frame).IsExecuting, Is.True);
            Assert.That(win.Advanced(VictoryStages.ClashSeconds).IsExecuting, Is.False);
            Assert.That(win.Advanced(win.Seconds).IsExecuting, Is.False);
            Assert.That(Fight.Of(ActionOutcome.Tie).IsExecuting, Is.False);
            Assert.That(Fight.Of(ActionOutcome.Loss).IsExecuting, Is.False);
            Assert.That(Fight.None.IsExecuting, Is.False);
        }

        [Test]
        public void ContactRingsThroughTheCameraAndDiesAwayInsideItsOwnWindow()
        {
            foreach (var outcome in Fights)
            {
                var fight = Fight.Of(outcome);

                Assert.That(fight.Impact, Is.EqualTo(0f), outcome.ToString());
                Assert.That(
                    fight.Advanced(fight.ContactAt - Frame).Impact, Is.EqualTo(0f), outcome.ToString());

                var struck = fight.Advanced(fight.ContactAt).Impact;

                Assert.That(struck, Is.EqualTo(1f).Within(Tolerance), outcome.ToString());
                Assert.That(
                    fight.Advanced(fight.ContactAt + Fight.ImpactSeconds * 0.5f).Impact,
                    Is.LessThan(struck),
                    outcome.ToString());
                Assert.That(
                    fight.Advanced(fight.ContactAt + Fight.ImpactSeconds).Impact,
                    Is.EqualTo(0f),
                    outcome.ToString());

                var ringing = 0f;
                foreach (var frame in Reel(outcome))
                {
                    Assert.That(frame.Impact, Is.InRange(0f, 1f), outcome.ToString());
                    ringing += frame.Impact > 0f ? Frame : 0f;
                }

                Assert.That(ringing, Is.LessThanOrEqualTo(Fight.ImpactSeconds + Frame), outcome.ToString());
                Assert.That(ringing, Is.GreaterThan(0f), outcome.ToString());
            }
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

            Assert.That(opening.Fade, Is.EqualTo(1f).Within(Tolerance));
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
                Assert.That(broken.Impact, Is.EqualTo(0f), outcome.ToString());
                Assert.That(broken.Shove, Is.EqualTo(0f), outcome.ToString());
                Assert.That(broken.Recoil, Is.EqualTo(0f), outcome.ToString());
                Assert.That(broken.Broken(), Is.EqualTo(broken), outcome.ToString());
            }

            Assert.That(Fight.Of(ActionOutcome.Win).Broken().Fade, Is.EqualTo(0f));
            Assert.That(Fight.None.Broken(), Is.EqualTo(Fight.None));
        }

        [Test]
        public void ALostFightThrowsThePlayerBackWhileTheEnemyStepsIn()
        {
            var loss = Reel(ActionOutcome.Loss);

            Assert.That(Peak(loss, frame => frame.Shove), Is.EqualTo(Fight.KnockbackTiles).Within(0.01f));
            Assert.That(Peak(loss, frame => frame.Recoil), Is.EqualTo(Fight.LungeTiles).Within(0.01f));

            foreach (var frame in loss)
            {
                Assert.That(frame.Shove, Is.LessThanOrEqualTo(0f), frame.ToString());
                Assert.That(frame.Recoil, Is.LessThanOrEqualTo(0f), frame.ToString());
                Assert.That(frame.Recoil, Is.GreaterThanOrEqualTo(frame.Shove), frame.ToString());
            }
        }

        [Test]
        public void ATieShovesBothFightersApartAndNeitherOfThemFalls()
        {
            var tie = Reel(ActionOutcome.Tie);

            Assert.That(Peak(tie, frame => frame.Recoil), Is.EqualTo(Fight.ClashTiles).Within(0.01f));
            Assert.That(Peak(tie, frame => frame.Shove), Is.EqualTo(Fight.ClashTiles).Within(0.01f));
            Assert.That(Fight.Of(ActionOutcome.Tie).FallBeat, Is.EqualTo(0f));

            foreach (var frame in tie)
            {
                Assert.That(frame.Shove, Is.LessThanOrEqualTo(0f), frame.ToString());
                Assert.That(frame.Recoil, Is.GreaterThanOrEqualTo(0f), frame.ToString());
            }
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

            var told = 0;
            var frames = 0;

            while (!tie.IsSettled || !loss.IsSettled)
            {
                if (!tie.Shove.Equals(loss.Shove) || !tie.Recoil.Equals(loss.Recoil))
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
