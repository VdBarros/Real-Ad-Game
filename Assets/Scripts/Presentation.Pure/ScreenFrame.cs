using System;

namespace Game.Presentation.Pure
{
    public static class ScreenFrame
    {
        public const int Width = 1080;

        public const int Height = 1920;

        public const float PanCeiling = 1000f;

        public static float PixelsPerMetre(float orthographicSize)
        {
            if (orthographicSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orthographicSize), orthographicSize, "A framing always has a positive size.");
            }

            return Height * 0.5f / orthographicSize;
        }

        public static float TileGroundPixels(float orthographicSize)
        {
            var perMetre = PixelsPerMetre(orthographicSize);
            var facing = Math.Abs(IsoProjection.CameraForward.Y);

            return perMetre * perMetre * IsoProjection.TileEdge * IsoProjection.TileEdge * facing;
        }

        public static float PanPixels(CameraFraming from, CameraFraming to)
        {
            var delta = new WorldPoint(
                to.Target.X - from.Target.X,
                to.Target.Y - from.Target.Y,
                to.Target.Z - from.Target.Z);

            var across = WorldPoint.Dot(delta, IsoProjection.CameraRight);
            var along = WorldPoint.Dot(delta, IsoProjection.CameraUp);
            var metres = (float)Math.Sqrt(across * across + along * along);

            return metres * PixelsPerMetre(Math.Min(from.OrthographicSize, to.OrthographicSize));
        }
    }
}
