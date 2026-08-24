using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class WorldMaterials : IDisposable
    {
        public const string NamePrefix = "World_";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly string[] ShaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Standard",
            "Unlit/Color"
        };

        readonly Material[] byStyle;

        Shader shader;

        bool disposed;

        public WorldMaterials()
        {
            byStyle = new Material[Enum.GetValues(typeof(PartStyle)).Length];
        }

        public Material Of(PartStyle style)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldMaterials));
            }

            var slot = (int)style;
            if (byStyle[slot] == null)
            {
                byStyle[slot] = Create(style);
            }

            return byStyle[slot];
        }

        Material Create(PartStyle style)
        {
            var material = new Material(LitShader())
            {
                name = NamePrefix + style,
                hideFlags = HideFlags.HideAndDontSave
            };

            var colour = WorldPalette.Of(style);
            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, colour);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, colour);
            }

            return material;
        }

        Shader LitShader()
        {
            if (shader != null)
            {
                return shader;
            }

            foreach (var candidate in ShaderNames)
            {
                shader = UnityEngine.Shader.Find(candidate);
                if (shader != null)
                {
                    return shader;
                }
            }

            throw new InvalidOperationException("No lit or unlit shader is available to colour the world with.");
        }

        public void Dispose()
        {
            for (var slot = 0; slot < byStyle.Length; slot++)
            {
                if (byStyle[slot] != null)
                {
                    WorldObjects.Destroy(byStyle[slot]);
                    byStyle[slot] = null;
                }
            }

            shader = null;
            disposed = true;
        }
    }
}
