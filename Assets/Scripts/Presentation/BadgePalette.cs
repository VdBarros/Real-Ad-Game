using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class BadgePalette
    {
        public static readonly Color Text = new Color(0.97f, 0.98f, 1f);

        public static Color Of(BadgeStyle style)
        {
            switch (style)
            {
                case BadgeStyle.Player:
                case BadgeStyle.Additive:
                case BadgeStyle.Multiplier:
                    return new Color(0.13f, 0.38f, 0.85f);
                case BadgeStyle.Enemy:
                case BadgeStyle.Boss:
                    return new Color(0.80f, 0.15f, 0.18f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No colour for that badge style.");
            }
        }
    }
}
