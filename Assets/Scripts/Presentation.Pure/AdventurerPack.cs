using System;

namespace Game.Presentation.Pure
{
    public static class AdventurerPack
    {
        public const float GridUnits = DungeonPack.GridUnits;

        public const float StandingScales = 1.6f;

        public const float Facing = 225f;

        public const string SlotNode = "handslot";

        public const float KnightPackHeight = 2.63436f;

        public const float KnightPackWidth = 1.94251f;

        public const float KnightPackDepth = 1.25362f;

        public const float KnightPackBase = -0.1678f;

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }

        public static bool Carries(PartModel model)
        {
            return model == PartModel.Knight;
        }

        public static float PackHeightOf(PartModel model)
        {
            Guard(model);

            return KnightPackHeight;
        }

        public static float PackWidthOf(PartModel model)
        {
            Guard(model);

            return KnightPackWidth;
        }

        public static float PackDepthOf(PartModel model)
        {
            Guard(model);

            return KnightPackDepth;
        }

        public static float PackBaseOf(PartModel model)
        {
            Guard(model);

            return KnightPackBase;
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

        static void Guard(PartModel model)
        {
            if (!Carries(model))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(model), model, "The adventurers pack carries no mesh for that part model.");
            }
        }
    }
}
