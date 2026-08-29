using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class VictoryTimelineTests
    {
        const float Tolerance = 1e-4f;

        const float Frame = 1f / 60f;

        const int Consecutive = 400;

        [Test]
        public void EveryStageNamesItsOwnDurationAndWhetherItHoldsTheControls()
        {
            Assert.That(VictoryStages.SecondsOf(VictoryStage.Clash), Is.EqualTo(0.9f).Within(Tolerance));
            Assert.That(VictoryStages.SecondsOf(VictoryStage.Dissolve), Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(VictoryStages.BlocksInput(VictoryStage.Clash), Is.True);
            Assert.That(VictoryStages.BlocksInput(VictoryStage.Dissolve), Is.True);

            Assert.That(VictoryStages.SecondsOf(VictoryStage.None), Is.EqualTo(0f));
            Assert.That(VictoryStages.SecondsOf(VictoryStage.Done), Is.EqualTo(0f));
            Assert.That(VictoryStages.BlocksInput(VictoryStage.None), Is.False);
            Assert.That(VictoryStages.BlocksInput(VictoryStage.Done), Is.False);
        }

        [Test]
        public void TheCeremonyIsTheSumOfTheStagesAndHoldsTheControlsForTheWholeOfIt()
        {
            var summed = 0f;
            foreach (var stage in VictoryStages.Order)
            {
                summed += VictoryStages.SecondsOf(stage);
            }

            Assert.That(VictoryStages.Seconds, Is.EqualTo(summed).Within(Tolerance));
            Assert.That(VictoryStages.Seconds, Is.EqualTo(1.2f).Within(Tolerance));
            Assert.That(VictoryStages.BlockingSeconds, Is.EqualTo(1.2f).Within(Tolerance));
        }

        [Test]
        public void EveryBlockingStageRunsBeforeEveryStageThatHandsTheControlsBack()
        {
            var handedBack = false;

            foreach (var stage in VictoryStages.Order)
            {
                if (!VictoryStages.BlocksInput(stage))
                {
                    handedBack = true;
                    continue;
                }

                Assert.That(handedBack, Is.False, stage.ToString());
                Assert.That(
                    VictoryStages.ClosesAt(stage),
                    Is.LessThanOrEqualTo(VictoryStages.BlockingSeconds + Tolerance),
                    stage.ToString());
            }
        }

        [Test]
        public void TheStagesRunInOrderAndEachOneHandsOnToTheNext()
        {
            Assert.That(VictoryStages.First, Is.EqualTo(VictoryStage.Clash));
            Assert.That(VictoryStages.After(VictoryStage.Clash), Is.EqualTo(VictoryStage.Dissolve));
            Assert.That(VictoryStages.After(VictoryStage.Dissolve), Is.EqualTo(VictoryStage.Done));
            Assert.That(VictoryStages.After(VictoryStage.Done), Is.EqualTo(VictoryStage.Done));
            Assert.That(VictoryStages.After(VictoryStage.None), Is.EqualTo(VictoryStage.None));

            Assert.That(VictoryStages.OpensAt(VictoryStage.Clash), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(
                VictoryStages.OpensAt(VictoryStage.Dissolve),
                Is.EqualTo(VictoryStages.ClosesAt(VictoryStage.Clash)).Within(Tolerance));
        }

        [Test]
        public void AStageTheCeremonyDoesNotHaveIsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryStages.SecondsOf((VictoryStage)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryStages.BlocksInput((VictoryStage)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryStages.OpensAt((VictoryStage)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryStages.After((VictoryStage)99));
        }

        [Test]
        public void ATimelineNobodyBeganStandsAtNoStageAndHoldsNothing()
        {
            var unbegun = VictoryTimeline.Unbegun;

            Assert.That(unbegun.HasBegun, Is.False);
            Assert.That(unbegun.Stage, Is.EqualTo(VictoryStage.None));
            Assert.That(unbegun.BlocksInput, Is.False);
            Assert.That(unbegun.IsOver, Is.True);
            Assert.That(unbegun.Elapsed, Is.EqualTo(0f));
            Assert.That(unbegun.Overrun, Is.EqualTo(0f));
            Assert.That(unbegun.BlockingSecondsLeft, Is.EqualTo(0f));
            Assert.That(unbegun.Advanced(10f), Is.EqualTo(unbegun));
            Assert.That(unbegun.Broken(), Is.EqualTo(unbegun));
        }

        [Test]
        public void ABegunTimelineOpensOnTheClashHoldingTheControls()
        {
            var timeline = VictoryTimeline.Begun;

            Assert.That(timeline.HasBegun, Is.True);
            Assert.That(timeline.Stage, Is.EqualTo(VictoryStages.First));
            Assert.That(timeline.Stage, Is.EqualTo(VictoryStage.Clash));
            Assert.That(timeline.StageElapsed, Is.EqualTo(0f));
            Assert.That(timeline.Through, Is.EqualTo(0f));
            Assert.That(timeline.BlocksInput, Is.True);
            Assert.That(timeline.IsOver, Is.False);
            Assert.That(timeline.BlockingSecondsLeft, Is.EqualTo(1.2f).Within(Tolerance));
        }

        [Test]
        public void EachStageMeasuresItsOwnProgressOnItsOwnClock()
        {
            var half = VictoryTimeline.Begun.Advanced(VictoryStages.ClashSeconds * 0.5f);

            Assert.That(half.Stage, Is.EqualTo(VictoryStage.Clash));
            Assert.That(half.Through, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(
                half.StageElapsed,
                Is.EqualTo(VictoryStages.ClashSeconds * 0.5f).Within(Tolerance));

            var dissolving = VictoryTimeline.Begun.Advanced(
                VictoryStages.ClashSeconds + VictoryStages.DissolveSeconds * 0.5f);

            Assert.That(dissolving.Stage, Is.EqualTo(VictoryStage.Dissolve));
            Assert.That(dissolving.Through, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(
                dissolving.StageElapsed,
                Is.EqualTo(VictoryStages.DissolveSeconds * 0.5f).Within(Tolerance));
        }

        [Test]
        public void TheControlsAreHeldToTheLastInstantOfTheDissolveAndHandedBackAtItsEnd()
        {
            var held = VictoryTimeline.Begun.Advanced(VictoryStages.BlockingSeconds - 0.001f);
            var freed = VictoryTimeline.Begun.Advanced(VictoryStages.BlockingSeconds);

            Assert.That(held.BlocksInput, Is.True);
            Assert.That(held.Stage, Is.EqualTo(VictoryStage.Dissolve));
            Assert.That(freed.BlocksInput, Is.False);
            Assert.That(freed.Stage, Is.EqualTo(VictoryStage.Done));
            Assert.That(freed.IsOver, Is.True);
            Assert.That(freed.BlockingSecondsLeft, Is.EqualTo(0f));
        }

        [Test]
        public void AdvancingTheTimelineWalksItThroughEveryStageInOrderAndStopsAtDone()
        {
            var timeline = VictoryTimeline.Begun;
            var walked = new List<VictoryStage>();
            var opened = new List<float>();
            var clock = 0d;

            for (var frame = 0; frame < 600 && !timeline.IsOver; frame++)
            {
                if (walked.Count == 0 || walked[walked.Count - 1] != timeline.Stage)
                {
                    walked.Add(timeline.Stage);
                    opened.Add((float)clock);
                }

                timeline = timeline.Advanced(Frame);
                clock += Frame;
            }

            walked.Add(timeline.Stage);
            opened.Add((float)clock);

            Assert.That(walked, Is.EqualTo(new[] { VictoryStage.Clash, VictoryStage.Dissolve, VictoryStage.Done }));
            Assert.That(opened[0], Is.EqualTo(0f).Within(Tolerance));
            Assert.That(opened[1], Is.EqualTo(VictoryStages.ClashSeconds).Within(Frame * 1.5f));
            Assert.That(opened[2], Is.EqualTo(VictoryStages.Seconds).Within(Frame * 1.5f));
        }

        [Test]
        public void TheOverrunIsWhatIsLeftOverOnTheFrameTheControlsComeBack()
        {
            var timeline = VictoryTimeline.Begun.Advanced(VictoryStages.BlockingSeconds - 0.004f);

            Assert.That(timeline.Overrun, Is.EqualTo(0f));

            var spilt = timeline.Advanced(Frame);

            Assert.That(spilt.BlocksInput, Is.False);
            Assert.That(spilt.Overrun, Is.EqualTo(Frame - 0.004f).Within(Tolerance));
        }

        [Test]
        public void BreakingOffAVictorySkipsStraightToTheEndOfTheStagesThatHoldTheControls()
        {
            var broken = VictoryTimeline.Begun.Advanced(0.1f).Broken();

            Assert.That(broken.BlocksInput, Is.False);
            Assert.That(broken.Elapsed, Is.EqualTo(VictoryStages.BlockingSeconds).Within(Tolerance));
            Assert.That(broken.Overrun, Is.EqualTo(0f));
            Assert.That(broken.Broken(), Is.EqualTo(broken));
        }

        [Test]
        public void ATimelineOnlyEverPlaysForwards()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryTimeline.Begun.Advanced(-Frame));
        }

        [Test]
        public void ARunOfConsecutiveVictoriesAccumulatesNoDriftInAnyStagesTiming()
        {
            var clock = 0d;
            var carried = 0f;
            var worstRelease = 0f;
            var worstDissolve = 0f;

            for (var fight = 0; fight < Consecutive; fight++)
            {
                var contact = clock - carried;
                var timeline = VictoryTimeline.Begun.Advanced(carried);

                var dissolveOpened = -1d;
                var delta = Frame * (1f + 0.25f * (fight % 5 - 2));

                for (var frame = 0; frame < 600 && timeline.BlocksInput; frame++)
                {
                    timeline = timeline.Advanced(delta);
                    clock += delta;

                    if (dissolveOpened < 0d && timeline.Stage != VictoryStage.Clash)
                    {
                        dissolveOpened = clock - timeline.StageElapsed;
                    }
                }

                Assert.That(timeline.BlocksInput, Is.False, "fight " + fight);
                Assert.That(dissolveOpened, Is.GreaterThanOrEqualTo(0d), "fight " + fight);

                var released = clock - timeline.Overrun;

                worstRelease = Wider(
                    worstRelease, released - contact - VictoryStages.BlockingSeconds);
                worstDissolve = Wider(
                    worstDissolve, dissolveOpened - contact - VictoryStages.ClashSeconds);

                carried = timeline.Overrun;
            }

            Assert.That(worstRelease, Is.LessThan(0.001f));
            Assert.That(worstDissolve, Is.LessThan(0.001f));
            Assert.That(
                (float)(clock - carried),
                Is.EqualTo(Consecutive * VictoryStages.BlockingSeconds).Within(0.01f));
        }

        [Test]
        public void TheLastVictoryOfALongRunHoldsTheControlsForAsLongAsTheFirstDid()
        {
            var spans = SpansOver(Consecutive);
            var shortest = spans[0];
            var longest = spans[0];

            foreach (var span in spans)
            {
                shortest = span < shortest ? span : shortest;
                longest = span > longest ? span : longest;
            }

            Assert.That(spans.Count, Is.EqualTo(Consecutive));
            Assert.That(longest - shortest, Is.LessThan(0.001f));
            Assert.That(spans[0], Is.EqualTo(VictoryStages.BlockingSeconds).Within(0.001f));
            Assert.That(
                spans[spans.Count - 1],
                Is.EqualTo(VictoryStages.BlockingSeconds).Within(0.001f));
        }

        [Test]
        public void TwoTimelinesShowingTheSameThingAreTheSameValue()
        {
            var one = VictoryTimeline.Begun.Advanced(0.2f);
            var other = VictoryTimeline.Begun.Advanced(0.2f);

            Assert.That(one, Is.EqualTo(other));
            Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(one.Equals((object)other), Is.True);
            Assert.That(one, Is.Not.EqualTo(VictoryTimeline.Unbegun));
            Assert.That(VictoryTimeline.Unbegun.ToString(), Does.Contain("no victory"));
            Assert.That(one.ToString(), Does.Contain("Clash"));
            Assert.That(one.ToString(), Does.Contain("holding"));
        }

        static List<float> SpansOver(int fights)
        {
            var clock = 0d;
            var carried = 0f;
            var spans = new List<float>();

            for (var fight = 0; fight < fights; fight++)
            {
                var contact = clock - carried;
                var timeline = VictoryTimeline.Begun.Advanced(carried);
                var delta = Frame * (1f + 0.25f * (fight % 5 - 2));

                for (var frame = 0; frame < 600 && timeline.BlocksInput; frame++)
                {
                    timeline = timeline.Advanced(delta);
                    clock += delta;
                }

                spans.Add((float)(clock - timeline.Overrun - contact));
                carried = timeline.Overrun;
            }

            return spans;
        }

        static float Wider(float worst, double slip)
        {
            var away = (float)Math.Abs(slip);

            return away > worst ? away : worst;
        }
    }
}
