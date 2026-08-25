using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class WorldPalette
    {
        public static Color Of(PartStyle style)
        {
            switch (style)
            {
                case PartStyle.Floor:
                    return new Color(0.24f, 0.25f, 0.30f);
                case PartStyle.Cleared:
                    return new Color(0.87f, 0.89f, 0.83f);
                case PartStyle.Wall:
                    return new Color(0.44f, 0.46f, 0.53f);
                case PartStyle.Ramp:
                    return new Color(0.66f, 0.52f, 0.30f);
                case PartStyle.Start:
                    return new Color(0.20f, 0.75f, 0.35f);
                case PartStyle.Enemy:
                    return new Color(0.85f, 0.22f, 0.22f);
                case PartStyle.Boss:
                    return new Color(0.55f, 0.08f, 0.12f);
                case PartStyle.Additive:
                    return new Color(0.25f, 0.55f, 0.95f);
                case PartStyle.Multiplier:
                    return new Color(0.30f, 0.82f, 0.93f);
                case PartStyle.Trail:
                    return new Color(0.97f, 0.93f, 0.55f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No colour for that style.");
            }
        }
    }
}
