using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class RecentreCallTests
    {
        static ScreenPoint Middle(int frameWidth, int frameHeight)
        {
            var scale = frameHeight / (float)ScreenFrame.Height;

            return new ScreenPoint(
                frameWidth * 0.5f, (RecentreCall.Lift + RecentreCall.Height * 0.5f) * scale);
        }

        [Test]
        public void TheCallShowsWhilePlayWithTheCameraOffThePlayer()
        {
            Assert.That(RecentreCall.Showing(GamePhase.Play, true, false), Is.True);
        }

        [Test]
        public void TheCallIsGoneWhileTheCameraSitsOnThePlayer()
        {
            Assert.That(RecentreCall.Showing(GamePhase.Play, false, false), Is.False);
        }

        [Test]
        public void TheCallIsGoneWhileTheFlightOrTheBeatOwnsTheCamera()
        {
            Assert.That(RecentreCall.Showing(GamePhase.Play, true, true), Is.False);
        }

        [Test]
        public void NoPhaseButPlayOffersTheCall()
        {
            foreach (GamePhase phase in Enum.GetValues(typeof(GamePhase)))
            {
                if (phase == GamePhase.Play)
                {
                    continue;
                }

                Assert.That(
                    RecentreCall.Showing(phase, true, false),
                    Is.False,
                    "The call showed in " + phase + ", which owns the screen itself.");
            }
        }

        [Test]
        public void TheCutsceneTheFlyThroughAndTheResultAllHoldTheCallBack()
        {
            Assert.That(RecentreCall.Showing(GamePhase.Cutscene, true, false), Is.False);
            Assert.That(RecentreCall.Showing(GamePhase.Preview, true, false), Is.False);
            Assert.That(RecentreCall.Showing(GamePhase.Result, true, false), Is.False);
        }

        [Test]
        public void ThePatchTakesAPressAtItsMiddle()
        {
            Assert.That(
                RecentreCall.Holds(
                    ScreenFrame.Width, ScreenFrame.Height, Middle(ScreenFrame.Width, ScreenFrame.Height)),
                Is.True);
        }

        [Test]
        public void ThePatchSitsBelowTheMazeAndLeavesTheRestOfTheFrameAlone()
        {
            var maze = new ScreenPoint(ScreenFrame.Width * 0.5f, ScreenFrame.Height * 0.5f);
            var corner = new ScreenPoint(4f, 4f);
            var top = new ScreenPoint(ScreenFrame.Width * 0.5f, ScreenFrame.Height - 4f);

            Assert.That(RecentreCall.Holds(ScreenFrame.Width, ScreenFrame.Height, maze), Is.False);
            Assert.That(RecentreCall.Holds(ScreenFrame.Width, ScreenFrame.Height, corner), Is.False);
            Assert.That(RecentreCall.Holds(ScreenFrame.Width, ScreenFrame.Height, top), Is.False);
        }

        [Test]
        public void ThePatchStopsWhereTheButtonStops()
        {
            var justOutside = new ScreenPoint(
                ScreenFrame.Width * 0.5f + RecentreCall.Width * 0.5f + 2f,
                RecentreCall.Lift + RecentreCall.Height * 0.5f);
            var justInside = new ScreenPoint(
                ScreenFrame.Width * 0.5f + RecentreCall.Width * 0.5f - 2f,
                RecentreCall.Lift + RecentreCall.Height * 0.5f);

            Assert.That(RecentreCall.Holds(ScreenFrame.Width, ScreenFrame.Height, justOutside), Is.False);
            Assert.That(RecentreCall.Holds(ScreenFrame.Width, ScreenFrame.Height, justInside), Is.True);
        }

        [Test]
        public void ThePatchRidesTheHeightTheCanvasScalesBy()
        {
            var width = ScreenFrame.Width / 2;
            var height = ScreenFrame.Height / 2;

            Assert.That(RecentreCall.Holds(width, height, Middle(width, height)), Is.True);
            Assert.That(
                RecentreCall.Holds(
                    width,
                    height,
                    new ScreenPoint(width * 0.5f, (RecentreCall.Lift + RecentreCall.Height) * 0.75f + 4f)),
                Is.False);
        }

        [Test]
        public void ThePatchIsAlwaysClearOfTheBottomAndOfTheMiddleOfTheFrame()
        {
            Assert.That(RecentreCall.Lift, Is.GreaterThan(0f));
            Assert.That(
                RecentreCall.Lift + RecentreCall.Height, Is.LessThan(ScreenFrame.Height * 0.5f));
            Assert.That(RecentreCall.Width, Is.LessThan(ScreenFrame.Width));
        }

        [Test]
        public void AFrameWithNoPixelsHasNowhereToPutTheCall()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RecentreCall.Holds(0, ScreenFrame.Height, new ScreenPoint(0f, 0f)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RecentreCall.Holds(ScreenFrame.Width, 0, new ScreenPoint(0f, 0f)));
        }
    }
}
