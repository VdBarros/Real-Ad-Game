namespace Game.Presentation.Pure
{
    public static class DungeonPack
    {
        public const float GridUnits = 4f;

        public const float BoundsEpsilon = 0.002f;

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }
    }
}
