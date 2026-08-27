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

            return SkeletonPack.Carries(model) ? ArtPack.Skeletons : ArtPack.Dungeon;
        }

        public static bool IsRigged(PartModel model)
        {
            return model != PartModel.None && IsRigged(Of(model));
        }

        public static bool IsRigged(ArtPack pack)
        {
            return pack == ArtPack.Adventurers || pack == ArtPack.Skeletons;
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
                default:
                    return DungeonPack.PackHeightOf(model);
            }
        }

        public static float PackWidthOf(PartModel model)
        {
            switch (Cast(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.PackWidthOf(model);
                case ArtPack.Skeletons:
                    return SkeletonPack.PackWidthOf(model);
                default:
                    throw Unmeasured(model);
            }
        }

        public static float PackDepthOf(PartModel model)
        {
            switch (Cast(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.PackDepthOf(model);
                case ArtPack.Skeletons:
                    return SkeletonPack.PackDepthOf(model);
                default:
                    throw Unmeasured(model);
            }
        }

        public static float PackBaseOf(PartModel model)
        {
            switch (Cast(model))
            {
                case ArtPack.Adventurers:
                    return AdventurerPack.PackBaseOf(model);
                case ArtPack.Skeletons:
                    return SkeletonPack.PackBaseOf(model);
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
                nameof(model), model, "That cast pack carries no measured figure footprint.");
        }

        static ArtPack Cast(PartModel model)
        {
            var pack = Of(model);

            if (!IsRigged(pack))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(model), model, "Only a rigged cast mesh carries a measured figure footprint.");
            }

            return pack;
        }
    }
}
