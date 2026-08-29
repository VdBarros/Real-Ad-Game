using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class PlayerTier
    {
        static readonly int[] thresholds = { 8, 30, 100, 300 };

        public static IReadOnlyList<int> Thresholds
        {
            get { return thresholds; }
        }

        public static int Count
        {
            get { return thresholds.Length + 1; }
        }

        public static int Of(int power)
        {
            if (power < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(power), power, "A run always holds power.");
            }

            var tier = 0;
            while (tier < thresholds.Length && power >= thresholds[tier])
            {
                tier++;
            }

            return tier;
        }
    }
}
