using System;

namespace Game.Presentation.Pure
{
    public static class AdventurerPack
    {
        public const float GridUnits = DungeonPack.GridUnits;

        public const float StandingScales = 1.6f;

        public const float Facing = 225f;

        public const string SlotNode = "handslot";

        public const string CloakNode = "Knight_Cape";

        public const float KnightPackHeight = 2.63436f;

        public const float KnightPackWidth = 1.94251f;

        public const float KnightPackDepth = 1.25362f;

        public const float KnightPackBase = -0.1678f;

        public const float Sword1HandedPackHeight = 1.77526f;

        public const float Sword1HandedPackWidth = 0.50344f;

        public const float Sword1HandedPackDepth = 0.13063f;

        public const float Sword1HandedPackBase = -0.36577f;

        public const float Axe2HandedPackHeight = 1.72463f;

        public const float Axe2HandedPackWidth = 1.23955f;

        public const float Axe2HandedPackDepth = 0.2618f;

        public const float Axe2HandedPackBase = -0.43298f;

        public const float StaffPackHeight = 2.1547f;

        public const float StaffPackWidth = 0.57621f;

        public const float StaffPackDepth = 0.29235f;

        public const float StaffPackBase = -0.90045f;

        public const float Sword2HandedPackHeight = 2.3658f;

        public const float Sword2HandedPackWidth = 0.83903f;

        public const float Sword2HandedPackDepth = 0.24802f;

        public const float Sword2HandedPackBase = -0.40136f;

        public const float StandingPerPackUnit = StandingScales / KnightPackHeight;

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }

        public static bool Carries(PartModel model)
        {
            return model == PartModel.Knight || Wields(model);
        }

        public static bool Wields(PartModel model)
        {
            switch (model)
            {
                case PartModel.Sword1Handed:
                case PartModel.Axe2Handed:
                case PartModel.Staff:
                case PartModel.Sword2Handed:
                    return true;
                default:
                    return false;
            }
        }

        public static float PackHeightOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Knight:
                    return KnightPackHeight;
                case PartModel.Sword1Handed:
                    return Sword1HandedPackHeight;
                case PartModel.Axe2Handed:
                    return Axe2HandedPackHeight;
                case PartModel.Staff:
                    return StaffPackHeight;
                case PartModel.Sword2Handed:
                    return Sword2HandedPackHeight;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackWidthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Knight:
                    return KnightPackWidth;
                case PartModel.Sword1Handed:
                    return Sword1HandedPackWidth;
                case PartModel.Axe2Handed:
                    return Axe2HandedPackWidth;
                case PartModel.Staff:
                    return StaffPackWidth;
                case PartModel.Sword2Handed:
                    return Sword2HandedPackWidth;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackDepthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Knight:
                    return KnightPackDepth;
                case PartModel.Sword1Handed:
                    return Sword1HandedPackDepth;
                case PartModel.Axe2Handed:
                    return Axe2HandedPackDepth;
                case PartModel.Staff:
                    return StaffPackDepth;
                case PartModel.Sword2Handed:
                    return Sword2HandedPackDepth;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackBaseOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Knight:
                    return KnightPackBase;
                case PartModel.Sword1Handed:
                    return Sword1HandedPackBase;
                case PartModel.Axe2Handed:
                    return Axe2HandedPackBase;
                case PartModel.Staff:
                    return StaffPackBase;
                case PartModel.Sword2Handed:
                    return Sword2HandedPackBase;
                default:
                    throw Stranger(model);
            }
        }

        public static float HeightOf(PartModel model)
        {
            return PackHeightOf(model) * ImportScale;
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

        static ArgumentOutOfRangeException Stranger(PartModel model)
        {
            return new ArgumentOutOfRangeException(
                nameof(model), model, "The adventurers pack carries no mesh for that part model.");
        }
    }
}
