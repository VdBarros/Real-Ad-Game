using System;

namespace Game.Presentation.Pure
{
    public static class FigureRim
    {
        public const float ContourPixels = 6f;

        public const float LeastSeparation = 3f;

        public static float ShareOfScreen
        {
            get { return ContourPixels / ScreenFrame.Height; }
        }

        public static float Width
        {
            get { return LevelFraming.HeightShowing(ShareOfScreen, LevelFraming.PlaySize); }
        }

        public static Tint Contour
        {
            get { return new Tint(0.04f, 0.04f, 0.05f); }
        }

        public static bool Contours(PartStyle style)
        {
            return CharacterCast.IsRole(style);
        }

        public static float PixelsAt(float orthographicSize)
        {
            if (orthographicSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orthographicSize), orthographicSize, "A framing always has a positive size.");
            }

            return LevelFraming.ShareOfScreen(Width, orthographicSize) * ScreenFrame.Height;
        }

        public static float SeparationFrom(Tint tint)
        {
            return Tint.Contrast(Contour, tint);
        }
    }
}
