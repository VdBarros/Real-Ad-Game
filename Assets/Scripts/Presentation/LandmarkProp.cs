using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class LandmarkProp : MonoBehaviour
    {
        public const string NamePrefix = "Landmark_";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        readonly List<Material> coats = new List<Material>();

        public LandmarkKind Kind { get; private set; }

        public int Pieces { get; private set; }

        public IReadOnlyList<Material> Coats
        {
            get { return coats; }
        }

        public IReadOnlyList<Tint> Tints
        {
            get { return tints; }
        }

        Tint[] tints = Array.Empty<Tint>();

        internal void Begin(LandmarkKind kind, IReadOnlyList<LandmarkPiece> pieces)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            Kind = kind;
            Pieces = pieces.Count;

            var worn = new List<Tint>();
            var painted = new List<Material>();

            foreach (var piece in pieces)
            {
                var slot = worn.IndexOf(piece.Tint);

                if (slot < 0)
                {
                    worn.Add(piece.Tint);
                    painted.Add(Coat(kind, worn.Count - 1, piece.Tint));
                    slot = worn.Count - 1;
                }

                var block = transform.Find(piece.Part.Name);

                if (block == null)
                {
                    throw new InvalidOperationException(
                        "A " + kind + " landmark is missing the piece named " + piece.Part.Name + ".");
                }

                foreach (var skin in block.GetComponentsInChildren<Renderer>(true))
                {
                    skin.sharedMaterial = painted[slot];
                }
            }

            tints = worn.ToArray();
            coats.Clear();
            coats.AddRange(painted);
        }

        Material Coat(LandmarkKind kind, int slot, Tint tint)
        {
            var material = new Material(LitShader())
            {
                name = NamePrefix + kind + "_" + slot,
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
