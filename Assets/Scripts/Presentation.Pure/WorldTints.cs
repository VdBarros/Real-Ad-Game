using System;

namespace Game.Presentation.Pure
{
    public static class WorldTints
    {
        public const float LeastSeparation = 3f;

        public const float LeastClearedChromaLift = 3f;

        public const float MostClearedValueShift = 1.15f;

        public const float SharedFloorHue = 0.5f;

        public static Tint Of(PartStyle style)
        {
            switch (style)
            {
                case PartStyle.Floor:
                    return new Tint(0.93f, 0.90375f, 0.86f);
                case PartStyle.Cleared:
                    return new Tint(0.98f, 0.8975f, 0.76f);
                case PartStyle.Wall:
                    return new Tint(0.89f, 0.9f, 0.93f);
                case PartStyle.Pillar:
                    return new Tint(0.46f, 0.34f, 0.16f);
                case PartStyle.Start:
                    return new Tint(0.09f, 0.36f, 0.16f);
                case PartStyle.Enemy:
                    return new Tint(0.6f, 0.09f, 0.09f);
                case PartStyle.Boss:
                    return new Tint(0.34f, 0.04f, 0.07f);
                case PartStyle.Additive:
                    return new Tint(0.13f, 0.3f, 0.72f);
                case PartStyle.Multiplier:
                    return new Tint(0.05f, 0.36f, 0.44f);
                case PartStyle.Landmark:
                    return new Tint(0.34f, 0.33f, 0.3f);
                case PartStyle.Staircase:
                    return new Tint(0.89f, 0.9f, 0.93f);
                case PartStyle.Foundation:
                    return new Tint(0.83f, 0.84f, 0.87f);
                case PartStyle.Trail:
                    return new Tint(0.72f, 0.5f, 0.06f);
                case PartStyle.Spark:
                    return new Tint(1f, 0.97f, 0.86f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No colour for that style.");
            }
        }

        public static PartLayer LayerOf(PartStyle style)
        {
            switch (style)
            {
                case PartStyle.Floor:
                case PartStyle.Cleared:
                case PartStyle.Wall:
                case PartStyle.Staircase:
                case PartStyle.Foundation:
                    return PartLayer.Surface;
                case PartStyle.Start:
                case PartStyle.Enemy:
                case PartStyle.Boss:
                case PartStyle.Additive:
                case PartStyle.Multiplier:
                case PartStyle.Pillar:
                case PartStyle.Landmark:
                    return PartLayer.Figure;
                case PartStyle.Trail:
                case PartStyle.Spark:
                    return PartLayer.Mark;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No layer for that style.");
            }
        }
    }
}
