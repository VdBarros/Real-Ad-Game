using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class BadgeStyles
    {
        public const string AdditivePrefix = "+";

        public const string MultiplierPrefix = "x";

        public const string NoPrefix = "";

        public static bool TryOf(NodeType type, out BadgeStyle style)
        {
            switch (type)
            {
                case NodeType.Start:
                    style = BadgeStyle.Player;
                    return true;
                case NodeType.Additive:
                    style = BadgeStyle.Additive;
                    return true;
                case NodeType.Multiplier:
                    style = BadgeStyle.Multiplier;
                    return true;
                case NodeType.Enemy:
                    style = BadgeStyle.Enemy;
                    return true;
                case NodeType.Boss:
                    style = BadgeStyle.Boss;
                    return true;
                default:
                    style = BadgeStyle.Player;
                    return false;
            }
        }

        public static BadgeShape ShapeOf(BadgeStyle style)
        {
            switch (style)
            {
                case BadgeStyle.Player:
                case BadgeStyle.Additive:
                case BadgeStyle.Multiplier:
                    return BadgeShape.RoundedRect;
                case BadgeStyle.Enemy:
                case BadgeStyle.Boss:
                    return BadgeShape.Pill;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No shape for that badge style.");
            }
        }

        public static string Prefix(BadgeStyle style)
        {
            switch (style)
            {
                case BadgeStyle.Additive:
                    return AdditivePrefix;
                case BadgeStyle.Multiplier:
                    return MultiplierPrefix;
                case BadgeStyle.Player:
                case BadgeStyle.Enemy:
                case BadgeStyle.Boss:
                    return NoPrefix;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "No prefix for that badge style.");
            }
        }
    }
}
