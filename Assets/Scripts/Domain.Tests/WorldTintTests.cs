using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public sealed class WorldTintTests
    {
        [Test]
        public void EveryStyleIsGivenATintAndALayer()
        {
            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                Assert.That(() => WorldTints.Of(style), Throws.Nothing, style.ToString());
                Assert.That(() => WorldTints.LayerOf(style), Throws.Nothing, style.ToString());
            }
        }

        [Test]
        public void EveryFigureClearsTheLeastSeparationFromEverySurface()
        {
            var worst = float.MaxValue;
            var pairing = string.Empty;

            foreach (var figure in On(PartLayer.Figure))
            {
                foreach (var surface in On(PartLayer.Surface))
                {
                    var apart = Tint.Contrast(WorldTints.Of(figure), WorldTints.Of(surface));

                    if (apart >= worst)
                    {
                        continue;
                    }

                    worst = apart;
                    pairing = figure + " against " + surface;
                }
            }

            Assert.That(
                worst,
                Is.GreaterThanOrEqualTo(WorldTints.LeastSeparation),
                "the closest pairing is " + pairing + " at " + worst.ToString("0.###") + ":1");
        }

        [Test]
        public void EveryFigureIsDarkerThanEverySurfaceSoTheFrameReadsInGrey()
        {
            foreach (var figure in On(PartLayer.Figure))
            {
                foreach (var surface in On(PartLayer.Surface))
                {
                    Assert.That(
                        WorldTints.Of(figure).Luminance,
                        Is.LessThan(WorldTints.Of(surface).Luminance),
                        figure + " against " + surface);
                }
            }
        }

        [Test]
        public void TheClearedFloorKeepsTheHueOfTheCursedOne()
        {
            var cursed = WorldTints.Of(PartStyle.Floor);
            var cleared = WorldTints.Of(PartStyle.Cleared);

            Assert.That(cursed.Chroma, Is.GreaterThan(0f));
            Assert.That(cleared.Chroma, Is.GreaterThan(0f));
            Assert.That(
                Tint.HueApart(cursed, cleared),
                Is.LessThanOrEqualTo(WorldTints.SharedFloorHue),
                "cursed sits at " + cursed.Hue.ToString("0.##")
                + " degrees and cleared at " + cleared.Hue.ToString("0.##"));
        }

        [Test]
        public void TheClearedFloorSignalIsCarriedByChromaAndNotByValue()
        {
            var cursed = WorldTints.Of(PartStyle.Floor);
            var cleared = WorldTints.Of(PartStyle.Cleared);

            Assert.That(
                cleared.Chroma,
                Is.GreaterThanOrEqualTo(cursed.Chroma * WorldTints.LeastClearedChromaLift),
                "cursed carries " + cursed.Chroma.ToString("0.###")
                + " of chroma and cleared " + cleared.Chroma.ToString("0.###"));

            Assert.That(
                Tint.Contrast(cursed, cleared),
                Is.LessThanOrEqualTo(WorldTints.MostClearedValueShift),
                "the two floors stand " + Tint.Contrast(cursed, cleared).ToString("0.###")
                + ":1 apart in value, which is a grey shift rather than a saturation one");
        }

        [Test]
        public void NeitherFloorIsDarkEnoughToSwallowAFigure()
        {
            foreach (var figure in On(PartLayer.Figure))
            {
                Assert.That(
                    Tint.Contrast(WorldTints.Of(figure), WorldTints.Of(PartStyle.Floor)),
                    Is.GreaterThanOrEqualTo(WorldTints.LeastSeparation),
                    figure + " over a cursed floor");
                Assert.That(
                    Tint.Contrast(WorldTints.Of(figure), WorldTints.Of(PartStyle.Cleared)),
                    Is.GreaterThanOrEqualTo(WorldTints.LeastSeparation),
                    figure + " over a cleared floor");
            }
        }

        [Test]
        public void EveryStyleKeepsTheHueItsIdentityIsToldBy()
        {
            var hues = new Dictionary<PartStyle, float>
            {
                { PartStyle.Start, 136f },
                { PartStyle.Enemy, 0f },
                { PartStyle.Boss, 355f },
                { PartStyle.Additive, 218f },
                { PartStyle.Multiplier, 191f },
                { PartStyle.Pillar, 37f }
            };

            foreach (var pair in hues)
            {
                Assert.That(WorldTints.Of(pair.Key).Chroma, Is.GreaterThan(0.1f), pair.Key.ToString());
                Assert.That(
                    Apart(WorldTints.Of(pair.Key).Hue, pair.Value),
                    Is.LessThanOrEqualTo(12f),
                    pair.Key + " sits at " + WorldTints.Of(pair.Key).Hue.ToString("0.#")
                    + " degrees against the " + pair.Value.ToString("0.#") + " its identity is told by");
            }
        }

        [Test]
        public void LuminanceRunsFromBlackToWhite()
        {
            Assert.That(new Tint(0f, 0f, 0f).Luminance, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(new Tint(1f, 1f, 1f).Luminance, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                new Tint(0.2f, 0.2f, 0.2f).Luminance,
                Is.LessThan(new Tint(0.8f, 0.8f, 0.8f).Luminance));
            Assert.That(
                Tint.Contrast(new Tint(0f, 0f, 0f), new Tint(1f, 1f, 1f)),
                Is.EqualTo(21f).Within(0.01f));
        }

        [Test]
        public void ChromaAndHueReadTheColourWheel()
        {
            Assert.That(new Tint(0.5f, 0.5f, 0.5f).Chroma, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(new Tint(1f, 0f, 0f).Hue, Is.EqualTo(0f).Within(0.01f));
            Assert.That(new Tint(0f, 1f, 0f).Hue, Is.EqualTo(120f).Within(0.01f));
            Assert.That(new Tint(0f, 0f, 1f).Hue, Is.EqualTo(240f).Within(0.01f));
            Assert.That(
                Tint.HueApart(new Tint(1f, 0f, 0f), new Tint(1f, 0f, 0.5f)),
                Is.EqualTo(30f).Within(0.01f));
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

        static float Apart(float one, float other)
        {
            var apart = one - other;

            if (apart < 0f)
            {
                apart = -apart;
            }

            return apart > 180f ? 360f - apart : apart;
        }
    }
}
