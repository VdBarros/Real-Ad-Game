using System.IO;
using Game.Presentation.Pure;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTooling
{
    public static class PreviewFilm
    {
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
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);
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
            var light = new GameObject("PreviewSun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, 200f, 0f);
            light.intensity = 1.6f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.34f);
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
