using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class ScreenProjectionTests
    {
        const int Width = ScreenFrame.Width;

        const int Height = ScreenFrame.Height;

        const float Tolerance = 0.01f;

        static CameraFraming Framing()
        {
            return new CameraFraming(new WorldPoint(3f, 2f, 5f), LevelFraming.PlaySize);
        }

        [Test]
        public void TheFramingTargetLandsInTheMiddleOfTheScreen()
        {
            var framing = Framing();

            var point = ScreenProjection.Of(framing, framing.Target, Width, Height);

            Assert.That(point.X, Is.EqualTo(Width * 0.5f).Within(Tolerance));
            Assert.That(point.Y, Is.EqualTo(Height * 0.5f).Within(Tolerance));
        }

        [Test]
        public void AMetreAlongTheCameraRightMovesAcrossTheScreenAndNotUpIt()
        {
            var framing = Framing();
            var right = IsoProjection.CameraRight;
            var pixels = ScreenProjection.PixelsPerMetre(framing.OrthographicSize, Height);

            var point = ScreenProjection.Of(
                framing,
                new WorldPoint(
                    framing.Target.X + right.X, framing.Target.Y + right.Y, framing.Target.Z + right.Z),
                Width,
                Height);

            Assert.That(point.X, Is.EqualTo(Width * 0.5f + pixels).Within(Tolerance));
            Assert.That(point.Y, Is.EqualTo(Height * 0.5f).Within(Tolerance));
        }

        [Test]
        public void AMetreAlongTheCameraUpMovesUpTheScreenAndNotAcrossIt()
        {
            var framing = Framing();
            var up = IsoProjection.CameraUp;
            var pixels = ScreenProjection.PixelsPerMetre(framing.OrthographicSize, Height);

            var point = ScreenProjection.Of(
                framing,
                new WorldPoint(framing.Target.X + up.X, framing.Target.Y + up.Y, framing.Target.Z + up.Z),
                Width,
                Height);

            Assert.That(point.X, Is.EqualTo(Width * 0.5f).Within(Tolerance));
            Assert.That(point.Y, Is.EqualTo(Height * 0.5f + pixels).Within(Tolerance));
        }

        [Test]
        public void DepthDoesNotMoveAPointBecauseTheLensIsOrthographic()
        {
            var framing = Framing();
            var forward = IsoProjection.CameraForward;
            var target = framing.Target;
            var behind = new WorldPoint(
                target.X + forward.X * 4f, target.Y + forward.Y * 4f, target.Z + forward.Z * 4f);

            var point = ScreenProjection.Of(framing, behind, Width, Height);

            Assert.That(point.X, Is.EqualTo(Width * 0.5f).Within(Tolerance));
            Assert.That(point.Y, Is.EqualTo(Height * 0.5f).Within(Tolerance));
        }

        [Test]
        public void ZoomingInDoublesThePixelsAMetreBuys()
        {
            var wide = ScreenProjection.PixelsPerMetre(LevelFraming.PlaySize, Height);
            var close = ScreenProjection.PixelsPerMetre(LevelFraming.PlaySize * 0.5f, Height);

            Assert.That(close, Is.EqualTo(wide * 2f).Within(Tolerance));
        }

        [Test]
        public void AShorterScreenBuysFewerPixelsAMetre()
        {
            var tall = ScreenProjection.PixelsPerMetre(LevelFraming.PlaySize, Height);
            var half = ScreenProjection.PixelsPerMetre(LevelFraming.PlaySize, Height / 2);

            Assert.That(half, Is.EqualTo(tall * 0.5f).Within(Tolerance));
        }

        [Test]
        public void TheSweptFramingAgreesWithTheConstantOneTheRigNeverLeaves()
        {
            Assert.That(
                ScreenProjection.PixelsPerMetre(LevelFraming.PlaySize, Height),
                Is.EqualTo(ScreenFrame.PixelsPerMetre(LevelFraming.PlaySize)).Within(Tolerance));
        }
    }
}
