using System;

namespace Game.Presentation.Pure
{
    public static class WorldParts
    {
        public static float TopOf(WorldPart part)
        {
            return part.Position.Y + HalfHeight(part);
        }

        public static float WidthOf(WorldPart part)
        {
            switch (part.Shape)
            {
                case PartShape.Capsule:
                    return FigureFit.WidthOf(part.Model, part.Scale.X);
                case PartShape.Cube:
                case PartShape.Quad:
                    return part.Scale.X;
                case PartShape.Gate:
                    return GateArch.Span * part.Scale.X;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Shape, "No width for that part shape.");
            }
        }

        static float HalfHeight(WorldPart part)
        {
            switch (part.Shape)
            {
                case PartShape.Capsule:
                    return part.Scale.Y;
                case PartShape.Cube:
                    return part.Scale.Y * 0.5f;
                case PartShape.Quad:
                    return 0f;
                case PartShape.Gate:
                    return GateArch.Height * part.Scale.Y * 0.5f;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(part), part.Shape, "No height for that part shape.");
            }
        }
    }
}
