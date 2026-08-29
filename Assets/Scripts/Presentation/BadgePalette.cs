using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class BadgePalette
    {
        public static readonly Color Text = Tints.Of(BadgeTints.Text);

        public static Color Of(BadgeStyle style)
        {
            return Tints.Of(BadgeTints.Of(style));
        }
    }
}
