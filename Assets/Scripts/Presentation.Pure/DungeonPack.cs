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

        public const float StaircasePackTread = 4f;

        public const float StaircasePackWidth = GridUnits;

        public const float StaircasePackRun = GridUnits;

        public const float FoundationPackHeight = 2f;

        public const float FoundationPackWidth = 2.2f;

        public const float FoundationPackRun = 2.2f;

        public const float PillarPackHeight = GridUnits;

        public const float PillarPackWidth = 1.5f;

        public const float PillarPackRun = 1.5f;

        public const float CandlePackHeight = 0.8732f;

        public const float CandlePackWidth = 0.3344f;

        public const float CandlePackRun = 0.3293f;

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }

        public static float WallPanelWidth
        {
            get { return WidthOf(PartModel.WallPanel); }
        }

        public static float StaircaseWidth
        {
            get { return WidthOf(PartModel.Staircase); }
        }

        public static float StaircaseRun
        {
            get { return DepthOf(PartModel.Staircase); }
        }

        public static float StaircaseTread
        {
            get { return FillOf(PartModel.Staircase); }
        }

        public static float StaircaseParapet
        {
            get { return HeightOf(PartModel.Staircase) - StaircaseTread; }
        }

        public static float FoundationWidth
        {
            get { return WidthOf(PartModel.Foundation); }
        }

        public static float FoundationRun
        {
            get { return DepthOf(PartModel.Foundation); }
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
                case PartModel.Pillar:
                    return PillarPackHeight;
                case PartModel.Candle:
                    return CandlePackHeight;
                case PartModel.None:
                    throw Unmeshed(model);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(model), model, "No measured pack height for that part model.");
            }
        }

        public static float PackWidthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.WallPanel:
                    return WallPanelPackWidth;
                case PartModel.Staircase:
                    return StaircasePackWidth;
                case PartModel.Foundation:
                    return FoundationPackWidth;
                case PartModel.Pillar:
                    return PillarPackWidth;
                case PartModel.Candle:
                    return CandlePackWidth;
                case PartModel.None:
                    throw Unmeshed(model);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(model), model, "No measured pack width for that part model.");
            }
        }

        public static float PackDepthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Staircase:
                    return StaircasePackRun;
                case PartModel.Foundation:
                    return FoundationPackRun;
                case PartModel.Pillar:
                    return PillarPackRun;
                case PartModel.Candle:
                    return CandlePackRun;
                case PartModel.None:
                    throw Unmeshed(model);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(model), model, "No measured pack depth for that part model.");
            }
        }

        public static float PackFillOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Staircase:
                    return StaircasePackTread;
                default:
                    return PackHeightOf(model);
            }
        }

        public static float HeightOf(PartModel model)
        {
            return PackHeightOf(model) * ImportScale;
        }

        public static float FillOf(PartModel model)
        {
            return PackFillOf(model) * ImportScale;
        }

        public static float WidthOf(PartModel model)
        {
            return PackWidthOf(model) * ImportScale;
        }

        public static float DepthOf(PartModel model)
        {
            return PackDepthOf(model) * ImportScale;
        }

        public static WorldPoint FitOf(PartModel model, WorldPoint size)
        {
            return new WorldPoint(
                size.X / WidthOf(model), size.Y / FillOf(model), size.Z / DepthOf(model));
        }

        static ArgumentOutOfRangeException Unmeshed(PartModel model)
        {
            return new ArgumentOutOfRangeException(
                nameof(model), model, "A part with no model has no mesh to measure.");
        }
    }
}
