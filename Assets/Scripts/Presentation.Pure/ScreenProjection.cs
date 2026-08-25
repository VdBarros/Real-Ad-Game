using System;

namespace Game.Presentation.Pure
{
    public static class ScreenProjection
    {
        public static float PixelsPerMetre(float orthographicSize, int screenHeight)
        {
            if (orthographicSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orthographicSize), orthographicSize, "A framing always has a positive size.");
            }

            if (screenHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(screenHeight), screenHeight, "A screen always has pixels to spend.");
            }

            return screenHeight * 0.5f / orthographicSize;
        }

        public static ScreenPoint Of(CameraFraming framing, WorldPoint point, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(screenWidth), screenWidth, "A screen always has pixels to spend.");
            }

            var pixels = PixelsPerMetre(framing.OrthographicSize, screenHeight);
            var delta = new WorldPoint(
                point.X - framing.Target.X,
                point.Y - framing.Target.Y,
                point.Z - framing.Target.Z);

            return new ScreenPoint(
                screenWidth * 0.5f + WorldPoint.Dot(delta, IsoProjection.CameraRight) * pixels,
                screenHeight * 0.5f + WorldPoint.Dot(delta, IsoProjection.CameraUp) * pixels);
        }
    }
}
