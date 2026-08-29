using System;

namespace Game.Presentation.Pure
{
    public static class DungeonPack
    {
        public const float GridUnits = 4f;

        public const float BoundsEpsilon = 0.002f;

        public const float FloorTilePackHeight = 0.15f;

        public const float FloorTilePackWidth = GridUnits;

        public const float FloorTilePackRun = GridUnits;

        public const float FloorTilePackBase = -0.1f;

        public const float WallPanelPackHeight = 1.1f;

        public const float WallPanelPackWidth = GridUnits;

        public const float WallPanelPackRun = 0.5f;

        public const float ChestPackHeight = 1.3f;

        public const float ChestPackWidth = 1.7f;

        public const float ChestPackRun = 1.4459f;

        public const float ChestPackShiftAlong = 0.0229f;

        public const float CoinStackPackHeight = 1.157f;

        public const float StaircasePackHeight = 5.1f;

        public const float StaircasePackTread = 4f;

        public const float StaircasePackWidth = GridUnits;

        public const float StaircasePackRun = GridUnits;

        public const float StaircasePackShiftAlong = 2f;

        public const float FoundationPackHeight = 2f;

        public const float FoundationPackWidth = 2.2f;

        public const float FoundationPackRun = 2.2f;

        public const float PillarPackHeight = GridUnits;

        public const float PillarPackWidth = 1.5f;

        public const float PillarPackRun = 1.5f;

        public const float CandlePackHeight = 0.8732f;

        public const float CandlePackWidth = 0.3344f;

        public const float CandlePackRun = 0.3293f;

        public const float CoinStackPackWidth = 1.4366f;

        public const float CoinStackPackRun = 1.6587f;

        public const float CoinStackPackShiftAcross = 0.0011f;

        public const float CoinStackPackShiftAlong = 0.0138f;

        public const float ColumnPackHeight = 1.4f;

        public const float ColumnPackWidth = 0.7f;

        public const float ColumnPackRun = 0.7f;

        public const float TorchLitPackHeight = 1.1258f;

        public const float TorchLitPackWidth = 0.5503f;

        public const float TorchLitPackRun = 0.5503f;

        public const float TorchLitPackBase = -0.3951f;

        public const float BarrelLargePackHeight = 2f;

        public const float BarrelLargePackWidth = 1.8f;

        public const float BarrelLargePackRun = 1.8f;

        public const float CratesStackedPackHeight = 2.1424f;

        public const float CratesStackedPackWidth = 2.0893f;

        public const float CratesStackedPackRun = 2.2495f;

        public const float CratesStackedPackShiftAcross = 0.0454f;

        public const float CratesStackedPackShiftAlong = -0.0325f;

        public const float SwordShieldPackHeight = 1.6675f;

        public const float SwordShieldPackWidth = 2.2321f;

        public const float SwordShieldPackRun = 0.3358f;

        public const float SwordShieldPackBase = -0.815f;

        public const float SwordShieldPackShiftAlong = 0.0833f;


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
                case PartModel.Column:
                    return ColumnPackHeight;
                case PartModel.TorchLit:
                    return TorchLitPackHeight;
                case PartModel.BarrelLarge:
                    return BarrelLargePackHeight;
                case PartModel.CratesStacked:
                    return CratesStackedPackHeight;
                case PartModel.SwordShield:
                    return SwordShieldPackHeight;
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
                case PartModel.FloorTile:
                    return FloorTilePackWidth;
                case PartModel.WallPanel:
                    return WallPanelPackWidth;
                case PartModel.Chest:
                    return ChestPackWidth;
                case PartModel.Staircase:
                    return StaircasePackWidth;
                case PartModel.Foundation:
                    return FoundationPackWidth;
                case PartModel.Pillar:
                    return PillarPackWidth;
                case PartModel.Candle:
                    return CandlePackWidth;
                case PartModel.CoinStack:
                    return CoinStackPackWidth;
                case PartModel.Column:
                    return ColumnPackWidth;
                case PartModel.TorchLit:
                    return TorchLitPackWidth;
                case PartModel.BarrelLarge:
                    return BarrelLargePackWidth;
                case PartModel.CratesStacked:
                    return CratesStackedPackWidth;
                case PartModel.SwordShield:
                    return SwordShieldPackWidth;
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
                case PartModel.FloorTile:
                    return FloorTilePackRun;
                case PartModel.WallPanel:
                    return WallPanelPackRun;
                case PartModel.Chest:
                    return ChestPackRun;
                case PartModel.Staircase:
                    return StaircasePackRun;
                case PartModel.Foundation:
                    return FoundationPackRun;
                case PartModel.Pillar:
                    return PillarPackRun;
                case PartModel.Candle:
                    return CandlePackRun;
                case PartModel.CoinStack:
                    return CoinStackPackRun;
                case PartModel.Column:
                    return ColumnPackRun;
                case PartModel.TorchLit:
                    return TorchLitPackRun;
                case PartModel.BarrelLarge:
                    return BarrelLargePackRun;
                case PartModel.CratesStacked:
                    return CratesStackedPackRun;
                case PartModel.SwordShield:
                    return SwordShieldPackRun;
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

        public static float PackBaseOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.FloorTile:
                    return FloorTilePackBase;
                case PartModel.TorchLit:
                    return TorchLitPackBase;
                case PartModel.SwordShield:
                    return SwordShieldPackBase;
                case PartModel.None:
                    throw Unmeshed(model);
                default:
                    RequireAMeasuredMesh(model);
                    return 0f;
            }
        }

        public static float BaseShareOf(PartModel model)
        {
            return PackBaseOf(model) / PackHeightOf(model);
        }

        public static float PackShiftAcrossOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.CoinStack:
                    return CoinStackPackShiftAcross;
                case PartModel.CratesStacked:
                    return CratesStackedPackShiftAcross;
                case PartModel.None:
                    throw Unmeshed(model);
                default:
                    RequireAMeasuredMesh(model);
                    return 0f;
            }
        }

        public static float PackShiftAlongOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Chest:
                    return ChestPackShiftAlong;
                case PartModel.Staircase:
                    return StaircasePackShiftAlong;
                case PartModel.CoinStack:
                    return CoinStackPackShiftAlong;
                case PartModel.CratesStacked:
                    return CratesStackedPackShiftAlong;
                case PartModel.SwordShield:
                    return SwordShieldPackShiftAlong;
                case PartModel.None:
                    throw Unmeshed(model);
                default:
                    RequireAMeasuredMesh(model);
                    return 0f;
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

        public static float BaseOf(PartModel model)
        {
            return PackBaseOf(model) * ImportScale;
        }

        public static float ShiftAcrossOf(PartModel model)
        {
            return PackShiftAcrossOf(model) * ImportScale;
        }

        public static float ShiftAlongOf(PartModel model)
        {
            return PackShiftAlongOf(model) * ImportScale;
        }

        public static WorldPoint SizeOf(PartModel model, float height)
        {
            var tall = HeightOf(model);

            if (height <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "A pack mesh is stood at a height above nothing.");
            }

            return new WorldPoint(
                WidthOf(model) * height / tall, height, DepthOf(model) * height / tall);
        }

        public static WorldPoint FitOf(PartModel model, WorldPoint size)
        {
            return new WorldPoint(
                size.X / WidthOf(model), size.Y / FillOf(model), size.Z / DepthOf(model));
        }

        static void RequireAMeasuredMesh(PartModel model)
        {
            PackHeightOf(model);
        }

        static ArgumentOutOfRangeException Unmeshed(PartModel model)
        {
            return new ArgumentOutOfRangeException(
                nameof(model), model, "A part with no model has no mesh to measure.");
        }
    }
}
