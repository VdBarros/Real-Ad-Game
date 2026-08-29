using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class WorldMaterials : IDisposable
    {
        public const string NamePrefix = "World_";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        public const float Smoothness = 0f;

        static readonly string[] shaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Standard",
            "Unlit/Color"
        };

        public static IReadOnlyList<string> ShaderNames
        {
            get { return shaderNames; }
        }

        readonly Material[] byStyle;

        readonly WorldModels models;

        Shader shader;

        bool disposed;

        public WorldMaterials()
            : this(null)
        {
        }

        public WorldMaterials(WorldModels dressing)
        {
            byStyle = new Material[Enum.GetValues(typeof(PartStyle)).Length];
            models = dressing;
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

        public static bool ShowsItsPack(PartStyle style)
        {
            return CharacterCast.IsRole(style);
        }

        public static Color ColourFor(PartStyle style, bool textured)
        {
            return textured && ShowsItsPack(style) ? Color.white : WorldPalette.Of(style);
        }

        Material Create(PartStyle style)
        {
            var material = new Material(LitShader())
            {
                name = NamePrefix + style,
                hideFlags = HideFlags.HideAndDontSave
            };

            var atlas = AtlasFor(style);
            var colour = ColourFor(style, atlas != null);

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, colour);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, colour);
            }

            if (material.HasProperty(SmoothnessId))
            {
                material.SetFloat(SmoothnessId, Smoothness);
            }

            Texture(material, atlas);

            return material;
        }

        Texture2D AtlasFor(PartStyle style)
        {
            if (models == null || !models.Dresses(style))
            {
                return null;
            }

            return models.AtlasFor(PartModels.Of(style));
        }

        static void Texture(Material material, Texture2D atlas)
        {
            if (atlas == null)
            {
                return;
            }

            if (material.HasProperty(BaseMapId))
            {
                material.SetTexture(BaseMapId, atlas);
            }

            if (material.HasProperty(MainTexId))
            {
                material.SetTexture(MainTexId, atlas);
            }
        }

        Shader LitShader()
        {
            if (shader != null)
            {
                return shader;
            }

            foreach (var candidate in shaderNames)
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
