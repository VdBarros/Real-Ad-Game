using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class WorldPalette
    {
        public static Color Of(PartStyle style)
        {
            return Tints.Of(WorldTints.Of(style));
        }
    }
}
