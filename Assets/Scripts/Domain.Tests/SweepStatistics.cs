using System.Collections.Generic;
using System.Globalization;

namespace Game.Domain.Tests
{
    static class SweepStatistics
    {
        public static T Percentile<T>(List<T> sorted, double share)
        {
            return sorted[(int)(share * (sorted.Count - 1))];
        }

        public static string Round(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
