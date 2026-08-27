using System;

namespace Game.Presentation.Pure
{
    public static class FigureFit
    {
        public const float CapsuleScales = 2f;

        public static float StandingScalesOf(PartModel model)
        {
            if (model == PartModel.None)
            {
                return CapsuleScales;
            }

            Guard(model);

            return AdventurerPack.StandingScales;
        }

        public static float ScaleOf(PartModel model)
        {
            if (model == PartModel.None)
            {
                return 1f;
            }

            Guard(model);

            return StandingScalesOf(model) / AdventurerPack.HeightOf(model);
        }

        public static float LiftOf(PartModel model)
        {
            if (model == PartModel.None)
            {
                return CapsuleScales * 0.5f;
            }

            Guard(model);

            return 0f;
        }

        public static float TopOf(PartModel model)
        {
            return StandingScalesOf(model);
        }

        public static float StandingHeight(PartModel model, float figureScale)
        {
            return figureScale * StandingScalesOf(model);
        }

        public static float WidthOf(PartModel model, float figureScale)
        {
            if (model == PartModel.None)
            {
                return figureScale;
            }

            Guard(model);

            return AdventurerPack.WidthOf(model) * ScaleOf(model) * figureScale;
        }

        public static float DepthOf(PartModel model, float figureScale)
        {
            if (model == PartModel.None)
            {
                return figureScale;
            }

            Guard(model);

            return AdventurerPack.DepthOf(model) * ScaleOf(model) * figureScale;
        }

        public static float SpreadOf(PartModel model, float figureScale)
        {
            var width = WidthOf(model, figureScale);
            var depth = DepthOf(model, figureScale);

            return model == PartModel.None
                ? figureScale
                : (float)Math.Sqrt(width * width + depth * depth);
        }

        public static float HiddenGroundOf(PartModel model, float figureScale)
        {
            return WidthOf(model, figureScale)
                * IsoProjection.SightReach(StandingHeight(model, figureScale));
        }

        public static float HiddenSpreadOf(PartModel model, float figureScale)
        {
            return SpreadOf(model, figureScale)
                * IsoProjection.SightReach(StandingHeight(model, figureScale));
        }

        static void Guard(PartModel model)
        {
            if (!ArtPacks.IsRigged(model))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(model), model, "Only a cast mesh or a primitive can stand as a figure.");
            }
        }
    }
}
