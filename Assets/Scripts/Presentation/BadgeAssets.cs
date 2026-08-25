using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class BadgeAssets : IDisposable
    {
        public const string NamePrefix = "Badge_";

        static readonly string[] ShaderNames =
        {
            "Universal Render Pipeline/2D/Sprite-Unlit-Default",
            "Sprites/Default",
            "Unlit/Transparent"
        };

        readonly Sprite[] byShape;

        Texture2D texture;

        Material material;

        bool disposed;

        public BadgeAssets()
        {
            byShape = new Sprite[Enum.GetValues(typeof(BadgeShape)).Length];
        }

        public Material Material
        {
            get
            {
                RequireOpen();
                if (material == null)
                {
                    material = new Material(BadgeShader())
                    {
                        name = NamePrefix + "Material",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }

                return material;
            }
        }

        public Sprite Of(BadgeShape shape)
        {
            RequireOpen();

            var slot = (int)shape;
            if (byShape[slot] == null)
            {
                byShape[slot] = Cut(shape);
            }

            return byShape[slot];
        }

        Sprite Cut(BadgeShape shape)
        {
            var border = BadgeShapeField.BorderOf(shape);
            var sprite = Sprite.Create(
                Texture(),
                new Rect(
                    BadgeShapeField.OriginX(shape),
                    0f,
                    BadgeShapeField.CellPixels,
                    BadgeShapeField.CellPixels),
                new Vector2(0.5f, 0.5f),
                BadgeShapeField.PixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));

            sprite.name = NamePrefix + shape;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        Texture2D Texture()
        {
            if (texture != null)
            {
                return texture;
            }

            texture = new Texture2D(
                BadgeShapeField.TextureWidth, BadgeShapeField.TextureHeight, TextureFormat.RGBA32, false, false)
            {
                name = NamePrefix + "Backgrounds",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[BadgeShapeField.TextureWidth * BadgeShapeField.TextureHeight];
            foreach (BadgeShape shape in Enum.GetValues(typeof(BadgeShape)))
            {
                var originX = BadgeShapeField.OriginX(shape);
                for (var y = 0; y < BadgeShapeField.CellPixels; y++)
                {
                    for (var x = 0; x < BadgeShapeField.CellPixels; x++)
                    {
                        var alpha = (byte)Mathf.RoundToInt(BadgeShapeField.Coverage(shape, x, y) * 255f);
                        pixels[y * BadgeShapeField.TextureWidth + originX + x] = new Color32(255, 255, 255, alpha);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        static Shader BadgeShader()
        {
            foreach (var candidate in ShaderNames)
            {
                var shader = Shader.Find(candidate);
                if (shader != null)
                {
                    return shader;
                }
            }

            throw new InvalidOperationException("No sprite shader is available to draw a badge background with.");
        }

        void RequireOpen()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BadgeAssets));
            }
        }

        public void Dispose()
        {
            for (var slot = 0; slot < byShape.Length; slot++)
            {
                WorldObjects.Destroy(byShape[slot]);
                byShape[slot] = null;
            }

            WorldObjects.Destroy(material);
            material = null;

            WorldObjects.Destroy(texture);
            texture = null;

            disposed = true;
        }
    }
}
