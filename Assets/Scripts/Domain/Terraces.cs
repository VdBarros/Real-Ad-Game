namespace Game.Domain
{
    public static class Terraces
    {
        public const int Rise = 2;

        public static int ElevationOf(int terrace)
        {
            return terrace * Rise;
        }

        public static bool IsTerrace(int elevation)
        {
            return elevation % Rise == 0;
        }

        public static int TerraceUnder(int elevation)
        {
            return elevation / Rise;
        }
    }
}
