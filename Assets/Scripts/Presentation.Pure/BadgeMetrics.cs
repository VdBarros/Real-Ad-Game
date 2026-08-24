using System;

namespace Game.Presentation.Pure
{
    public static class BadgeMetrics
    {
        public const float CellWidth = 0.26f;

        public const float SidePadding = 0.09f;

        public const float Height = 0.4f;

        public const float VerticalPadding = 0.06f;

        public const float MonospaceEm = 0.62f;

        public const float CapHeightEm = 0.72f;

        public const float UnitsPerFontPoint = 0.1f;

        public const float Clearance = 0.12f;

        public const float TextLift = 0.01f;

        public static float WidthFor(int capacity)
        {
            RequireCapacity(capacity);
            return capacity * CellWidth + 2f * SidePadding;
        }

        public static float FontSizeFor(int capacity)
        {
            RequireCapacity(capacity);

            var byWidth = CellWidth / (MonospaceEm * UnitsPerFontPoint);
            var byHeight = (Height - 2f * VerticalPadding) / (CapHeightEm * UnitsPerFontPoint);
            return byWidth < byHeight ? byWidth : byHeight;
        }

        public static float AnchorHeight(WorldPart prop)
        {
            return WorldParts.TopOf(prop) + Clearance + Height * 0.5f;
        }

        static void RequireCapacity(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A badge holds at least one glyph.");
            }
        }
    }
}
