using System;

namespace Game.Presentation.Pure
{
    public static class BadgeShapeField
    {
        public const int CellPixels = 64;

        public const int GutterPixels = 2;

        public const int TextureWidth = CellPixels * 2 + GutterPixels;

        public const int TextureHeight = CellPixels;

        public const int PixelsPerUnit = CellPixels;

        public const float RoundedRectRadius = 18f;

        public const float PillRadius = 31f;

        public static int OriginX(BadgeShape shape)
        {
            switch (shape)
            {
                case BadgeShape.RoundedRect:
                    return 0;
                case BadgeShape.Pill:
                    return CellPixels + GutterPixels;
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

            var radius = RadiusOf(shape);
            var half = CellPixels * 0.5f;
            var offsetX = Math.Abs(x + 0.5f - half) - (half - radius);
            var offsetY = Math.Abs(y + 0.5f - half) - (half - radius);
            var outsideX = offsetX > 0f ? offsetX : 0f;
            var outsideY = offsetY > 0f ? offsetY : 0f;
            var inside = Math.Max(offsetX, offsetY);
            var distance = (float)Math.Sqrt(outsideX * outsideX + outsideY * outsideY)
                + (inside < 0f ? inside : 0f)
                - radius;

            var coverage = 0.5f - distance;
            return coverage < 0f ? 0f : coverage > 1f ? 1f : coverage;
        }
    }
}
