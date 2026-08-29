using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public sealed class BackdropTests
    {
        [Test]
        public void TheRampRunsLighterUpTheFrame()
        {
            for (var band = 1; band < Backdrop.RampBands; band++)
            {
                var under = Backdrop.At(Backdrop.BandHeight(band - 1)).Luminance;
                var over = Backdrop.At(Backdrop.BandHeight(band)).Luminance;

                Assert.That(over, Is.GreaterThan(under), "band " + band);
            }

            Assert.That(
                Backdrop.Above.Luminance - Backdrop.Below.Luminance,
                Is.GreaterThan(0.02f),
                "the grade is too slight to read as a grade");
            Assert.That(Backdrop.At(0f), Is.EqualTo(Backdrop.Below));
            Assert.That(Backdrop.At(1f), Is.EqualTo(Backdrop.Above));
        }

        [Test]
        public void TheClearColourSitsInsideTheRamp()
        {
            Assert.That(Backdrop.Clear.Luminance, Is.GreaterThan(Backdrop.Below.Luminance));
            Assert.That(Backdrop.Clear.Luminance, Is.LessThan(Backdrop.Above.Luminance));
        }

        [Test]
        public void EverySurfaceStandsClearOfTheBackdropSoATerraceEdgeCuts()
        {
            foreach (var style in On(PartLayer.Surface))
            {
                var apart = Backdrop.SeparationFrom(WorldTints.Of(style));

                Assert.That(
                    apart,
                    Is.GreaterThanOrEqualTo(Backdrop.LeastSurfaceSeparation),
                    style + " stands " + apart.ToString("0.###") + ":1 off the backdrop");
            }
        }

        [Test]
        public void TheDarkestFigureStandsClearOfTheBackdropSoNoSilhouetteReadsAsAHole()
        {
            var darkest = PartStyle.Floor;

            foreach (var style in On(PartLayer.Figure))
            {
                if (WorldTints.Of(style).Luminance < WorldTints.Of(darkest).Luminance)
                {
                    darkest = style;
                }
            }

            Assert.That(
                Backdrop.SeparationFrom(WorldTints.Of(darkest)),
                Is.GreaterThanOrEqualTo(Backdrop.LeastFigureSeparation),
                darkest + " is the darkest figure and stands "
                + Backdrop.SeparationFrom(WorldTints.Of(darkest)).ToString("0.###") + ":1 off the backdrop");
            Assert.That(
                WorldTints.Of(darkest).Luminance,
                Is.LessThan(Backdrop.Below.Luminance),
                "the backdrop is darker than the darkest thing that can stand against it");
        }

        [Test]
        public void TheWholeBackdropIsDarkerThanEverySurfaceSoTheDungeonReadsAsLitAgainstIt()
        {
            foreach (var surface in On(PartLayer.Surface))
            {
                Assert.That(
                    WorldTints.Of(surface).Luminance,
                    Is.GreaterThan(Backdrop.Above.Luminance),
                    surface + " against the backdrop's lightest band");
            }
        }

        [Test]
        public void TheBackdropIsNoStretchOfGrey()
        {
            Assert.That(Backdrop.Below.Chroma, Is.GreaterThan(0.1f));
            Assert.That(Backdrop.Above.Chroma, Is.GreaterThan(0.1f));
            Assert.That(
                Tint.HueApart(Backdrop.Below, Backdrop.Above),
                Is.LessThanOrEqualTo(Backdrop.SharedAmbientHue),
                "the grade shifts hue rather than depth");
        }

        [Test]
        public void TheBackdropTakesTheHueTheWallsDoNotSoTheWorldReadsWarmAgainstIt()
        {
            Assert.That(
                Tint.HueApart(Backdrop.Clear, WorldTints.Of(PartStyle.Floor)),
                Is.GreaterThan(90f),
                "the backdrop sits at " + Backdrop.Clear.Hue.ToString("0.#")
                + " degrees and the floor at " + WorldTints.Of(PartStyle.Floor).Hue.ToString("0.#"));
        }

        [Test]
        public void MarksAreNeverHeldAgainstTheBackdropBecauseTheyLieOnTheFloor()
        {
            Assert.That(
                () => Backdrop.LeastSeparationFor(PartLayer.Mark),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                Backdrop.LeastSeparationFor(PartLayer.Surface),
                Is.EqualTo(Backdrop.LeastSurfaceSeparation));
            Assert.That(
                Backdrop.LeastSeparationFor(PartLayer.Figure),
                Is.EqualTo(Backdrop.LeastFigureSeparation));
        }

        [Test]
        public void AmbientLightFallsFromSkyToGroundRatherThanWashingFlat()
        {
            Assert.That(
                Backdrop.AmbientSky.Luminance,
                Is.GreaterThan(Backdrop.AmbientEquator.Luminance));
            Assert.That(
                Backdrop.AmbientEquator.Luminance,
                Is.GreaterThan(Backdrop.AmbientGround.Luminance));
            Assert.That(
                Tint.Contrast(Backdrop.AmbientSky, Backdrop.AmbientGround),
                Is.GreaterThanOrEqualTo(Backdrop.LeastAmbientTilt),
                "sky and ground ambient stand "
                + Tint.Contrast(Backdrop.AmbientSky, Backdrop.AmbientGround).ToString("0.###") + ":1 apart");
        }

        [Test]
        public void AmbientLightStaysUnderTheBudgetTheWorldWasTunedOn()
        {
            Assert.That(
                Backdrop.AmbientLoad,
                Is.LessThanOrEqualTo(Backdrop.AmbientBudget.Luminance),
                "ambient carries " + Backdrop.AmbientLoad.ToString("0.####")
                + " against the " + Backdrop.AmbientBudget.Luminance.ToString("0.####") + " budget");
            Assert.That(
                Backdrop.AmbientLoad,
                Is.GreaterThanOrEqualTo(Backdrop.AmbientBudget.Luminance * 0.5f),
                "ambient this thin leaves every unlit face crushed");
        }

        [Test]
        public void AmbientLightIsTheRoomTheDungeonStandsIn()
        {
            Assert.That(
                Tint.HueApart(Backdrop.AmbientSky, Backdrop.Above),
                Is.LessThanOrEqualTo(Backdrop.SharedAmbientHue),
                "the sky ambient sits at " + Backdrop.AmbientSky.Hue.ToString("0.#")
                + " degrees against the backdrop's " + Backdrop.Above.Hue.ToString("0.#"));
            Assert.That(
                Tint.HueApart(Backdrop.AmbientGround, WorldTints.Of(PartStyle.Floor)),
                Is.LessThanOrEqualTo(Backdrop.SharedAmbientHue),
                "the ground ambient sits at " + Backdrop.AmbientGround.Hue.ToString("0.#")
                + " degrees against the floor's " + WorldTints.Of(PartStyle.Floor).Hue.ToString("0.#"));
        }

        [Test]
        public void NothingIsReflectedOffASkyThatIsNoLongerThere()
        {
            Assert.That(Backdrop.ReflectionStrength, Is.EqualTo(0f));
        }

        [Test]
        public void TheSheetHangsShortOfTheFarPlaneAndCoversAWideFrame()
        {
            Assert.That(Backdrop.Reach, Is.LessThan(IsoProjection.FarPlane));
            Assert.That(Backdrop.Reach, Is.GreaterThan(IsoProjection.CameraBack));
            Assert.That(Backdrop.WidthOverHeight, Is.GreaterThanOrEqualTo(2f));
            Assert.That(Backdrop.Overscan, Is.GreaterThan(1f));
            Assert.That(Backdrop.RampBands, Is.GreaterThanOrEqualTo(16));
        }

        static IReadOnlyList<PartStyle> On(PartLayer layer)
        {
            var styles = new List<PartStyle>();

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (WorldTints.LayerOf(style) == layer)
                {
                    styles.Add(style);
                }
            }

            return styles;
        }
    }
}
