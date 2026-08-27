using System;

namespace Game.Presentation.Pure
{
    public static class ModelPose
    {
        public static WorldPoint RotationOf(WorldPart part)
        {
            switch (part.Model)
            {
                case PartModel.FloorTile:
                    return new WorldPoint(0f, part.Rotation.Y, 0f);
                case PartModel.None:
                case PartModel.WallPanel:
                case PartModel.Chest:
                case PartModel.Candles:
                    return part.Rotation;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Model, "No model pose for that part model.");
            }
        }

        public static WorldPoint ScaleOf(WorldPart part)
        {
            switch (part.Model)
            {
                case PartModel.FloorTile:
                    return new WorldPoint(part.Scale.X, part.Scale.Z, part.Scale.Y);
                case PartModel.None:
                case PartModel.WallPanel:
                case PartModel.Chest:
                case PartModel.Candles:
                    return part.Scale;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Model, "No model scale for that part model.");
            }
        }
    }
}
