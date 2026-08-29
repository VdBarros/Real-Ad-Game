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

            return ArtPacks.StandingScalesOf(model);
        }

        public static float ScaleOf(PartModel model)
        {
            if (model == PartModel.None)
            {
                return 1f;
            }

            Guard(model);

            return StandingScalesOf(model) / ArtPacks.HeightOf(model);
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

            return ArtPacks.WidthOf(model) * ScaleOf(model) * figureScale;
        }

        public static float DepthOf(PartModel model, float figureScale)
        {
            if (model == PartModel.None)
            {
                return figureScale;
            }

            Guard(model);

            return ArtPacks.DepthOf(model) * ScaleOf(model) * figureScale;
        }

        public static float SpreadOf(PartModel model, float figureScale)
        {
            var width = WidthOf(model, figureScale);
            var depth = DepthOf(model, figureScale);

            return model == PartModel.None
                ? figureScale
                : (float)Math.Sqrt(width * width + depth * depth);
        }

        public static float TileReachOf(PartModel model, float figureScale)
        {
            if (model == PartModel.None)
            {
                return figureScale;
            }

            var width = WidthOf(model, figureScale);
            var depth = DepthOf(model, figureScale);
            var turn = ArtPacks.FacingOf(model) * Math.PI / 180.0;

            return (float)(width * Math.Abs(Math.Cos(turn)) + depth * Math.Abs(Math.Sin(turn)));
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
            if (!ArtPacks.IsRiggedCharacter(model))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(model), model, "Only a cast mesh or a primitive can stand as a figure.");
            }
        }
    }
}
