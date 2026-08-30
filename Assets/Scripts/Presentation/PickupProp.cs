using System;
using Game.Presentation.Pure;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Presentation
{
    [ExecuteAlways]
    public sealed class PickupProp : MonoBehaviour
    {
        public const string LidNode = "chest_lid";

        public const string FadingPrefix = PartNames.FadingPrefix;

        const float HingeSense = -1f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly int SurfaceId = Shader.PropertyToID("_Surface");

        static readonly int BlendId = Shader.PropertyToID("_Blend");

        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");

        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        static readonly int ModeId = Shader.PropertyToID("_Mode");

        Transform badge;

        Renderer[] skins;

        Material plain;

        Material thinning;

        Vector3 shut;

        bool begun;

        public int NodeId { get; private set; }

        public Take Reel { get; private set; }

        public Transform Lid { get; private set; }

        public bool IsSpent
        {
            get { return Reel.IsSpent; }
        }

        public bool Draws
        {
            get
            {
                if (skins == null)
                {
                    return false;
                }

                foreach (var skin in skins)
                {
                    if (skin != null && skin.enabled && skin.gameObject.activeInHierarchy)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal void Begin(WorldPart part, int nodeId, Transform hangingBadge, bool wearsAMesh)
        {
            skins = GetComponentsInChildren<Renderer>(true);
            badge = hangingBadge;
            NodeId = nodeId;
            plain = skins.Length == 0 ? null : skins[0].sharedMaterial;
            Lid = wearsAMesh ? Hinge(transform) : null;
            shut = Lid == null ? Vector3.zero : Lid.localEulerAngles;
            begun = true;

            Wear(Take.None);
        }

        public void Wear(Take reel)
        {
            if (!begun)
            {
                throw new InvalidOperationException(
                    "A pickup opens a lid it has not been given. Call Begin.");
            }

            Reel = reel;

            if (Lid != null)
            {
                Lid.localEulerAngles = new Vector3(
                    shut.x + reel.LidSwing * HingeSense, shut.y, shut.z);
            }

            if (!reel.IsSpent)
            {
                return;
            }

            if (!reel.IsSettled)
            {
                Thin(reel.Opacity);
                return;
            }

            Leave();

            if (badge != null)
            {
                badge.gameObject.SetActive(false);
            }
        }

        void Thin(float opacity)
        {
            if (opacity >= 1f || plain == null)
            {
                return;
            }

            if (thinning == null)
            {
                thinning = Fading(plain);

                foreach (var skin in skins)
                {
                    if (skin != null)
                    {
                        skin.sharedMaterial = thinning;
                    }
                }
            }

            Alpha(thinning, opacity);
        }

        void Leave()
        {
            foreach (var skin in skins)
            {
                if (skin == null)
                {
                    continue;
                }

                skin.enabled = false;

                if (plain != null)
                {
                    skin.sharedMaterial = plain;
                }
            }

            if (thinning != null)
            {
                WorldObjects.Destroy(thinning);
                thinning = null;
            }

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (thinning == null)
            {
                return;
            }

            WorldObjects.Destroy(thinning);
            thinning = null;
        }

        static Transform Hinge(Transform root)
        {
            foreach (var joint in root.GetComponentsInChildren<Transform>(true))
            {
                if (joint != root
                    && joint.name.IndexOf(LidNode, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return joint;
                }
            }

            return null;
        }

        static Material Fading(Material solid)
        {
            var copy = new Material(solid)
            {
                name = PartNames.Fading(solid.name),
                hideFlags = HideFlags.HideAndDontSave
            };

            copy.SetOverrideTag("RenderType", "Transparent");

            Set(copy, SurfaceId, 1f);
            Set(copy, BlendId, 0f);
            Set(copy, ModeId, 2f);
            Set(copy, SrcBlendId, (float)BlendMode.SrcAlpha);
            Set(copy, DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            Set(copy, ZWriteId, 0f);

            copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            copy.EnableKeyword("_ALPHABLEND_ON");
            copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            copy.DisableKeyword("_ALPHATEST_ON");
            copy.renderQueue = (int)RenderQueue.Transparent;

            return copy;
        }

        static void Set(Material material, int property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        static void Alpha(Material material, float opacity)
        {
            Alpha(material, BaseColorId, opacity);
            Alpha(material, ColorId, opacity);
        }

        static void Alpha(Material material, int property, float opacity)
        {
            if (!material.HasProperty(property))
            {
                return;
            }

            var colour = material.GetColor(property);
            colour.a = opacity;
            material.SetColor(property, colour);
        }
    }
}
