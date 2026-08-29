using System;

namespace Game.Presentation.Pure
{
    public static class FigureReadability
    {
        public const float ReadablePixels = 96f;

        public static float ShareOfScreen
        {
            get { return ReadablePixels / ScreenFrame.Height; }
        }

        public static float Height
        {
            get { return LevelFraming.HeightShowing(ShareOfScreen, LevelFraming.PlaySize); }
        }

        public static float ScaleOf(PartModel model)
        {
            return Height / FigureFit.StandingScalesOf(model);
        }

        public static float ShareShowing(PartModel model, float figureScale)
        {
            return LevelFraming.ShareOfScreen(
                FigureFit.StandingHeight(model, figureScale), LevelFraming.PlaySize);
        }

        public static float PixelsShowing(PartModel model, float figureScale)
        {
            return ShareShowing(model, figureScale) * ScreenFrame.Height;
        }

        public static bool Reads(PartModel model, float figureScale)
        {
            if (figureScale <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(figureScale), figureScale, "A figure always stands at a positive scale.");
            }

            return ShareShowing(model, figureScale) >= ShareOfScreen;
        }
    }
}
