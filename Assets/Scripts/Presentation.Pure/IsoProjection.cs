using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class IsoProjection
    {
        public const float TileEdge = 1f;

        public const float FloorHeight = 2f;

        public const float WallHeight = 1f;

        public const float CameraPitch = 30f;

        public const float CameraYaw = 45f;

        public const float CameraRoll = 0f;

        public const float OrthographicSize = 9.5f;

        public static WorldPoint CameraRotation
        {
            get { return new WorldPoint(CameraPitch, CameraYaw, CameraRoll); }
        }

        public static WorldPoint Of(TilePosition position)
        {
            return new WorldPoint(position.X * TileEdge, position.Floor * FloorHeight, position.Y * TileEdge);
        }
    }
}
