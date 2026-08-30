using System;

namespace Game.Presentation.Pure
{
    public static class CastClips
    {
        public static ClipTable TableOf(ArtPack pack)
        {
            switch (pack)
            {
                case ArtPack.Adventurers:
                    return AdventurerClips.Table;
                case ArtPack.Skeletons:
                    return SkeletonClips.Table;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(pack), pack, "Only a cast pack names the clips a figure plays.");
            }
        }

        public static ClipTable TableFor(PartModel model)
        {
            return TableOf(ArtPacks.Of(model));
        }

        public static string NameOf(PartModel model, FigureAct act)
        {
            return TableFor(model).NameOf(act);
        }

        public static bool Loops(FigureAct act)
        {
            return FigureActs.Loops(act);
        }
    }
}
