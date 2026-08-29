using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Presentation
{
    public static class Ghosting
    {
        public const string NamePrefix = "Ghost_";

        public const float Alpha = 0.55f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly int SurfaceId = Shader.PropertyToID("_Surface");

        static readonly int BlendId = Shader.PropertyToID("_Blend");

        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");

        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");

        static readonly int ModeId = Shader.PropertyToID("_Mode");

        public static Material[] Raise(GameObject figure)
        {
            if (figure == null)
            {
                return new Material[0];
            }

            var raised = new List<Material>();

            foreach (var skin in figure.GetComponentsInChildren<Renderer>(true))
            {
                var worn = skin.sharedMaterials;
                if (worn == null || worn.Length == 0)
                {
                    continue;
                }

                var haunted = new Material[worn.Length];

                for (var slot = 0; slot < worn.Length; slot++)
                {
                    if (worn[slot] == null)
                    {
                        continue;
                    }

                    haunted[slot] = Translucent(worn[slot]);
                    raised.Add(haunted[slot]);
                }

                skin.sharedMaterials = haunted;
                skin.shadowCastingMode = ShadowCastingMode.Off;
            }

            return raised.ToArray();
        }

        public static void Fade(Material[] ghosts, float fade)
        {
            if (ghosts == null)
            {
                return;
            }

            var alpha = Alpha * (fade < 0f ? 0f : fade > 1f ? 1f : fade);

            foreach (var ghost in ghosts)
            {
                if (ghost == null)
                {
                    continue;
                }

                Alphaed(ghost, BaseColorId, alpha);
                Alphaed(ghost, ColorId, alpha);
            }
        }

        public static float AlphaOf(Material[] ghosts)
        {
            if (ghosts == null)
            {
                return 1f;
            }

            var lit = 0f;

            foreach (var ghost in ghosts)
            {
                if (ghost == null || !ghost.HasProperty(BaseColorId))
                {
                    continue;
                }

                var alpha = ghost.GetColor(BaseColorId).a;
                if (alpha > lit)
                {
                    lit = alpha;
                }
            }

            return lit;
        }

        public static void Lay(Material[] ghosts)
        {
            if (ghosts == null)
            {
                return;
            }

            foreach (var ghost in ghosts)
            {
                WorldObjects.Destroy(ghost);
            }
        }

        public static bool IsGhost(Material material)
        {
            return material != null
                && material.name.StartsWith(NamePrefix, StringComparison.Ordinal);
        }

        static Material Translucent(Material worn)
        {
            var ghost = new Material(worn)
            {
                name = NamePrefix + worn.name,
                hideFlags = HideFlags.HideAndDontSave
            };

            Set(ghost, SurfaceId, 1f);
            Set(ghost, BlendId, 0f);
            Set(ghost, SrcBlendId, (float)BlendMode.SrcAlpha);
            Set(ghost, DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            Set(ghost, ZWriteId, 0f);
            Set(ghost, AlphaClipId, 0f);
            Set(ghost, ModeId, 3f);

            ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghost.EnableKeyword("_ALPHABLEND_ON");
            ghost.DisableKeyword("_ALPHATEST_ON");
            ghost.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghost.SetShaderPassEnabled("ShadowCaster", false);
            ghost.renderQueue = (int)RenderQueue.Transparent;

            Alphaed(ghost, BaseColorId, Alpha);
            Alphaed(ghost, ColorId, Alpha);

            return ghost;
        }

        static void Set(Material ghost, int property, float value)
        {
            if (ghost.HasProperty(property))
            {
                ghost.SetFloat(property, value);
            }
        }

        static void Alphaed(Material ghost, int property, float alpha)
        {
            if (!ghost.HasProperty(property))
            {
                return;
            }

            var colour = ghost.GetColor(property);
            ghost.SetColor(property, new Color(colour.r, colour.g, colour.b, alpha));
        }
    }
}
