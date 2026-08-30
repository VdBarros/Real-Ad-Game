using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [ExecuteAlways]
    public sealed class LandmarkProp : MonoBehaviour
    {
        public const string NamePrefix = "Landmark_";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        readonly List<Material> coats = new List<Material>();

        public LandmarkKind Kind { get; private set; }

        public int Pieces { get; private set; }

        public bool Textured { get; private set; }

        public Tint Tint { get; private set; }

        public IReadOnlyList<Material> Coats
        {
            get { return coats; }
        }

        internal void Begin(LandmarkKind kind, IReadOnlyList<WorldPart> pieces, Texture2D atlas)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            Kind = kind;
            Pieces = pieces.Count;
            Tint = LandmarkLook.Of(kind);
            Textured = atlas != null;

            var coat = Coat(kind, Tint, atlas);

            foreach (var piece in pieces)
            {
                var block = transform.Find(piece.Name);

                if (block == null)
                {
                    throw new InvalidOperationException(
                        "A " + kind + " landmark is missing the piece named " + piece.Name + ".");
                }

                foreach (var skin in block.GetComponentsInChildren<Renderer>(true))
                {
                    skin.sharedMaterial = coat;
                }
            }

            coats.Clear();
            coats.Add(coat);
        }

        Material Coat(LandmarkKind kind, Tint tint, Texture2D atlas)
        {
            var material = new Material(LitShader())
            {
                name = NamePrefix + kind,
                hideFlags = HideFlags.HideAndDontSave
            };

            var colour = Presentation.Tints.Of(tint);

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
                material.SetFloat(SmoothnessId, WorldMaterials.Smoothness);
            }

            if (atlas != null && material.HasProperty(BaseMapId))
            {
                material.SetTexture(BaseMapId, atlas);
            }

            if (atlas != null && material.HasProperty(MainTexId))
            {
                material.SetTexture(MainTexId, atlas);
            }

            return material;
        }

        void OnDestroy()
        {
            foreach (var coat in coats)
            {
                WorldObjects.Destroy(coat);
            }

            coats.Clear();
        }

        static Shader LitShader()
        {
            foreach (var candidate in WorldMaterials.ShaderNames)
            {
                var shader = Shader.Find(candidate);
                if (shader != null)
                {
                    return shader;
                }
            }

            throw new InvalidOperationException("No shader is available to colour a landmark with.");
        }
    }
}
