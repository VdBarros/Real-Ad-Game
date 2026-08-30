using System.Globalization;
using System.IO;
using Game.Flow;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTooling
{
    public static class PreviewFilm
    {
        public const string SunName = "PreviewSun";

        const float BandTolerance = 0.001f;

        public static void Shoot(Camera camera, string path)
        {
            var frame = Frame(camera);

            Save(frame, path);

            Object.DestroyImmediate(frame);
        }

        public static void Save(Texture2D frame, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, frame.EncodeToPNG());
        }

        public static void Warm(Camera camera)
        {
            Object.DestroyImmediate(Frame(camera));
        }

        public static Camera Rig(Vector3 centre, float distance, float orthographicSize)
        {
            return Rig(centre, distance, orthographicSize, IsoProjection.CameraYaw);
        }

        public static Camera Rig(Vector3 centre, float distance, float orthographicSize, float yaw)
        {
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, yaw, IsoProjection.CameraRoll);
            camera.transform.position = centre - camera.transform.forward * distance;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = distance * 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            return camera;
        }

        public static Texture2D Frame(Camera camera)
        {
            var aspect = camera.aspect;
            camera.aspect = (float)ScreenFrame.Width / ScreenFrame.Height;

            var target = new RenderTexture(
                ScreenFrame.Width,
                ScreenFrame.Height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            Render(camera, target);

            var frame = new Texture2D(ScreenFrame.Width, ScreenFrame.Height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            frame.ReadPixels(new Rect(0, 0, ScreenFrame.Width, ScreenFrame.Height), 0, 0);
            frame.Apply();
            RenderTexture.active = previous;

            camera.targetTexture = null;
            camera.aspect = aspect;
            target.Release();
            Object.DestroyImmediate(target);

            return frame;
        }

        public static void Sun()
        {
            Sunlight();
            Room();
        }

        public static void Room()
        {
            WorldBackdrop.Room();
        }

        public static string RoomAsPhotographed()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} ambient from {1} sky through {2} to {3} ground, reflection {4:0.###} off {5}, fog {6}",
                RenderSettings.ambientMode,
                Lit(RenderSettings.ambientSkyColor),
                Lit(RenderSettings.ambientEquatorColor),
                Lit(RenderSettings.ambientGroundColor),
                RenderSettings.reflectionIntensity,
                RenderSettings.skybox == null ? "no skybox" : RenderSettings.skybox.name,
                RenderSettings.fog ? "on" : "off");
        }

        public static string RoomApartFromTheBuild()
        {
            if (RenderSettings.skybox != null)
            {
                return "the scene still carries the skybox " + RenderSettings.skybox.name
                    + ", which lights and reflects off every pack material";
            }

            if (RenderSettings.customReflectionTexture != null)
            {
                return "the scene reflects off a cubemap the dungeon never stood in";
            }

            if (RenderSettings.reflectionIntensity > Backdrop.ReflectionStrength)
            {
                return "a reflection probe still washes the pack materials at "
                    + RenderSettings.reflectionIntensity.ToString("0.###", CultureInfo.InvariantCulture)
                    + " strength";
            }

            if (RenderSettings.fog)
            {
                return "fog stands between the camera and everything it photographs";
            }

            if (RenderSettings.ambientMode != AmbientMode.Trilight)
            {
                return "ambient runs on " + RenderSettings.ambientMode
                    + " rather than the three-band room the rig sets, so unlit faces read flat";
            }

            return BandApart("sky", RenderSettings.ambientSkyColor, Backdrop.AmbientSky)
                ?? BandApart("equator", RenderSettings.ambientEquatorColor, Backdrop.AmbientEquator)
                ?? BandApart("ground", RenderSettings.ambientGroundColor, Backdrop.AmbientGround);
        }

        static string BandApart(string band, Color live, Tint wanted)
        {
            var lit = Lit(live);
            var apart = Mathf.Max(
                Mathf.Abs(lit.Red - wanted.Red),
                Mathf.Max(Mathf.Abs(lit.Green - wanted.Green), Mathf.Abs(lit.Blue - wanted.Blue)));

            if (apart <= BandTolerance)
            {
                return null;
            }

            return "the " + band + " ambient lights the frame at " + lit + " rather than the "
                + wanted + " the build ships";
        }

        static Tint Lit(Color colour)
        {
            return new Tint(colour.r, colour.g, colour.b);
        }

        public static Light Sunlight()
        {
            var risen = TheSunAlreadyUp();
            if (risen != null)
            {
                return risen;
            }

            var light = new GameObject(SunName).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = GameBoot.SunAngle;
            light.intensity = GameBoot.SunStrength;

            return light;
        }

        public static Light TheSunAlreadyUp()
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    return light;
                }
            }

            return null;
        }

        public static int SunsUp()
        {
            var risen = 0;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    risen++;
                }
            }

            return risen;
        }

        static void Render(Camera camera, RenderTexture target)
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                camera.SubmitRenderRequest(request);
                return;
            }

            camera.Render();
        }
    }
}
