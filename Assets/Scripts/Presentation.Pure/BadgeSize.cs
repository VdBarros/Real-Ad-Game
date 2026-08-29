using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct BadgeSize : IEquatable<BadgeSize>
    {
        internal BadgeSize(float cells, float scale)
        {
            Cells = cells;
            Scale = scale;
        }

        public float Cells { get; }

        public float Scale { get; }

        public float Width
        {
            get { return BadgeMetrics.WidthFor(Cells) * Scale; }
        }

        public float Height
        {
            get { return BadgeMetrics.Height * Scale; }
        }

        public float FontSize
        {
            get { return BadgeMetrics.FontSize * Scale; }
        }

        public bool Equals(BadgeSize other)
        {
            return Cells.Equals(other.Cells) && Scale.Equals(other.Scale);
        }

        public override bool Equals(object obj)
        {
            return obj is BadgeSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Cells.GetHashCode() * 397) ^ Scale.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Concat(
                Cells.ToString("0.###", CultureInfo.InvariantCulture),
                " cells at ",
                Scale.ToString("0.###", CultureInfo.InvariantCulture),
                " scale, ",
                Width.ToString("0.###", CultureInfo.InvariantCulture),
                " x ",
                Height.ToString("0.###", CultureInfo.InvariantCulture),
                " units");
        }
    }
}
