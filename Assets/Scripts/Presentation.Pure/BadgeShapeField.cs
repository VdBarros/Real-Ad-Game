using System;

namespace Game.Presentation.Pure
{
    public static class BadgeShapeField
    {
        public const int CellPixels = 64;

        public const int GutterPixels = 2;

        public const int Cells = 3;

        public const int TextureWidth = CellPixels * Cells + GutterPixels * (Cells - 1);

        public const int TextureHeight = CellPixels;

        public const float PixelsPerUnit = CellPixels / BadgeMetrics.Height;

        public const float RoundedRectRadius = 18f;

        public const float PillRadius = 31f;

        public const float TagChamfer = 28f;

        static readonly float DiagonalFall = (float)(1.0 / Math.Sqrt(2.0));

        public static int OriginX(BadgeShape shape)
        {
            switch (shape)
            {
                case BadgeShape.RoundedRect:
                    return 0;
                case BadgeShape.Pill:
                    return CellPixels + GutterPixels;
                case BadgeShape.Tag:
                    return 2 * (CellPixels + GutterPixels);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, "No texture cell for that shape.");
            }
        }

        public static float RadiusOf(BadgeShape shape)
        {
            switch (shape)
            {
                case BadgeShape.RoundedRect:
                    return RoundedRectRadius;
                case BadgeShape.Pill:
                    return PillRadius;
                case BadgeShape.Tag:
                    return TagChamfer;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, "No corner radius for that shape.");
            }
        }

        public static int BorderOf(BadgeShape shape)
        {
            return (int)Math.Ceiling(RadiusOf(shape));
        }

        public static float Coverage(BadgeShape shape, int x, int y)
        {
            if (x < 0 || x >= CellPixels || y < 0 || y >= CellPixels)
            {
                throw new ArgumentOutOfRangeException(
                    x < 0 || x >= CellPixels ? nameof(x) : nameof(y),
                    x < 0 || x >= CellPixels ? x : y,
                    "A badge shape is sampled inside its own cell.");
            }

            var coverage = 0.5f - DistanceOutside(shape, x, y);
            return coverage < 0f ? 0f : coverage > 1f ? 1f : coverage;
        }

        static float DistanceOutside(BadgeShape shape, int x, int y)
        {
            var half = CellPixels * 0.5f;
            var fromMiddleX = Math.Abs(x + 0.5f - half);
            var fromMiddleY = Math.Abs(y + 0.5f - half);

            if (shape == BadgeShape.Tag)
            {
                var straight = Math.Max(fromMiddleX - half, fromMiddleY - half);
                var cut = (fromMiddleX + fromMiddleY - (2f * half - TagChamfer)) * DiagonalFall;

                return Math.Max(straight, cut);
            }

            var radius = RadiusOf(shape);
            var offsetX = fromMiddleX - (half - radius);
            var offsetY = fromMiddleY - (half - radius);
            var outsideX = offsetX > 0f ? offsetX : 0f;
            var outsideY = offsetY > 0f ? offsetY : 0f;
            var inside = Math.Max(offsetX, offsetY);

            return (float)Math.Sqrt(outsideX * outsideX + outsideY * outsideY)
                + (inside < 0f ? inside : 0f)
                - radius;
        }
    }
}
