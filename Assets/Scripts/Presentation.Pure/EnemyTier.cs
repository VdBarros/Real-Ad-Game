using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class EnemyTier
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

        public static int Of(int number)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(number), number, "An enemy always holds power.");
            }

            var tier = 0;
            while (tier < thresholds.Length && number >= thresholds[tier])
            {
                tier++;
            }

            return tier;
        }
    }
}
