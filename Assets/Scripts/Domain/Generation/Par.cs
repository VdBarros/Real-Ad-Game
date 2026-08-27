using System;
using System.Globalization;

namespace Game.Domain
{
    public sealed class Par
    {
        Par(int floor, int ceiling)
        {
            Floor = floor;
            Ceiling = ceiling;
        }

        public int Floor { get; }

        public int Ceiling { get; }

        public int Span
        {
            get { return Ceiling - Floor; }
        }

        public bool IsDegenerate
        {
            get { return Span <= 0; }
        }

        public static Par Between(int floor, int ceiling)
        {
            return new Par(floor, ceiling);
        }

        public static Par Of(PlacedLevel level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var floor = level.ShortestPathPower + level.BossPower;
            var richest = ParWalk.Richest(level.Graph, level.Tuning, level.BossNodeId);

            return new Par(floor, richest.BeatsTheBoss ? richest.Finish : floor);
        }

        public double PositionOf(int finalPower)
        {
            if (IsDegenerate || finalPower >= Ceiling)
            {
                return 1.0;
            }

            if (finalPower <= Floor)
            {
                return 0.0;
            }

            return (double)(finalPower - Floor) / Span;
        }

        public override string ToString()
        {
            return string.Concat(
                "par ",
                Floor.ToString(CultureInfo.InvariantCulture),
                "..",
                Ceiling.ToString(CultureInfo.InvariantCulture),
                IsDegenerate ? " (walls met)" : string.Empty);
        }
    }
}
