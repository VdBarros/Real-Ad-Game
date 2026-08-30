using System;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public sealed class FigureRimTests
    {
        [Test]
        public void TheContourIsAsWideAsThePixelsItPromisesAtThePlayFraming()
        {
            Assert.That(
                FigureRim.PixelsAt(LevelFraming.PlaySize),
                Is.EqualTo(FigureRim.ContourPixels).Within(1e-4f),
                "the contour is " + FigureRim.Width.ToString("0.#####")
                + " world units wide at the " + LevelFraming.PlaySize.ToString("0.##") + " play framing");
        }

        [Test]
        public void TheContourHoldsItsWorldWidthWhenTheCameraPunchesIn()
        {
            Assert.That(
                FigureRim.PixelsAt(LevelFraming.CloseUpSize),
                Is.GreaterThan(FigureRim.PixelsAt(LevelFraming.PlaySize)),
                "a contour of a fixed world width grows on screen as the camera closes, never shrinks");
        }

        [Test]
        public void TheContourIsThinnerThanTheFigureItOutlines()
        {
            Assert.That(
                FigureRim.ContourPixels,
                Is.LessThan(FigureReadability.ReadablePixels * 0.1f),
                "a contour of " + FigureRim.ContourPixels + " pixels against the "
                + FigureReadability.ReadablePixels + " a figure is guaranteed to stand");
        }

        [Test]
        public void TheContourReadsAgainstEverySurfaceTheWorldPaints()
        {
            var worst = float.MaxValue;
            var surface = string.Empty;

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (WorldTints.LayerOf(style) != PartLayer.Surface)
                {
                    continue;
                }

                var apart = FigureRim.SeparationFrom(WorldTints.Of(style));

                if (apart >= worst)
                {
                    continue;
                }

                worst = apart;
                surface = style.ToString();
            }

            Assert.That(
                worst,
                Is.GreaterThanOrEqualTo(FigureRim.LeastSeparation),
                "the closest surface is " + surface + " at " + worst.ToString("0.###") + ":1");
        }

        [Test]
        public void TheContourReadsAgainstTheBackdropTheDungeonHangsIn()
        {
            Assert.That(
                Backdrop.SeparationFrom(FigureRim.Contour),
                Is.GreaterThanOrEqualTo(Backdrop.LeastFigureSeparation),
                "the contour stands "
                + Backdrop.SeparationFrom(FigureRim.Contour).ToString("0.###")
                + ":1 from the nearer end of the backdrop ramp");
        }

        [Test]
        public void OnlyTheCastIsContoured()
        {
            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                Assert.That(
                    FigureRim.Contours(style),
                    Is.EqualTo(CharacterCast.IsRole(style)),
                    style.ToString());
            }
        }

        [Test]
        public void AFramingWithoutASizeIsRefused()
        {
            Assert.That(() => FigureRim.PixelsAt(0f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
