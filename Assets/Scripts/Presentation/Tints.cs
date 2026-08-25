using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class Tints
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static MaterialPropertyBlock block;

        public static Color Of(Tint tint)
        {
            return new Color(tint.Red, tint.Green, tint.Blue);
        }

        public static void Wash(Renderer skin, Tint tint)
        {
            if (skin == null)
            {
                return;
            }

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            skin.GetPropertyBlock(block);
            var colour = Of(tint);
            block.SetColor(BaseColorId, colour);
            block.SetColor(ColorId, colour);
            skin.SetPropertyBlock(block);
        }
    }
}
