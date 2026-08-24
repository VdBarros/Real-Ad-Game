using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public static class BadgeText
    {
        public static string Of(BadgeStyle style, long value)
        {
            return BadgeStyles.Prefix(style) + value.ToString(CultureInfo.InvariantCulture);
        }

        public static int Cells(BadgeStyle style, long value)
        {
            return BadgeStyles.Prefix(style).Length + Digits(value);
        }

        public static int Digits(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "A badge never carries a negative number.");
            }

            var digits = 1;
            var remaining = value;
            while (remaining >= 10)
            {
                remaining /= 10;
                digits++;
            }

            return digits;
        }
    }
}
