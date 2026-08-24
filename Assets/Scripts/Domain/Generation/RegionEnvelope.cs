using System.Globalization;

namespace Game.Domain
{
    public sealed class RegionEnvelope
    {
        public RegionEnvelope(int regionId, int cheapestEntry, int richestEntry, int cheapestEnemy, int dearestEnemy)
        {
            RegionId = regionId;
            CheapestEntry = cheapestEntry;
            RichestEntry = richestEntry;
            CheapestEnemy = cheapestEnemy;
            DearestEnemy = dearestEnemy;
        }

        public int RegionId { get; }

        public int CheapestEntry { get; }

        public int RichestEntry { get; }

        public int CheapestEnemy { get; }

        public int DearestEnemy { get; }

        public bool HoldsAnEnemy
        {
            get { return CheapestEnemy > 0; }
        }

        public bool FloorHolds
        {
            get { return !HoldsAnEnemy || CheapestEnemy <= CheapestEntry; }
        }

        public bool WallsAreOrdered
        {
            get { return CheapestEntry <= RichestEntry; }
        }

        public double Spread
        {
            get { return CheapestEntry <= 0 ? 0.0 : (double)RichestEntry / CheapestEntry; }
        }

        public override string ToString()
        {
            return string.Concat(
                "region ",
                RegionId.ToString(CultureInfo.InvariantCulture),
                ": P_min ",
                CheapestEntry.ToString(CultureInfo.InvariantCulture),
                ", P_max ",
                RichestEntry.ToString(CultureInfo.InvariantCulture),
                ", enemies ",
                CheapestEnemy.ToString(CultureInfo.InvariantCulture),
                "..",
                DearestEnemy.ToString(CultureInfo.InvariantCulture));
        }
    }
}
