using System;

namespace Game.Presentation.Pure
{
    public static class AdventurerPack
    {
        public const float GridUnits = DungeonPack.GridUnits;

        public const float StandingScales = 1.6f;

        public const float Facing = 225f;

        public const string SlotNode = "handslot";

        public const string RigNode = "Rig";

        public const string CloakSuffix = "_Cape";

        public const string KnightCloakNode = "Knight" + CloakSuffix;

        public const string BarbarianCloakNode = "Barbarian" + CloakSuffix;

        public const string RogueCloakNode = "Rogue" + CloakSuffix;

        public const string MageCloakNode = "Mage" + CloakSuffix;

        public const float KnightPackHeight = 2.63436f;

        public const float KnightPackWidth = 1.94251f;

        public const float KnightPackDepth = 1.25362f;

        public const float KnightPackBase = -0.1678f;

        public const float BarbarianPackHeight = 2.58248f;

        public const float BarbarianPackWidth = 1.94251f;

        public const float BarbarianPackDepth = 1.2639f;

        public const float BarbarianPackBase = -0.18471f;

        public const float RoguePackHeight = 2.37168f;

        public const float RoguePackWidth = 1.94251f;

        public const float RoguePackDepth = 1.01551f;

        public const float RoguePackBase = -0.18471f;

        public const float MagePackHeight = 2.90035f;

        public const float MagePackWidth = 2.09291f;

        public const float MagePackDepth = 2.03961f;

        public const float MagePackBase = -0.18471f;

        public const float KnightPackTurn = 1.94385f;

        public const float BarbarianPackTurn = 1.94398f;

        public const float RoguePackTurn = 1.94385f;

        public const float MagePackTurn = 2.16179f;

        public const float StandingPerPackUnit = StandingScales / KnightPackHeight;

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }

        public static bool Carries(PartModel model)
        {
            return model == PartModel.Knight
                || model == PartModel.Barbarian
                || model == PartModel.Rogue
                || model == PartModel.Mage;
        }

        public static float PackHeightOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Knight:
                    return KnightPackHeight;
                case PartModel.Barbarian:
                    return BarbarianPackHeight;
                case PartModel.Rogue:
                    return RoguePackHeight;
                case PartModel.Mage:
                    return MagePackHeight;
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
                case PartModel.Barbarian:
                    return BarbarianPackWidth;
                case PartModel.Rogue:
                    return RoguePackWidth;
                case PartModel.Mage:
                    return MagePackWidth;
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
                case PartModel.Barbarian:
                    return BarbarianPackDepth;
                case PartModel.Rogue:
                    return RoguePackDepth;
                case PartModel.Mage:
                    return MagePackDepth;
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
                case PartModel.Barbarian:
                    return BarbarianPackBase;
                case PartModel.Rogue:
                    return RoguePackBase;
                case PartModel.Mage:
                    return MagePackBase;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackTurnOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.Knight:
                    return KnightPackTurn;
                case PartModel.Barbarian:
                    return BarbarianPackTurn;
                case PartModel.Rogue:
                    return RoguePackTurn;
                case PartModel.Mage:
                    return MagePackTurn;
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
