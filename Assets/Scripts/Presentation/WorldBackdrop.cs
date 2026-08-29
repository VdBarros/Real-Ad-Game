using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Presentation
{
    public sealed class WorldBackdrop : MonoBehaviour
    {
        public const string MaterialName = "World_Backdrop";

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static readonly int CullId = Shader.PropertyToID("_Cull");

        static readonly string[] shaderNames =
        {
            "Universal Render Pipeline/Unlit",
            "Unlit/Texture",
            "Sprites/Default"
        };

        public static IReadOnlyList<string> ShaderNames
        {
            get { return shaderNames; }
        }

        Material skin;

        Texture2D ramp;

        Mesh sheet;

        MeshRenderer face;

        public Material Skin
        {
            get { return skin; }
        }

        public Texture2D Ramp
        {
            get { return ramp; }
        }

        public Mesh Sheet
        {
            get { return sheet; }
        }

        public MeshRenderer Face
        {
            get { return face; }
        }

        public static WorldBackdrop Hang(Camera lens)
        {
            if (lens == null)
            {
                throw new ArgumentNullException(nameof(lens));
            }

            Room();

            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = Tints.Of(Backdrop.Clear);

            var carrier = new GameObject(PartNames.Backdrop);
            carrier.transform.SetParent(lens.transform, false);
            carrier.transform.localPosition = new Vector3(0f, 0f, Backdrop.Reach);

            var hung = carrier.AddComponent<WorldBackdrop>();
            hung.Dress();
            hung.Fit(lens.orthographicSize);

            return hung;
        }

        public static void Room()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Tints.Of(Backdrop.AmbientSky);
            RenderSettings.ambientEquatorColor = Tints.Of(Backdrop.AmbientEquator);
            RenderSettings.ambientGroundColor = Tints.Of(Backdrop.AmbientGround);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = Backdrop.ReflectionStrength;
            RenderSettings.fog = false;
        }

        public void Fit(float orthographicSize)
        {
            var height = orthographicSize * 2f * Backdrop.Overscan;

            transform.localScale = new Vector3(height * Backdrop.WidthOverHeight, height, 1f);
        }

        void Dress()
        {
            sheet = Sheeting();
            ramp = Grading();
            skin = Skinning();

            gameObject.AddComponent<MeshFilter>().sharedMesh = sheet;

            face = gameObject.AddComponent<MeshRenderer>();
            face.sharedMaterial = skin;
            face.shadowCastingMode = ShadowCastingMode.Off;
            face.receiveShadows = false;
            face.lightProbeUsage = LightProbeUsage.Off;
            face.reflectionProbeUsage = ReflectionProbeUsage.Off;
            face.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            face.allowOcclusionWhenDynamic = false;
        }

        static Mesh Sheeting()
        {
            var back = new Vector3(0f, 0f, -1f);
            var made = new Mesh
            {
                name = PartNames.Backdrop,
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                normals = new[] { back, back, back, back }
            };

            made.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            made.RecalculateBounds();

            return made;
        }

        static Texture2D Grading()
        {
            var made = new Texture2D(1, Backdrop.RampBands, TextureFormat.RGB24, false, false)
            {
                name = PartNames.Backdrop,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };

            var bands = new Color[Backdrop.RampBands];
            for (var band = 0; band < bands.Length; band++)
            {
                bands[band] = Tints.Of(Backdrop.At(Backdrop.BandHeight(band)));
            }

            made.SetPixels(bands);
            made.Apply(false, false);

            return made;
        }

        Material Skinning()
        {
            var made = new Material(UnlitShader())
            {
                name = MaterialName,
                hideFlags = HideFlags.HideAndDontSave
            };

            if (made.HasProperty(BaseMapId))
            {
                made.SetTexture(BaseMapId, ramp);
            }

            if (made.HasProperty(MainTexId))
            {
                made.SetTexture(MainTexId, ramp);
            }

            if (made.HasProperty(BaseColorId))
            {
                made.SetColor(BaseColorId, Color.white);
            }

            if (made.HasProperty(ColorId))
            {
                made.SetColor(ColorId, Color.white);
            }

            if (made.HasProperty(CullId))
            {
                made.SetFloat(CullId, (float)CullMode.Off);
            }

            return made;
        }

        static Shader UnlitShader()
        {
            foreach (var candidate in shaderNames)
            {
                var found = Shader.Find(candidate);
                if (found != null)
                {
                    return found;
                }
            }

            throw new InvalidOperationException("No unlit shader is available to grade the backdrop with.");
        }

        void OnDestroy()
        {
            WorldObjects.Destroy(skin);
            WorldObjects.Destroy(ramp);
            WorldObjects.Destroy(sheet);

            skin = null;
            ramp = null;
            sheet = null;
        }
    }
}
