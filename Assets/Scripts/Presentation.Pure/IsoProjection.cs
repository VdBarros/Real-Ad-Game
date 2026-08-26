using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class IsoProjection
    {
        public const float TileEdge = 1f;

        public const float StepHeight = 1f;

        public const float WallHeight = 1f;

        public const float CameraPitch = 30f;

        public const float CameraYaw = 45f;

        public const float CameraRoll = 0f;

        public const float OrthographicSize = 9.5f;

        public const float CameraBack = 20f;

        public const float NearPlane = 0.3f;

        public const float FarPlane = 40f;

        const double Radians = Math.PI / 180.0;

        static readonly WorldPoint Forward = new WorldPoint(
            (float)(Sin(CameraYaw) * Cos(CameraPitch)),
            (float)-Sin(CameraPitch),
            (float)(Cos(CameraYaw) * Cos(CameraPitch)));

        static readonly WorldPoint Right = new WorldPoint(
            (float)Cos(CameraYaw),
            0f,
            (float)-Sin(CameraYaw));

        static readonly WorldPoint Up = new WorldPoint(
            (float)(Sin(CameraYaw) * Sin(CameraPitch)),
            (float)Cos(CameraPitch),
            (float)(Cos(CameraYaw) * Sin(CameraPitch)));

        public static WorldPoint CameraRotation
        {
            get { return new WorldPoint(CameraPitch, CameraYaw, CameraRoll); }
        }

        public static WorldPoint CameraForward
        {
            get { return Forward; }
        }

        public static WorldPoint CameraRight
        {
            get { return Right; }
        }

        public static WorldPoint CameraUp
        {
            get { return Up; }
        }

        public static WorldPoint Of(TilePosition position)
        {
            return new WorldPoint(position.X * TileEdge, position.Elevation * StepHeight, position.Y * TileEdge);
        }

        static double Sin(float degrees)
        {
            return Math.Sin(degrees * Radians);
        }

        static double Cos(float degrees)
        {
            return Math.Cos(degrees * Radians);
        }
    }
}
