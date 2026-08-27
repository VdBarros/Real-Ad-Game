using System;

namespace Game.Presentation.Pure
{
    public static class DungeonPack
    {
        public const float GridUnits = 4f;

        public const float BoundsEpsilon = 0.002f;

        public const float FloorTilePackHeight = 0.15f;

        public const float WallPanelPackHeight = 1.1f;

        public const float WallPanelPackWidth = GridUnits;

        public const float ChestPackHeight = 1.3f;

        public const float CoinStackPackHeight = 1.157f;

        public const float StaircasePackHeight = 5.1f;

        public const float StaircasePackWidth = GridUnits;

        public const float StaircasePackRun = GridUnits;

        public const float FoundationPackHeight = 2f;

        public const float FoundationPackWidth = 2.2f;

        public const float FoundationPackRun = 2.2f;

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }

        public static float WallPanelWidth
        {
            get { return WallPanelPackWidth * ImportScale; }
        }

        public static float StaircaseWidth
        {
            get { return StaircasePackWidth * ImportScale; }
        }

        public static float StaircaseRun
        {
            get { return StaircasePackRun * ImportScale; }
        }

        public static float FoundationWidth
        {
            get { return FoundationPackWidth * ImportScale; }
        }

        public static float FoundationRun
        {
            get { return FoundationPackRun * ImportScale; }
        }

        public static float PackHeightOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.FloorTile:
                    return FloorTilePackHeight;
                case PartModel.WallPanel:
                    return WallPanelPackHeight;
                case PartModel.Chest:
                    return ChestPackHeight;
                case PartModel.CoinStack:
                    return CoinStackPackHeight;
                case PartModel.Staircase:
                    return StaircasePackHeight;
                case PartModel.Foundation:
                    return FoundationPackHeight;
                case PartModel.None:
                    throw new ArgumentOutOfRangeException(
                        nameof(model), model, "A part with no model has no mesh to measure.");
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(model), model, "No measured pack height for that part model.");
            }
        }

        public static float HeightOf(PartModel model)
        {
            return PackHeightOf(model) * ImportScale;
        }
    }
}
