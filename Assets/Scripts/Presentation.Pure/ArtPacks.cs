using System;

namespace Game.Presentation.Pure
{
    public static class ArtPacks
    {
        public static ArtPack Of(PartModel model)
        {
            if (model == PartModel.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(model), model, "A part with no model belongs to no pack.");
            }

            if (AdventurerPack.Carries(model))
            {
                return ArtPack.Adventurers;
            }

            if (WeaponsPack.Carries(model))
            {
                return ArtPack.Weapons;
            }

            return SkeletonPack.Carries(model) ? ArtPack.Skeletons : ArtPack.Dungeon;
        }

        public static bool IsCastPack(ArtPack pack)
        {
            return pack == ArtPack.Adventurers || pack == ArtPack.Skeletons;
        }

        public static bool HangsOnTheCast(ArtPack pack)
        {
            return pack == ArtPack.Weapons;
        }

        public static bool ShipsWithTheCast(PartModel model)
        {
            if (model == PartModel.None)
            {
                return false;
            }

            var pack = Of(model);

            return IsCastPack(pack) || HangsOnTheCast(pack);
        }

        public static bool IsRiggedCharacter(PartModel model)
        {
            if (model == PartModel.None)
            {
                return false;
            }

            switch (Of(model))
            {
                case ArtPack.Adventurers:
                    return !AdventurerPack.Wields(model);
                case ArtPack.Skeletons:
                    return true;
                default:
                    return false;
            }
        }

        public static float CastImportScale
        {
            get
            {
                var adventurers = ImportScaleOf(ArtPack.Adventurers);
                var skeletons = ImportScaleOf(ArtPack.Skeletons);

                if (adventurers != skeletons)
                {
                    throw new InvalidOperationException(
                        "The cast packs map their grids onto a tile by different amounts, so one "
                        + "import setting cannot settle both of them.");
                }

                return adventurers;
            }
        }

        public static string CastSlotNode
        {
            get
            {
                if (!string.Equals(AdventurerPack.SlotNode, SkeletonPack.SlotNode, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The cast packs hang their accessories off differently named nodes, so one strip "
                        + "cannot bare both of them.");
                }

                return AdventurerPack.SlotNode;
            }
        }

        public static float ImportScaleOf(ArtPack pack)
        {
            switch (pack)
            {
                case ArtPack.Dungeon:
                    return DungeonPack.ImportScale;
                case ArtPack.Adventurers:
                    return AdventurerPack.ImportScale;
                case ArtPack.Skeletons:
                    return SkeletonPack.ImportScale;
                case ArtPack.Weapons:
                    return WeaponsPack.ImportScale;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pack), pack, "No import scale for that pack.");
            }
        }

        public static float ImportScaleFor(PartModel model)
        {
            return ImportScaleOf(Of(model));
        }

        public static float PackHeightOf(PartModel model)
        {
            switch (Of(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.PackHeightOf(model);
                case ArtPack.Skeletons:
                    return SkeletonPack.PackHeightOf(model);
                case ArtPack.Weapons:
                    return WeaponsPack.PackHeightOf(model);
                default:
                    return DungeonPack.PackHeightOf(model);
            }
        }

        public static float PackWidthOf(PartModel model)
        {
            switch (Hung(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.PackWidthOf(model);
                case ArtPack.Skeletons:
                    return SkeletonPack.PackWidthOf(model);
                case ArtPack.Weapons:
                    return WeaponsPack.PackWidthOf(model);
                default:
                    throw Unmeasured(model);
            }
        }

        public static float PackDepthOf(PartModel model)
        {
            switch (Hung(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.PackDepthOf(model);
                case ArtPack.Skeletons:
                    return SkeletonPack.PackDepthOf(model);
                case ArtPack.Weapons:
                    return WeaponsPack.PackDepthOf(model);
                default:
                    throw Unmeasured(model);
            }
        }

        public static float PackBaseOf(PartModel model)
        {
            switch (Hung(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.PackBaseOf(model);
                case ArtPack.Skeletons:
                    return SkeletonPack.PackBaseOf(model);
                case ArtPack.Weapons:
                    return WeaponsPack.PackBaseOf(model);
                default:
                    throw Unmeasured(model);
            }
        }

        public static float StandingScalesOf(PartModel model)
        {
            switch (Cast(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.StandingScales;
                case ArtPack.Skeletons:
                    return SkeletonPack.StandingScales;
                default:
                    throw Unmeasured(model);
            }
        }

        public static float FacingOf(PartModel model)
        {
            switch (Cast(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.Facing;
                case ArtPack.Skeletons:
                    return SkeletonPack.Facing;
                default:
                    throw Unmeasured(model);
            }
        }

        public static float MountRollOf(PartModel model)
        {
            return Of(model) == ArtPack.Weapons ? WeaponsPack.MountRollOf(model) : 0f;
        }

        public static float MountTurnOf(PartModel model)
        {
            return Of(model) == ArtPack.Weapons ? WeaponsPack.MountTurnOf(model) : 0f;
        }

        public static float MountedPackWidthOf(PartModel model)
        {
            return Of(model) == ArtPack.Weapons
                ? WeaponsPack.MountedPackWidthOf(model)
                : PackWidthOf(model);
        }

        public static float MountedPackHeightOf(PartModel model)
        {
            return Of(model) == ArtPack.Weapons
                ? WeaponsPack.MountedPackHeightOf(model)
                : PackHeightOf(model);
        }

        public static float MountedPackDepthOf(PartModel model)
        {
            return Of(model) == ArtPack.Weapons
                ? WeaponsPack.MountedPackDepthOf(model)
                : PackDepthOf(model);
        }

        public static float MountedPackBaseOf(PartModel model)
        {
            return Of(model) == ArtPack.Weapons
                ? WeaponsPack.MountedPackBaseOf(model)
                : PackBaseOf(model);
        }

        public static float MountedWidthOf(PartModel model)
        {
            return MountedPackWidthOf(model) * ImportScaleFor(model);
        }

        public static float MountedHeightOf(PartModel model)
        {
            return MountedPackHeightOf(model) * ImportScaleFor(model);
        }

        public static float MountedDepthOf(PartModel model)
        {
            return MountedPackDepthOf(model) * ImportScaleFor(model);
        }

        public static float MountedBaseOf(PartModel model)
        {
            return MountedPackBaseOf(model) * ImportScaleFor(model);
        }

        public static float HeightOf(PartModel model)
        {
            return PackHeightOf(model) * ImportScaleFor(model);
        }

        public static float WidthOf(PartModel model)
        {
            return PackWidthOf(model) * ImportScaleFor(model);
        }

        public static float DepthOf(PartModel model)
        {
            return PackDepthOf(model) * ImportScaleFor(model);
        }

        public static float BaseOf(PartModel model)
        {
            return PackBaseOf(model) * ImportScaleFor(model);
        }

        static ArgumentOutOfRangeException Unmeasured(PartModel model)
        {
            return new ArgumentOutOfRangeException(
                nameof(model), model, "That pack carries no measured footprint.");
        }

        static ArtPack Hung(PartModel model)
        {
            if (!ShipsWithTheCast(model))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(model), model, "Only a mesh that ships with the cast carries a measured "
                    + "footprint.");
            }

            return Of(model);
        }

        static ArtPack Cast(PartModel model)
        {
            var pack = Of(model);

            if (!IsCastPack(pack))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(model), model, "Only a cast pack mesh carries a measured figure footprint.");
            }

            return pack;
        }
    }
}
