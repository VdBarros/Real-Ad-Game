using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [ExecuteAlways]
    public sealed class GateProp : MonoBehaviour
    {
        public const string NamePrefix = "Gate_";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly string[] shaderNames =
        {
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Universal Render Pipeline/Lit",
            "Standard"
        };

        public static IReadOnlyList<string> ShaderNames
        {
            get { return shaderNames; }
        }

        Renderer[] skins;

        Material glow;

        public int Factor { get; private set; }

        public Color Plain { get; private set; }

        public Tint Tint { get; private set; }

        public int Pips { get; private set; }

        public Material Glow
        {
            get { return glow; }
        }

        public Color Colour
        {
            get
            {
                if (glow == null)
                {
                    return Color.black;
                }

                if (glow.HasProperty(BaseColorId))
                {
                    return glow.GetColor(BaseColorId);
                }

                return glow.HasProperty(ColorId) ? glow.GetColor(ColorId) : Color.black;
            }
        }

        internal void Begin(int factor, IReadOnlyList<WorldPart> pieces)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            Factor = factor;
            Pips = GateArch.PipsOn(pieces);
            Tint = GateLook.Of(factor);
            Plain = Tints.Of(Tint);
            skins = GetComponentsInChildren<Renderer>(true);

            glow = new Material(GlowShader())
            {
                name = NamePrefix + factor,
                hideFlags = HideFlags.HideAndDontSave
            };

            Wash(Plain);

            foreach (var skin in skins)
            {
                if (skin != null)
                {
                    skin.sharedMaterial = glow;
                }
            }
        }

        public void Wash(Color colour)
        {
            if (glow == null)
            {
                throw new InvalidOperationException(
                    "A gate glows in a colour it has not been given. Call Begin.");
            }

            if (glow.HasProperty(BaseColorId))
            {
                glow.SetColor(BaseColorId, colour);
            }

            if (glow.HasProperty(ColorId))
            {
                glow.SetColor(ColorId, colour);
            }
        }

        void OnDestroy()
        {
            if (glow == null)
            {
                return;
            }

            WorldObjects.Destroy(glow);
            glow = null;
        }

        static Shader GlowShader()
        {
            foreach (var candidate in shaderNames)
            {
                var shader = Shader.Find(candidate);
                if (shader != null)
                {
                    return shader;
                }
            }

            throw new InvalidOperationException("No shader is available to light a gate arch with.");
        }
    }
}
