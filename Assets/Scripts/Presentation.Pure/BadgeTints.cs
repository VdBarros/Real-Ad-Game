using System;

namespace Game.Presentation.Pure
{
    public static class BadgeTints
    {
        public static readonly Tint Text = new Tint(0.97f, 0.98f, 1f);

        public static Tint Of(BadgeStyle style)
        {
            switch (style)
            {
                case BadgeStyle.Player:
                    return new Tint(0.13f, 0.38f, 0.85f);
                case BadgeStyle.Additive:
                    return new Tint(0.15f, 0.70f, 0.40f);
                case BadgeStyle.Enemy:
                case BadgeStyle.Boss:
                    return new Tint(0.80f, 0.15f, 0.18f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No colour for that badge style.");
            }
        }
    }
}
