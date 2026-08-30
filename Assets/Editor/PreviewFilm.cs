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

        const float PlaneTolerance = 0.0001f;

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
            if (distance <= IsoProjection.NearPlane || distance >= Backdrop.Reach)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(distance),
                    distance,
                    "A preview rig stands between the build's near plane and the backdrop it hangs, or it"
                    + " photographs its subject through a clip plane the build never puts there.");
            }

            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, yaw, IsoProjection.CameraRoll);
            camera.transform.position = centre - camera.transform.forward * distance;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.nearClipPlane = IsoProjection.NearPlane;
            camera.farClipPlane = IsoProjection.FarPlane;
            WorldBackdrop.Hang(camera);

            return camera;
        }

        public static string LensAsPhotographed(Camera lens)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} lens clipping from {1:0.###} to {2:0.###}, clearing to {3} with {4}, {5}",
                lens.orthographic ? "an orthographic" : "a perspective",
                lens.nearClipPlane,
                lens.farClipPlane,
                Lit(lens.backgroundColor),
                lens.clearFlags,
                lens.GetComponentInChildren<WorldBackdrop>(true) == null
                    ? "with nothing hung behind its subject"
                    : "with the graded sheet hung behind its subject");
        }

        public static string LensApartFromTheBuild(Camera lens)
        {
            if (!lens.orthographic)
            {
                return "the lens projects in perspective where the build's is orthographic, so a subject"
                    + " is photographed at a size the game never draws it";
            }

            if (Mathf.Abs(lens.nearClipPlane - IsoProjection.NearPlane) > PlaneTolerance)
            {
                return "the lens opens at " + Plane(lens.nearClipPlane) + " where the build clips near at "
                    + Plane(IsoProjection.NearPlane);
            }

            if (Mathf.Abs(lens.farClipPlane - IsoProjection.FarPlane) > PlaneTolerance)
            {
                return "the lens sees out to " + Plane(lens.farClipPlane) + " where the build clips far at "
                    + Plane(IsoProjection.FarPlane);
            }

            if (lens.clearFlags != CameraClearFlags.SolidColor)
            {
                return "the lens clears on " + lens.clearFlags
                    + " rather than the solid colour the build clears to";
            }

            var clear = Lit(lens.backgroundColor);
            if (Apart(clear, Backdrop.Clear) > BandTolerance)
            {
                return "the lens clears to " + clear + " rather than the " + Backdrop.Clear
                    + " the build clears to";
            }

            if (lens.GetComponentInChildren<WorldBackdrop>(true) == null)
            {
                return "no graded sheet hangs on the lens, so a subject overhanging the floor is measured"
                    + " against empty space rather than the slate the build stands behind it";
            }

            return null;
        }

        static string Plane(float depth)
        {
            return depth.ToString("0.###", CultureInfo.InvariantCulture);
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

            if (Apart(lit, wanted) <= BandTolerance)
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

        static float Apart(Tint lit, Tint wanted)
        {
            return Mathf.Max(
                Mathf.Abs(lit.Red - wanted.Red),
                Mathf.Max(Mathf.Abs(lit.Green - wanted.Green), Mathf.Abs(lit.Blue - wanted.Blue)));
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
