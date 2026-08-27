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

            return AdventurerPack.Carries(model) ? ArtPack.Adventurers : ArtPack.Dungeon;
        }

        public static bool IsRigged(PartModel model)
        {
            return model != PartModel.None && Of(model) == ArtPack.Adventurers;
        }

        public static float ImportScaleOf(ArtPack pack)
        {
            switch (pack)
            {
                case ArtPack.Dungeon:
                    return DungeonPack.ImportScale;
                case ArtPack.Adventurers:
                    return AdventurerPack.ImportScale;
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
            return Of(model) == ArtPack.Adventurers
                ? AdventurerPack.PackHeightOf(model)
                : DungeonPack.PackHeightOf(model);
        }

        public static float HeightOf(PartModel model)
        {
            return Of(model) == ArtPack.Adventurers
                ? AdventurerPack.HeightOf(model)
                : DungeonPack.HeightOf(model);
        }
    }
}
