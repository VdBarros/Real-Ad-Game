using System;
using System.Collections.Generic;
using System.Linq;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class FigureActTests
    {
        const float Tolerance = 1e-4f;

        const float Frame = 1f / 60f;

        static readonly ActionOutcome[] FoughtOutcomes =
        {
            ActionOutcome.Win, ActionOutcome.Tie, ActionOutcome.Loss
        };

        [Test]
        public void EveryActNamesOneClipAndEveryClipIsWanted()
        {
            var acts = Enum.GetValues(typeof(FigureAct)).Cast<FigureAct>().ToList();

            Assert.That(acts.Count, Is.EqualTo(AdventurerClips.Count));
            Assert.That(AdventurerClips.Names.Distinct().Count(), Is.EqualTo(AdventurerClips.Count));

            foreach (var act in acts)
            {
                var clip = AdventurerClips.NameOf(act);

                Assert.That(clip, Is.Not.Null.And.Not.Empty, act.ToString());
                Assert.That(AdventurerClips.Wants(clip), Is.True, act.ToString());
                Assert.That(AdventurerClips.LoopsOf(clip), Is.EqualTo(AdventurerClips.Loops(act)), act.ToString());
            }
        }

        [Test]
        public void OnlyTheStandingAndTravellingActsLoop()
        {
            Assert.That(AdventurerClips.Loops(FigureAct.Idle), Is.True);
            Assert.That(AdventurerClips.Loops(FigureAct.Walk), Is.True);
            Assert.That(AdventurerClips.Loops(FigureAct.Retreat), Is.True);
            Assert.That(AdventurerClips.Loops(FigureAct.Strike), Is.False);
            Assert.That(AdventurerClips.Loops(FigureAct.Clash), Is.False);
            Assert.That(AdventurerClips.Loops(FigureAct.Recoil), Is.False);
            Assert.That(AdventurerClips.Loops(FigureAct.Take), Is.False);
        }

        [Test]
        public void AClipNameOutsideTheTableIsNotWanted()
        {
            Assert.That(AdventurerClips.Wants("Sit_Chair_Idle"), Is.False);
            Assert.That(AdventurerClips.Wants(string.Empty), Is.False);
            Assert.That(AdventurerClips.Wants(null), Is.False);
            Assert.That(AdventurerClips.LoopsOf("Sit_Chair_Idle"), Is.False);
        }

        [Test]
        public void NoActOutsideTheEnumNamesAClip()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AdventurerClips.NameOf((FigureAct)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => AdventurerClips.Loops((FigureAct)(-1)));
        }

        [Test]
        public void ACueCutToABeatNeedsABeat()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FigureCue.Within(FigureAct.Take, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FigureCue.Within(FigureAct.Take, -1f));
        }

        [Test]
        public void AStandingFigureCuesItsIdleClipOnALoop()
        {
            Assert.That(FigureCue.Still.Act, Is.EqualTo(FigureAct.Idle));
            Assert.That(FigureCue.Still.Clip, Is.EqualTo(AdventurerClips.Idle));
            Assert.That(FigureCue.Still.Loops, Is.True);
            Assert.That(FigureCue.Still.Beat, Is.EqualTo(0f));
        }

        [Test]
        public void NoJourneyAtAllCuesTheIdleClip()
        {
            Assert.That(FigureCues.Of(null), Is.EqualTo(FigureCue.Still));
            Assert.That(FigureCues.Of(Journey.Nowhere), Is.EqualTo(FigureCue.Still));
        }

        [Test]
        public void AWalkingJourneyCuesTheWalkClipAndAFallingBackOneCuesTheRetreat()
        {
            var journey = Journey.Toward(Opening(), Toward(Opening()));

            Assert.That(FigureCues.Of(journey), Is.EqualTo(FigureCue.Walking));
            Assert.That(FigureCues.Of(journey).Clip, Is.EqualTo(AdventurerClips.Walk));
            Assert.That(FigureCues.Of(journey).Loops, Is.True);

            var falling = journey.Advanced(0.1f).Cancelled();

            Assert.That(falling.IsOver, Is.False);
            Assert.That(falling.Walk.IsRetreating, Is.True);
            Assert.That(FigureCues.Of(falling).Act, Is.EqualTo(FigureAct.Retreat));
            Assert.That(FigureCues.Of(falling).Clip, Is.EqualTo(AdventurerClips.Retreat));
            Assert.That(FigureCues.Of(falling).Loops, Is.True);
        }

        [Test]
        public void EveryFoughtOutcomeCuesItsOwnClipCutToTheFightsOwnSeconds()
        {
            var seen = new List<FigureAct>();

            foreach (var outcome in FoughtOutcomes)
            {
                var fight = Fight.Of(outcome);
                var cue = FigureCues.Striking(fight);

                Assert.That(fight.IsJoined, Is.True, outcome.ToString());
                Assert.That(cue.Loops, Is.False, outcome.ToString());
                Assert.That(cue.Beat, Is.EqualTo(fight.Seconds).Within(Tolerance), outcome.ToString());
                Assert.That(seen.Contains(cue.Act), Is.False, outcome.ToString());

                seen.Add(cue.Act);
            }

            Assert.That(FigureCues.Striking(Fight.Of(ActionOutcome.Win)).Act, Is.EqualTo(FigureAct.Strike));
            Assert.That(FigureCues.Striking(Fight.Of(ActionOutcome.Tie)).Act, Is.EqualTo(FigureAct.Clash));
            Assert.That(FigureCues.Striking(Fight.Of(ActionOutcome.Loss)).Act, Is.EqualTo(FigureAct.Recoil));
        }

        [Test]
        public void AFightNobodyJoinedCuesNothingButTheIdleClip()
        {
            Assert.That(FigureCues.Striking(Fight.None), Is.EqualTo(FigureCue.Still));
            Assert.That(FigureCues.Striking(Fight.Of(ActionOutcome.Walked)), Is.EqualTo(FigureCue.Still));
        }

        [Test]
        public void EveryFoughtOutcomeAnswersWithTheMirroredActOnTheSameBeat()
        {
            foreach (var outcome in FoughtOutcomes)
            {
                var fight = Fight.Of(outcome);
                var blow = FigureCues.Striking(fight);
                var reply = FigureCues.Answering(fight);

                Assert.That(reply.Loops, Is.False, outcome.ToString());
                Assert.That(reply.Beat, Is.EqualTo(blow.Beat).Within(Tolerance), outcome.ToString());
                Assert.That(reply.Beat, Is.EqualTo(fight.Seconds).Within(Tolerance), outcome.ToString());
                Assert.That(reply.Act, Is.EqualTo(FigureCues.Answered(blow.Act)), outcome.ToString());
            }

            Assert.That(FigureCues.Answering(Fight.Of(ActionOutcome.Win)).Act, Is.EqualTo(FigureAct.Recoil));
            Assert.That(FigureCues.Answering(Fight.Of(ActionOutcome.Tie)).Act, Is.EqualTo(FigureAct.Clash));
            Assert.That(FigureCues.Answering(Fight.Of(ActionOutcome.Loss)).Act, Is.EqualTo(FigureAct.Strike));
        }

        [Test]
        public void TheEnemyPlaysWhatThePlayerWouldHaveHadTheFightGoneTheOtherWay()
        {
            foreach (var outcome in FoughtOutcomes)
            {
                var reversed = Fight.Of(Reversed(outcome));

                Assert.That(
                    FigureCues.Answering(Fight.Of(outcome)).Act,
                    Is.EqualTo(FigureCues.Striking(reversed).Act),
                    outcome.ToString());
            }
        }

        [Test]
        public void TheAnswerIsCutToTheFightsOwnBeatRatherThanTheMirroredFights()
        {
            var win = Fight.Of(ActionOutcome.Win);
            var loss = Fight.Of(ActionOutcome.Loss);

            Assert.That(Math.Abs(win.Seconds - loss.Seconds), Is.GreaterThan(Tolerance));
            Assert.That(FigureCues.Answering(win).Beat, Is.EqualTo(win.Seconds).Within(Tolerance));
            Assert.That(FigureCues.Answering(loss).Beat, Is.EqualTo(loss.Seconds).Within(Tolerance));
        }

        [Test]
        public void MirroringAnActTwiceGivesTheActBack()
        {
            foreach (var act in Enum.GetValues(typeof(FigureAct)).Cast<FigureAct>())
            {
                Assert.That(FigureCues.Answered(FigureCues.Answered(act)), Is.EqualTo(act), act.ToString());
            }

            Assert.That(FigureCues.Answered(FigureAct.Strike), Is.EqualTo(FigureAct.Recoil));
            Assert.That(FigureCues.Answered(FigureAct.Recoil), Is.EqualTo(FigureAct.Strike));
        }

        [Test]
        public void OnlyABlowAndItsAnswerAreMirroredAtAll()
        {
            var unmirrored = new[] { FigureAct.Idle, FigureAct.Walk, FigureAct.Retreat, FigureAct.Take };

            foreach (var act in unmirrored)
            {
                Assert.That(FigureCues.Answered(act), Is.EqualTo(act), act.ToString());
            }

            Assert.That(FigureCues.Answered(FigureAct.Clash), Is.EqualTo(FigureAct.Clash));
        }

        [Test]
        public void BothSidesOfEveryFightPlayABlowAndNeitherStandsStill()
        {
            foreach (var outcome in FoughtOutcomes)
            {
                var fight = Fight.Of(outcome);
                var sides = new[] { FigureCues.Striking(fight).Act, FigureCues.Answering(fight).Act };

                Assert.That(sides, Has.No.Member(FigureAct.Idle), outcome.ToString());
                Assert.That(sides, Has.No.Member(FigureAct.Walk), outcome.ToString());
                Assert.That(sides, Has.No.Member(FigureAct.Retreat), outcome.ToString());
                Assert.That(sides, Has.No.Member(FigureAct.Take), outcome.ToString());
                Assert.That(
                    sides[0] == FigureAct.Clash,
                    Is.EqualTo(sides[1] == FigureAct.Clash),
                    outcome.ToString());
            }
        }

        [Test]
        public void AFightNobodyJoinedIsAnsweredWithNothingButTheIdleClip()
        {
            Assert.That(FigureCues.Answering(Fight.None), Is.EqualTo(FigureCue.Still));
            Assert.That(FigureCues.Answering(Fight.Of(ActionOutcome.Walked)), Is.EqualTo(FigureCue.Still));
            Assert.That(FigureCues.Answering(Fight.Of(ActionOutcome.Rejected)), Is.EqualTo(FigureCue.Still));
        }

        [Test]
        public void AnAnsweringCueRunsOutOnTheSameFrameTheBlowDoes()
        {
            foreach (var outcome in FoughtOutcomes)
            {
                var fight = Fight.Of(outcome);
                var blow = FigureMotion.Still.Cued(FigureCues.Striking(fight));
                var reply = FigureMotion.Still.Cued(FigureCues.Answering(fight));

                for (var frame = 0; frame * Frame < fight.Seconds + Frame; frame++)
                {
                    blow = blow.Advanced(Frame);
                    reply = reply.Advanced(Frame);

                    Assert.That(
                        reply.Act == FigureAct.Idle,
                        Is.EqualTo(blow.Act == FigureAct.Idle),
                        outcome + " frame " + frame);
                }

                Assert.That(blow.Act, Is.EqualTo(FigureAct.Idle), outcome.ToString());
                Assert.That(reply.Act, Is.EqualTo(FigureAct.Idle), outcome.ToString());
            }
        }

        static ActionOutcome Reversed(ActionOutcome outcome)
        {
            if (outcome == ActionOutcome.Win)
            {
                return ActionOutcome.Loss;
            }

            return outcome == ActionOutcome.Loss ? ActionOutcome.Win : outcome;
        }

        [Test]
        public void APickupCuesTheTakeClipCutToTheTakesOwnSeconds()
        {
            var cue = FigureCue.Within(FigureAct.Take, Take.Seconds);

            Assert.That(cue.Act, Is.EqualTo(FigureAct.Take));
            Assert.That(cue.Clip, Is.EqualTo(AdventurerClips.Take));
            Assert.That(cue.Loops, Is.False);
            Assert.That(cue.Beat, Is.EqualTo(Take.Seconds).Within(Tolerance));
        }

        [Test]
        public void AClipLongerThanItsBeatIsSpedUpUntilItEndsOnTheBeat()
        {
            var beat = 0.3f;
            var cue = FigureCue.Within(FigureAct.Take, beat);
            var clip = 1.2f;

            Assert.That(cue.SpeedIn(clip), Is.EqualTo(clip / beat).Within(Tolerance));
            Assert.That(cue.TimeIn(clip, beat), Is.EqualTo(clip).Within(Tolerance));
            Assert.That(cue.EndsWithin(clip), Is.True);
            Assert.That(cue.TimeIn(clip, beat * 0.5f), Is.EqualTo(clip * 0.5f).Within(Tolerance));
        }

        [Test]
        public void AClipShorterThanItsBeatPlaysAtItsOwnSpeedAndHoldsItsLastFrame()
        {
            var beat = 1f;
            var cue = FigureCue.Within(FigureAct.Take, beat);
            var clip = 0.4f;

            Assert.That(cue.SpeedIn(clip), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(cue.TimeIn(clip, 0.2f), Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(cue.TimeIn(clip, 0.9f), Is.EqualTo(clip).Within(Tolerance));
            Assert.That(cue.TimeIn(clip, beat), Is.EqualTo(clip).Within(Tolerance));
            Assert.That(cue.EndsWithin(clip), Is.True);
        }

        [Test]
        public void ALoopingCueWrapsRoundItsClipAndNeverRunsOut()
        {
            var cue = FigureCue.Walking;
            var clip = 0.5f;

            Assert.That(cue.SpeedIn(clip), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(cue.TimeIn(clip, 0.1f), Is.EqualTo(0.1f).Within(Tolerance));
            Assert.That(cue.TimeIn(clip, 0.6f), Is.EqualTo(0.1f).Within(Tolerance));
            Assert.That(cue.TimeIn(clip, 4.35f), Is.EqualTo(0.35f).Within(Tolerance));
            Assert.That(cue.EndsWithin(clip), Is.True);
        }

        [Test]
        public void ACueSampledOutsideAClipStaysAtTheStart()
        {
            Assert.That(FigureCue.Walking.TimeIn(0f, 3f), Is.EqualTo(0f));
            Assert.That(FigureCue.Walking.TimeIn(-1f, 3f), Is.EqualTo(0f));
            Assert.That(FigureCue.Walking.TimeIn(1f, -3f), Is.EqualTo(0f));
            Assert.That(FigureCue.Walking.SpeedIn(0f), Is.EqualTo(1f));
        }

        [Test]
        public void AMotionStartsStillAndKeepsPlayingTheCueItIsHandedAgain()
        {
            var motion = FigureMotion.Still;

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Idle));
            Assert.That(motion.Elapsed, Is.EqualTo(0f));

            motion = motion.Cued(FigureCue.Walking).Advanced(Frame).Advanced(Frame);

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Walk));
            Assert.That(motion.Elapsed, Is.EqualTo(Frame * 2f).Within(Tolerance));

            motion = motion.Cued(FigureCue.Walking);

            Assert.That(motion.Elapsed, Is.EqualTo(Frame * 2f).Within(Tolerance));
        }

        [Test]
        public void ANewCueRestartsTheClipFromItsFirstFrame()
        {
            var motion = FigureMotion.Still.Cued(FigureCue.Walking).Advanced(0.4f);

            Assert.That(motion.Elapsed, Is.EqualTo(0.4f).Within(Tolerance));

            motion = motion.Cued(FigureCue.Looping(FigureAct.Retreat));

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Retreat));
            Assert.That(motion.Elapsed, Is.EqualTo(0f));
        }

        [Test]
        public void ABeatHandsTheFigureBackToIdleWhenItRunsOut()
        {
            var beat = FigureCue.Within(FigureAct.Take, Take.Seconds);
            var motion = FigureMotion.Still.Cued(beat);
            var frames = 0;

            while (motion.Act != FigureAct.Idle && frames < 600)
            {
                motion = motion.Advanced(Frame);
                frames++;
            }

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Idle));
            Assert.That(frames * Frame, Is.LessThanOrEqualTo(Take.Seconds + Frame + Tolerance));
            Assert.That(motion.HasSpentABeat, Is.True);
        }

        [Test]
        public void ASpentBeatIsNotRetriggeredWhileTheSameCueKeepsArriving()
        {
            var beat = FigureCue.Within(FigureAct.Take, Take.Seconds);
            var motion = FigureMotion.Still.Cued(beat);

            for (var frame = 0; frame < 600; frame++)
            {
                motion = motion.Cued(beat).Advanced(Frame);
            }

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Idle));
            Assert.That(motion.HasSpentABeat, Is.True);
        }

        [Test]
        public void ASpentBeatDoesNotBlockADifferentCue()
        {
            var beat = FigureCue.Within(FigureAct.Take, Take.Seconds);
            var motion = FigureMotion.Still.Cued(beat).Advanced(Take.Seconds + Frame);

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Idle));

            motion = motion.Cued(FigureCue.Walking);

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Walk));
            Assert.That(motion.HasSpentABeat, Is.False);

            motion = motion.Cued(beat);

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Take));
            Assert.That(motion.Elapsed, Is.EqualTo(0f));
        }

        [Test]
        public void ALoopingMotionNeverHandsItselfBack()
        {
            var motion = FigureMotion.Still.Cued(FigureCue.Walking);

            for (var frame = 0; frame < 600; frame++)
            {
                motion = motion.Advanced(Frame);
            }

            Assert.That(motion.Act, Is.EqualTo(FigureAct.Walk));
            Assert.That(motion.HasSpentABeat, Is.False);
        }

        [Test]
        public void AMotionOnlyEverRunsForwards()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FigureMotion.Still.Advanced(-Frame));
        }

        [Test]
        public void AMotionSamplesTheClipThroughItsOwnCue()
        {
            var beat = FigureCue.Within(FigureAct.Take, 0.3f);
            var motion = FigureMotion.Still.Cued(beat).Advanced(0.15f);

            Assert.That(motion.SpeedIn(1.2f), Is.EqualTo(4f).Within(Tolerance));
            Assert.That(motion.TimeIn(1.2f), Is.EqualTo(0.6f).Within(Tolerance));
        }

        [Test]
        public void TwoMotionsInTheSameStateAreTheSameValue()
        {
            var one = FigureMotion.Still.Cued(FigureCue.Walking).Advanced(Frame);
            var other = FigureMotion.Still.Cued(FigureCue.Walking).Advanced(Frame);

            Assert.That(one, Is.EqualTo(other));
            Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(one.Equals((object)other), Is.True);
            Assert.That(one.Equals(FigureMotion.Still), Is.False);
            Assert.That(FigureCue.Walking, Is.Not.EqualTo(FigureCue.Still));
            Assert.That(FigureCue.Walking.Equals((object)FigureCue.Walking), Is.True);
            Assert.That(FigureCue.Walking.GetHashCode(), Is.EqualTo(FigureCue.Walking.GetHashCode()));
        }

        [Test]
        public void ACueAndAMotionSayWhatTheyArePlaying()
        {
            Assert.That(FigureCue.Walking.ToString(), Does.Contain("Walk"));
            Assert.That(FigureCue.Within(FigureAct.Take, 0.3f).ToString(), Does.Contain("0.3"));
            Assert.That(FigureMotion.Still.ToString(), Does.Contain("Idle"));
        }

        [Test]
        public void AClipIsOnlyEverComplainedAboutOnce()
        {
            var complaints = new ClipComplaints();

            Assert.That(complaints.ShouldSay("Idle"), Is.True);
            Assert.That(complaints.ShouldSay("Idle"), Is.False);
            Assert.That(complaints.ShouldSay("Walking_A"), Is.True);
            Assert.That(complaints.Said, Is.EqualTo(2));
            complaints.Forget();

            Assert.That(complaints.Said, Is.EqualTo(0));
            Assert.That(complaints.ShouldSay("Idle"), Is.True);
        }

        [Test]
        public void AnUnnamedClipIsNeverComplainedAbout()
        {
            var complaints = new ClipComplaints();

            Assert.That(complaints.ShouldSay(null), Is.False);
            Assert.That(complaints.ShouldSay(string.Empty), Is.False);
            Assert.That(complaints.Said, Is.EqualTo(0));
        }

        [Test]
        public void AWholeJourneyCuesWalkThenTheBeatThenIdleWithoutMovingASettledTiming()
        {
            var state = Opening();
            var journey = Journey.Toward(state, Toward(state));
            var motion = FigureMotion.Still;
            var acts = new List<FigureAct>();
            var frames = 0;

            while (!journey.IsOver && frames < 4000)
            {
                journey = journey.Advanced(Frame);
                motion = motion.Cued(FigureCues.Of(journey)).Advanced(Frame);
                frames++;

                if (acts.Count == 0 || acts[acts.Count - 1] != motion.Act)
                {
                    acts.Add(motion.Act);
                }

                if (journey.IsWaiting && journey.Fight.IsSettled && !journey.HoldsForABeat)
                {
                    journey = journey.Resumed();
                }
            }

            Assert.That(journey.IsOver, Is.True);
            Assert.That(acts, Does.Contain(FigureAct.Walk));
            Assert.That(acts[acts.Count - 1], Is.EqualTo(FigureAct.Idle));
        }

        static RunState Opening()
        {
            var graph = LevelGenerator.Generate(20250824L, MazePreset.Ship).Graph;

            return RunState.Begin(graph, PowerTuning.For(MazePreset.Ship).StartingPower);
        }

        static int Toward(RunState state)
        {
            foreach (var nodeId in TapAim.Aimable(state))
            {
                var resolved = ActionResolver.Resolve(state, nodeId);

                if (resolved.Outcome != ActionOutcome.Rejected && resolved.Route.Count > 2)
                {
                    return nodeId;
                }
            }

            foreach (var nodeId in TapAim.Aimable(state))
            {
                if (ActionResolver.Resolve(state, nodeId).Outcome != ActionOutcome.Rejected)
                {
                    return nodeId;
                }
            }

            return TapAim.Nothing;
        }
    }
}
