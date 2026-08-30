using System;

namespace Game.Presentation.Pure
{
    public static class SkeletonPack
    {
        public const float GridUnits = DungeonPack.GridUnits;

        public const float Facing = 225f;

        public const string SlotNode = "handslot";

        public const float MinionPackHeight = 2.34452f;

        public const float MinionPackWidth = 1.93818f;

        public const float MinionPackDepth = 0.91223f;

        public const float MinionPackBase = -0.17844f;

        public const float RoguePackHeight = 2.52014f;

        public const float RoguePackWidth = 1.94251f;

        public const float RoguePackDepth = 1.15279f;

        public const float RoguePackBase = -0.21222f;

        public const float WarriorPackHeight = 2.80265f;

        public const float WarriorPackWidth = 1.94251f;

        public const float WarriorPackDepth = 1.45877f;

        public const float WarriorPackBase = -0.21222f;

        public const float MagePackHeight = 2.80867f;

        public const float MagePackWidth = 1.93818f;

        public const float MagePackDepth = 1.7353f;

        public const float MagePackBase = -0.17844f;

        public const float MinionPackTurn = 1.93891f;

        public const float RoguePackTurn = 1.94385f;

        public const float WarriorPackTurn = 1.94385f;

        public const float MagePackTurn = 1.93891f;

        static readonly float shortestPackHeight = Shortest();

        public static float ImportScale
        {
            get { return IsoProjection.TileEdge / GridUnits; }
        }

        public static float ShortestPackHeight
        {
            get { return shortestPackHeight; }
        }

        public static float StandingScales
        {
            get { return AdventurerPack.StandingPerPackUnit * shortestPackHeight; }
        }

        public static bool Carries(PartModel model)
        {
            switch (model)
            {
                case PartModel.SkeletonMinion:
                case PartModel.SkeletonRogue:
                case PartModel.SkeletonWarrior:
                case PartModel.SkeletonMage:
                    return true;
                default:
                    return false;
            }
        }

        public static float PackHeightOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SkeletonMinion:
                    return MinionPackHeight;
                case PartModel.SkeletonRogue:
                    return RoguePackHeight;
                case PartModel.SkeletonWarrior:
                    return WarriorPackHeight;
                case PartModel.SkeletonMage:
                    return MagePackHeight;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackWidthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SkeletonMinion:
                    return MinionPackWidth;
                case PartModel.SkeletonRogue:
                    return RoguePackWidth;
                case PartModel.SkeletonWarrior:
                    return WarriorPackWidth;
                case PartModel.SkeletonMage:
                    return MagePackWidth;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackDepthOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SkeletonMinion:
                    return MinionPackDepth;
                case PartModel.SkeletonRogue:
                    return RoguePackDepth;
                case PartModel.SkeletonWarrior:
                    return WarriorPackDepth;
                case PartModel.SkeletonMage:
                    return MagePackDepth;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackTurnOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SkeletonMinion:
                    return MinionPackTurn;
                case PartModel.SkeletonRogue:
                    return RoguePackTurn;
                case PartModel.SkeletonWarrior:
                    return WarriorPackTurn;
                case PartModel.SkeletonMage:
                    return MagePackTurn;
                default:
                    throw Stranger(model);
            }
        }

        public static float PackBaseOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.SkeletonMinion:
                    return MinionPackBase;
                case PartModel.SkeletonRogue:
                    return RoguePackBase;
                case PartModel.SkeletonWarrior:
                    return WarriorPackBase;
                case PartModel.SkeletonMage:
                    return MagePackBase;
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

        static float Shortest()
        {
            var found = float.MaxValue;

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (!Carries(model))
                {
                    continue;
                }

                var height = PackHeightOf(model);

                if (height < found)
                {
                    found = height;
                }
            }

            return found;
        }

        static ArgumentOutOfRangeException Stranger(PartModel model)
        {
            return new ArgumentOutOfRangeException(
                nameof(model), model, "The skeletons pack carries no mesh for that part model.");
        }
    }
}
