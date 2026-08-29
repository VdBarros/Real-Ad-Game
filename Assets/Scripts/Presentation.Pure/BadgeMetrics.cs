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

        public const float MinimumCells = 1f;

        public const float MinimumWidth = MinimumCells * CellWidth + 2f * SidePadding;

        public const float FontSizeByWidth = CellWidth / (MonospaceEm * UnitsPerFontPoint);

        public const float FontSizeByHeight = (Height - 2f * VerticalPadding) / (CapHeightEm * UnitsPerFontPoint);

        public const float FontSize = FontSizeByWidth < FontSizeByHeight ? FontSizeByWidth : FontSizeByHeight;

        public static float WidthFor(float cells)
        {
            RequireCells(cells);
            return cells * CellWidth + 2f * SidePadding;
        }

        public static float AnchorHeight(WorldPart prop)
        {
            return AnchorAbove(WorldParts.TopOf(prop));
        }

        public static float AnchorAbove(float top)
        {
            return top + Clearance + Height * 0.5f;
        }

        static void RequireCells(float cells)
        {
            if (!(cells >= MinimumCells) || float.IsInfinity(cells))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cells), cells, "A badge holds at least one glyph.");
            }
        }
    }
}
