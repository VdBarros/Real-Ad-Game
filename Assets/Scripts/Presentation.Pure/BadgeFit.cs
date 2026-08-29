using System;

namespace Game.Presentation.Pure
{
    public static class BadgeFit
    {
        public static BadgeSize Of(float cells, float subjectWidth)
        {
            if (!(cells >= 0f) || float.IsInfinity(cells))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cells), cells, "A badge shows a countable number of glyphs.");
            }

            if (!(subjectWidth > 0f) || float.IsInfinity(subjectWidth))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(subjectWidth), subjectWidth, "A badge labels something that has a width.");
            }

            var legible = cells > BadgeMetrics.MinimumCells ? cells : BadgeMetrics.MinimumCells;
            var plate = BadgeMetrics.WidthFor(legible);

            return new BadgeSize(legible, subjectWidth < plate ? subjectWidth / plate : 1f);
        }
    }
}
