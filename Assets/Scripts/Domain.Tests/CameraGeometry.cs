using System;
using Game.Presentation.Pure;

namespace Game.Domain.Tests
{
    static class CameraGeometry
    {
        public const int FrameWidth = 1080;

        public const int FrameHeight = 1920;

        public const float PanBudget = 1000f;

        public static float Dot(WorldPoint first, WorldPoint second)
        {
            return first.X * second.X + first.Y * second.Y + first.Z * second.Z;
        }

        public static float PixelsPerMetre(float orthographicSize)
        {
            return FrameHeight * 0.5f / orthographicSize;
        }

        public static float PanPixels(CameraFraming from, CameraFraming to)
        {
            var delta = new WorldPoint(
                to.Target.X - from.Target.X,
                to.Target.Y - from.Target.Y,
                to.Target.Z - from.Target.Z);

            var across = Dot(delta, IsoProjection.CameraRight);
            var along = Dot(delta, IsoProjection.CameraUp);
            var metres = (float)Math.Sqrt(across * across + along * along);

            return metres * PixelsPerMetre(Math.Min(from.OrthographicSize, to.OrthographicSize));
        }

        public static float Depth(CameraFraming framing, WorldPoint point)
        {
            var camera = framing.Position;
            var offset = new WorldPoint(point.X - camera.X, point.Y - camera.Y, point.Z - camera.Z);
            return Dot(offset, IsoProjection.CameraForward);
        }
    }
}
