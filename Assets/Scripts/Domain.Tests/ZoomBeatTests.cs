using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ZoomBeatTests
    {
        const float Frame = 1f / 60f;

        static readonly TilePosition Gate = new TilePosition(2, 3, 4);

        static readonly WorldPoint Anchor = IsoProjection.Of(Gate);

        static readonly CameraFraming Resting = LevelFraming.Play(Anchor);

        [Test]
        public void NoBeatIsRunningUntilOneIsCutTo()
        {
            Assert.That(ZoomBeat.None.IsSettled, Is.True);
            Assert.That(ZoomBeat.None.IsGripping, Is.False);
            Assert.That(default(ZoomBeat).IsSettled, Is.True);
            Assert.That(ZoomBeat.On(Anchor).IsSettled, Is.False);
            Assert.That(ZoomBeat.On(Anchor).IsGripping, Is.True);
        }

        [Test]
        public void ThePunchIsARatioOfWhateverFramingItIsLaidOver()
        {
            var peak = ZoomBeat.On(Anchor).Advanced(ZoomBeat.InSeconds);

            Assert.That(
                peak.Over(Resting).OrthographicSize,
                Is.EqualTo(Resting.OrthographicSize / ZoomBeat.Punch).Within(1e-4f));

            var wider = new CameraFraming(Anchor, Resting.OrthographicSize * 3f);

            Assert.That(
                peak.Over(wider).OrthographicSize,
                Is.EqualTo(wider.OrthographicSize / ZoomBeat.Punch).Within(1e-4f));
        }

        [Test]
        public void ThePunchIsBetweenATenthAndAQuarterCloser()
        {
            Assert.That(ZoomBeat.Punch, Is.GreaterThanOrEqualTo(1.10f));
            Assert.That(ZoomBeat.Punch, Is.LessThanOrEqualTo(1.25f));
        }

        [Test]
        public void ThePunchEasesInOverAnEighthOfASecondRatherThanCutting()
        {
            Assert.That(ZoomBeat.InSeconds, Is.EqualTo(0.15f).Within(0.03f));

            var beat = ZoomBeat.On(Anchor);

            Assert.That(beat.Over(Resting), Is.EqualTo(Resting), "The punch began with a cut.");

            var sizes = new List<float>();
            for (var elapsed = 0f; elapsed < ZoomBeat.InSeconds; elapsed += Frame)
            {
                sizes.Add(beat.Over(Resting).OrthographicSize);
                beat = beat.Advanced(Frame);
            }

            Assert.That(sizes.Count, Is.GreaterThan(4), "The ease in ran too few frames to read as a move.");

            for (var step = 1; step < sizes.Count; step++)
            {
                Assert.That(sizes[step], Is.LessThan(sizes[step - 1]), "The punch stalled on its way in.");
            }

            var biggestStep = 0f;
            for (var step = 1; step < sizes.Count; step++)
            {
                biggestStep = Math.Max(biggestStep, sizes[step - 1] - sizes[step]);
            }

            Assert.That(
                biggestStep,
                Is.LessThan(Resting.OrthographicSize - Resting.OrthographicSize / ZoomBeat.Punch),
                "One frame swallowed the whole punch, which is a cut.");
        }

        [Test]
        public void ThePunchEasesOutOverNearlyHalfASecondRatherThanCutting()
        {
            Assert.That(ZoomBeat.OutSeconds, Is.EqualTo(0.4f).Within(0.05f));

            var beat = ZoomBeat.On(Anchor).Advanced(ZoomBeat.FloorSeconds).Released();
            var peak = beat.Over(Resting).OrthographicSize;
            var sizes = new List<float>();

            for (var frames = 0; frames < 40 && !beat.IsSettled; frames++)
            {
                beat = beat.Advanced(Frame);
                sizes.Add(beat.Over(Resting).OrthographicSize);
            }

            Assert.That(beat.IsSettled, Is.True);
            Assert.That(sizes.Count, Is.GreaterThan(12), "The ease out ran too few frames to read as a move.");

            var previous = peak;
            var biggestStep = 0f;
            foreach (var size in sizes)
            {
                Assert.That(size, Is.GreaterThanOrEqualTo(previous), "The punch went back in on its way out.");
                biggestStep = Math.Max(biggestStep, size - previous);
                previous = size;
            }

            Assert.That(previous, Is.EqualTo(Resting.OrthographicSize).Within(1e-4f));
            Assert.That(
                biggestStep,
                Is.LessThan(Resting.OrthographicSize - peak),
                "One frame swallowed the whole return, which is a cut.");
        }

        [Test]
        public void TheEaseBackTakesLongerThanTheEaseIn()
        {
            Assert.That(ZoomBeat.OutSeconds, Is.GreaterThan(ZoomBeat.InSeconds));
        }

        [Test]
        public void AReleaseBeforeTheFloorStillHoldsUntilTheFloor()
        {
            var beat = ZoomBeat.On(Anchor).Advanced(0.1f).Released();

            Assert.That(beat.IsGripping, Is.True);

            beat = beat.Advanced(ZoomBeat.FloorSeconds - 0.1f);

            Assert.That(beat.IsGripping, Is.False);
        }

        [Test]
        public void ThePunchHoldsAtItsPeakForAsLongAsTheAnimationItEnclosesTakes()
        {
            var beat = ZoomBeat.On(Anchor).Advanced(ZoomBeat.FloorSeconds + 0.2f);

            Assert.That(beat.IsGripping, Is.True);
            Assert.That(
                beat.Over(Resting).OrthographicSize,
                Is.EqualTo(Resting.OrthographicSize / ZoomBeat.Punch).Within(1e-4f));

            Assert.That(beat.Released().IsGripping, Is.False);
        }

        [Test]
        public void AnEnclosedAnimationThatNeverSettlesIsCutOffAtTheCap()
        {
            var beat = ZoomBeat.On(Anchor).Advanced(ZoomBeat.CapSeconds);

            Assert.That(beat.IsGripping, Is.False);
            Assert.That(beat.IsSettled, Is.False);
            Assert.That(beat.Advanced(ZoomBeat.OutSeconds).IsSettled, Is.True);
        }

        [Test]
        public void TheGripEndsBeforeTheReturnDoesSoTheLockoutIsShorterThanTheBeat()
        {
            var beat = ZoomBeat.On(Anchor).Advanced(ZoomBeat.FloorSeconds).Released();

            Assert.That(beat.IsGripping, Is.False);

            var moving = 0;
            for (var frames = 0; frames < 60 && !beat.IsSettled; frames++)
            {
                beat = beat.Advanced(Frame);
                moving++;

                Assert.That(beat.IsGripping, Is.False, "The lockout came back on during the return.");
            }

            Assert.That(moving, Is.GreaterThan(12), "The camera stopped moving when the lockout lifted.");
        }

        [Test]
        public void ThePunchPullsOntoWhatItFiredOnAndHandsTheFramingBackWhole()
        {
            var elsewhere = LevelFraming.Play(new WorldPoint(9f, 0f, 9f));
            var beat = ZoomBeat.On(Anchor).Advanced(ZoomBeat.InSeconds);

            Assert.That(beat.Over(elsewhere).Target, Is.EqualTo(Anchor));

            beat = beat.Released();
            for (var frames = 0; frames < 60 && !beat.IsSettled; frames++)
            {
                beat = beat.Advanced(Frame);
            }

            Assert.That(beat.Over(elsewhere), Is.EqualTo(elsewhere));
        }

        [Test]
        public void ABeatOnlyEverRunsForwards()
        {
            Assert.That(
                () => ZoomBeat.On(Anchor).Advanced(-0.1f),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
