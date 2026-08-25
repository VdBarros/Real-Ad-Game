using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public class BadgeShapeFieldTests
    {
        [Test]
        public void EveryShapeIsSolidThroughItsMiddle()
        {
            foreach (var shape in new[] { BadgeShape.RoundedRect, BadgeShape.Pill })
            {
                var centre = BadgeShapeField.CellPixels / 2;
                Assert.That(BadgeShapeField.Coverage(shape, centre, centre), Is.EqualTo(1f), shape.ToString());
                Assert.That(BadgeShapeField.Coverage(shape, 0, centre), Is.EqualTo(1f), shape.ToString());
            }
        }

        [Test]
        public void EveryShapeIsEmptyInItsCorners()
        {
            var last = BadgeShapeField.CellPixels - 1;

            foreach (var shape in new[] { BadgeShape.RoundedRect, BadgeShape.Pill })
            {
                Assert.That(BadgeShapeField.Coverage(shape, 0, 0), Is.EqualTo(0f), shape.ToString());
                Assert.That(BadgeShapeField.Coverage(shape, last, 0), Is.EqualTo(0f), shape.ToString());
                Assert.That(BadgeShapeField.Coverage(shape, 0, last), Is.EqualTo(0f), shape.ToString());
                Assert.That(BadgeShapeField.Coverage(shape, last, last), Is.EqualTo(0f), shape.ToString());
            }
        }

        [Test]
        public void APillIsRounderThanARoundedRect()
        {
            var eighth = BadgeShapeField.CellPixels / 8;

            Assert.That(BadgeShapeField.Coverage(BadgeShape.RoundedRect, eighth, eighth), Is.EqualTo(1f));
            Assert.That(BadgeShapeField.Coverage(BadgeShape.Pill, eighth, eighth), Is.EqualTo(0f));
            Assert.That(BadgeShapeField.PillRadius, Is.GreaterThan(BadgeShapeField.RoundedRectRadius));
        }

        [Test]
        public void CoverageStaysBetweenNothingAndEverything()
        {
            foreach (var shape in new[] { BadgeShape.RoundedRect, BadgeShape.Pill })
            {
                for (var y = 0; y < BadgeShapeField.CellPixels; y++)
                {
                    for (var x = 0; x < BadgeShapeField.CellPixels; x++)
                    {
                        var coverage = BadgeShapeField.Coverage(shape, x, y);
                        Assert.That(coverage, Is.InRange(0f, 1f));
                    }
                }
            }
        }

        [Test]
        public void TheNineSliceBordersLeaveAStretchableMiddle()
        {
            foreach (var shape in new[] { BadgeShape.RoundedRect, BadgeShape.Pill })
            {
                Assert.That(BadgeShapeField.BorderOf(shape) * 2, Is.LessThan(BadgeShapeField.CellPixels));
            }
        }

        [Test]
        public void TheTwoCellsDoNotTouch()
        {
            Assert.That(
                BadgeShapeField.OriginX(BadgeShape.Pill),
                Is.GreaterThanOrEqualTo(BadgeShapeField.CellPixels + BadgeShapeField.GutterPixels));
            Assert.That(
                BadgeShapeField.OriginX(BadgeShape.Pill) + BadgeShapeField.CellPixels,
                Is.LessThanOrEqualTo(BadgeShapeField.TextureWidth));
        }
    }
}
