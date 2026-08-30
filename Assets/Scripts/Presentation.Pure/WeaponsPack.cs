using System;

namespace Game.Presentation.Pure
{
    public static class WeaponsPack
    {
        public const float GridUnits = 4f;

        public const float UprightRoll = 90f;

        public const float UprightTurn = 90f;

        public const float SwordAPackHeight = 1.77386f;

        public const float SwordAPackWidth = 0.60775f;

        public const float SwordAPackDepth = 0.12868f;

        public const float SwordAPackBase = -0.35705f;

        public const float AxeBPackHeight = 1.66126f;

        public const float AxeBPackWidth = 1.15345f;

        public const float AxeBPackDepth = 0.25036f;

        public const float AxeBPackBase = -0.41285f;

        public const float StaffAPackHeight = 2.14938f;

        public const float StaffAPackWidth = 0.12486f;

        public const float StaffAPackDepth = 0.11146f;

        public const float StaffAPackBase = -1.07469f;

        public const float StaffBPackHeight = 2.31593f;

        public const float StaffBPackWidth = 0.61694f;

        public const float StaffBPackDepth = 0.27372f;

        public const float StaffBPackBase = -0.86171f;

        public const float BowAPackHeight = 0.09164f;

        public const float BowAPackWidth = 1.95746f;

        public const float BowAPackDepth = 0.39575f;

        public const float BowAPackBase = -0.04582f;

        public const float BowAPackLeft = -0.97873f;

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }

        public static bool Carries(PartModel model)
        {
            return Wields(model);
        }

        public static bool Wields(PartModel model)
        {
            switch (model)
            {
                case PartModel.SwordA:
                case PartModel.AxeB:
                case PartModel.StaffA:
                case PartModel.StaffB:
                case PartModel.BowA:
                    return true;
                default:
                    return false;
            }
        }

        public static bool LiesFlat(PartModel model)
        {
            return model == PartModel.BowA;
        }

        public static float MountRollOf(PartModel model)
        {
            return LiesFlat(model) ? UprightRoll : 0f;
        }

        public static float MountTurnOf(PartModel model)
        {
            return LiesFlat(model) ? UprightTurn : 0f;
        }

        public static float PackLeftOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.BowA:
                    return BowAPackLeft;
                default:
                    throw Upright(model);
            }
        }

        public static float MountedPackWidthOf(PartModel model)
        {
            return LiesFlat(model) ? PackDepthOf(model) : PackWidthOf(model);
        }

        public static float MountedPackHeightOf(PartModel model)
        {
            return LiesFlat(model) ? PackWidthOf(model) : PackHeightOf(model);
        }

        public static float MountedPackDepthOf(PartModel model)
        {
            return LiesFlat(model) ? PackHeightOf(model) : PackDepthOf(model);
        }

        public static float MountedPackBaseOf(PartModel model)
        {
            return LiesFlat(model) ? PackLeftOf(model) : PackBaseOf(model);
        }

        public static float PackHeightOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SwordA:
                    return SwordAPackHeight;
                case PartModel.AxeB:
                    return AxeBPackHeight;
                case PartModel.StaffA:
                    return StaffAPackHeight;
                case PartModel.StaffB:
                    return StaffBPackHeight;
                case PartModel.BowA:
                    return BowAPackHeight;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackWidthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SwordA:
                    return SwordAPackWidth;
                case PartModel.AxeB:
                    return AxeBPackWidth;
                case PartModel.StaffA:
                    return StaffAPackWidth;
                case PartModel.StaffB:
                    return StaffBPackWidth;
                case PartModel.BowA:
                    return BowAPackWidth;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackDepthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SwordA:
                    return SwordAPackDepth;
                case PartModel.AxeB:
                    return AxeBPackDepth;
                case PartModel.StaffA:
                    return StaffAPackDepth;
                case PartModel.StaffB:
                    return StaffBPackDepth;
                case PartModel.BowA:
                    return BowAPackDepth;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackBaseOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SwordA:
                    return SwordAPackBase;
                case PartModel.AxeB:
                    return AxeBPackBase;
                case PartModel.StaffA:
                    return StaffAPackBase;
                case PartModel.StaffB:
                    return StaffBPackBase;
                case PartModel.BowA:
                    return BowAPackBase;
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
                nameof(model), model, "The weapons pack carries no mesh for that part model.");
        }

        static ArgumentOutOfRangeException Upright(PartModel model)
        {
            return new ArgumentOutOfRangeException(
                nameof(model), model, "Only a mesh the pack authors lying flat is measured from its left "
                + "edge, because only a rolled mount stands that edge up.");
        }
    }
}
