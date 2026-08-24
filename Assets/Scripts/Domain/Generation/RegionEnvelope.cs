using System.Globalization;

namespace Game.Domain
{
    public sealed class RegionEnvelope
    {
        public RegionEnvelope(int regionId, int minimum, int maximum, int cheapestEnemy, int dearestEnemy)
        {
            RegionId = regionId;
            Minimum = minimum;
            Maximum = maximum;
            CheapestEnemy = cheapestEnemy;
            DearestEnemy = dearestEnemy;
        }

        public int RegionId { get; }

        public int Minimum { get; }

        public int Maximum { get; }

        public int CheapestEnemy { get; }

        public int DearestEnemy { get; }

        public bool HoldsAnEnemy
        {
            get { return CheapestEnemy > 0; }
        }

        public bool FloorHolds
        {
            get { return !HoldsAnEnemy || CheapestEnemy <= Minimum; }
        }

        public bool WallsAreOrdered
        {
            get { return Minimum <= Maximum; }
        }

        public double Spread
        {
            get { return Minimum <= 0 ? 0.0 : (double)Maximum / Minimum; }
        }

        public override string ToString()
        {
            return string.Concat(
                "region ",
                RegionId.ToString(CultureInfo.InvariantCulture),
                ": P_min ",
                Minimum.ToString(CultureInfo.InvariantCulture),
                ", P_max ",
                Maximum.ToString(CultureInfo.InvariantCulture),
                ", enemies ",
                CheapestEnemy.ToString(CultureInfo.InvariantCulture),
                "..",
                DearestEnemy.ToString(CultureInfo.InvariantCulture));
        }
    }
}
