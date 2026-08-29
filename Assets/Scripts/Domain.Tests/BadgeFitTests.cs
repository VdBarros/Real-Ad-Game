using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class BadgeFitTests
    {
        const float Roomy = 4f;

        const float Tolerance = 1e-5f;

        [Test]
        public void ABadgeIsAsWideAsTheDigitsItShows()
        {
            for (var cells = 1; cells <= 6; cells++)
            {
                var size = BadgeFit.Of(cells, Roomy);

                Assert.That(size.Scale, Is.EqualTo(1f).Within(Tolerance), cells.ToString());
                Assert.That(
                    size.Width,
                    Is.EqualTo(cells * BadgeMetrics.CellWidth + 2f * BadgeMetrics.SidePadding).Within(Tolerance),
                    cells.ToString());
            }
        }

        [Test]
        public void ANumberWithMoreDigitsWearsAWiderBadge()
        {
            var widths = new List<float>();
            for (var power = 1; power <= 10000; power *= 10)
            {
                widths.Add(BadgeFit.Of(BadgeText.Digits(power), Roomy).Width);
            }

            Assert.That(widths, Is.Ordered.Ascending);
        }

        [Test]
        public void NoBadgeIsWiderThanTheThingItLabels()
        {
            foreach (var subject in new[] { 0.05f, 0.2f, 0.4f, 0.44f, 0.5f, 1f, 3f })
            {
                for (var cells = 1; cells <= 6; cells++)
                {
                    var size = BadgeFit.Of(cells, subject);

                    Assert.That(
                        size.Width,
                        Is.LessThanOrEqualTo(subject + Tolerance),
                        cells + " cells over " + subject);
                }
            }
        }

        [Test]
        public void ABadgeNarrowerThanItsSubjectIsLeftAlone()
        {
            var size = BadgeFit.Of(2f, Roomy);

            Assert.That(size.Scale, Is.EqualTo(1f));
            Assert.That(size.Height, Is.EqualTo(BadgeMetrics.Height));
            Assert.That(size.FontSize, Is.EqualTo(BadgeMetrics.FontSize));
        }

        [Test]
        public void AWideNumberOnANarrowCharacterShrinksWholeRatherThanCroppingItsText()
        {
            var size = BadgeFit.Of(4f, 0.5f);

            Assert.That(size.Width, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(size.Cells, Is.EqualTo(4f));
            Assert.That(size.Scale, Is.EqualTo(0.5f / BadgeMetrics.WidthFor(4f)).Within(Tolerance));
            Assert.That(size.Height, Is.EqualTo(BadgeMetrics.Height * size.Scale).Within(Tolerance));
            Assert.That(size.FontSize, Is.EqualTo(BadgeMetrics.FontSize * size.Scale).Within(Tolerance));
            Assert.That(
                size.Width / size.Scale,
                Is.EqualTo(BadgeMetrics.WidthFor(4f)).Within(Tolerance));
        }

        [Test]
        public void TheMinimumClampKeepsASingleDigitLegibleNoMatterHowNarrowTheSubjectIs()
        {
            foreach (var subject in new[] { 0.01f, 0.1f, 0.3f, 0.43f })
            {
                var size = BadgeFit.Of(0.4f, subject);

                Assert.That(size.Cells, Is.EqualTo(BadgeMetrics.MinimumCells), subject.ToString());
                Assert.That(
                    size.Width / size.Scale,
                    Is.EqualTo(BadgeMetrics.MinimumWidth).Within(Tolerance),
                    subject.ToString());
                Assert.That(size.FontSize, Is.GreaterThan(0f), subject.ToString());
                Assert.That(
                    size.FontSize / size.Width,
                    Is.EqualTo(BadgeMetrics.FontSize / BadgeMetrics.MinimumWidth).Within(Tolerance),
                    subject.ToString());
            }
        }

        [Test]
        public void TheOneDigitPlateIsNeverNarrowerThanOneGlyphPlusItsPadding()
        {
            Assert.That(
                BadgeMetrics.MinimumWidth,
                Is.EqualTo(BadgeMetrics.CellWidth + 2f * BadgeMetrics.SidePadding).Within(Tolerance));
            Assert.That(BadgeFit.Of(1f, 10f).Width, Is.EqualTo(BadgeMetrics.MinimumWidth).Within(Tolerance));
        }

        [Test]
        public void AFractionalGlyphCountWidensSmoothlyBetweenTwoWholeOnes()
        {
            var narrow = BadgeFit.Of(1f, Roomy).Width;
            var wide = BadgeFit.Of(2f, Roomy).Width;
            var halfway = BadgeFit.Of(1.5f, Roomy).Width;

            Assert.That(halfway, Is.EqualTo((narrow + wide) * 0.5f).Within(Tolerance));
        }

        [Test]
        public void ABadgeRefusesToLabelSomethingWithoutAWidth()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BadgeFit.Of(1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BadgeFit.Of(1f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BadgeFit.Of(-1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BadgeFit.Of(float.PositiveInfinity, 1f));
        }
    }
}
