namespace Game.Domain
{
    public static class Terraces
    {
        public const int Rise = 2;

        public static int ElevationOf(int terrace)
        {
            return terrace * Rise;
        }
    }
}
