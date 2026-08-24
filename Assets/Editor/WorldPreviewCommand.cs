using System.Globalization;
using System.IO;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.EditorTooling
{
    public static class WorldPreviewCommand
    {
        const long PreviewSeed = 20250824L;

        const int CaptureWidth = 1080;

        const int CaptureHeight = 1920;

        const float CameraDistance = 60f;

        const string CapturePath = "dev/scratch/t-08-world-preview.png";

        static WorldBuilder previewBuilder;

        [MenuItem("Tools/Real Ad Game/Build Preview Level")]
        public static void BuildPreview()
        {
            Clear();

            previewBuilder = new WorldBuilder();
            previewBuilder.Build(LevelGenerator.Generate(PreviewSeed, MazePreset.Ship).Graph);
        }

        [MenuItem("Tools/Real Ad Game/Clear Preview Level")]
        public static void Clear()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == PartNames.Root)
                {
                    WorldObjects.Destroy(root);
                }
            }

            if (previewBuilder != null)
            {
                previewBuilder.Dispose();
                previewBuilder = null;
            }

            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (material.name.StartsWith(WorldMaterials.NamePrefix, System.StringComparison.Ordinal))
                {
                    WorldObjects.Destroy(material);
                }
            }
        }

        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = LevelGenerator.Generate(PreviewSeed, MazePreset.Ship);
            var blueprint = LevelBlueprintBuilder.Build(level.Graph);

            var builder = new WorldBuilder();
            var root = builder.Build(level.Graph);
            var camera = Rig(Centre(blueprint));
            Sun();

            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            Render(camera, target);

            var frame = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            frame.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            frame.Apply();
            RenderTexture.active = previous;

            Directory.CreateDirectory(Path.GetDirectoryName(CapturePath));
            File.WriteAllBytes(CapturePath, frame.EncodeToPNG());

            camera.targetTexture = null;
            Object.DestroyImmediate(frame);
            target.Release();
            Object.DestroyImmediate(target);

            Report(blueprint, root);

            builder.Dispose();
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

        static Camera Rig(Vector3 centre)
        {
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);
            camera.transform.position = centre - camera.transform.forward * CameraDistance;
            camera.orthographic = true;
            camera.orthographicSize = IsoProjection.OrthographicSize;
            camera.aspect = (float)CaptureWidth / CaptureHeight;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = CameraDistance * 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            return camera;
        }

        static void Sun()
        {
            var light = new GameObject("PreviewSun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, 200f, 0f);
            light.intensity = 1.6f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.34f);
        }

        static Vector3 Centre(LevelBlueprint blueprint)
        {
            var minimum = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maximum = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var floor in blueprint.Floors)
            {
                foreach (var part in floor.Tiles)
                {
                    if (part.Style != PartStyle.Floor)
                    {
                        continue;
                    }

                    var point = new Vector3(part.Position.X, part.Position.Y, part.Position.Z);
                    minimum = Vector3.Min(minimum, point);
                    maximum = Vector3.Max(maximum, point);
                }
            }

            return (minimum + maximum) * 0.5f;
        }

        static void Report(LevelBlueprint blueprint, GameObject root)
        {
            var quads = 0;
            var walls = 0;
            var ramps = 0;
            var props = 0;

            foreach (var part in blueprint.AllParts)
            {
                switch (part.Style)
                {
                    case PartStyle.Floor:
                        quads++;
                        break;
                    case PartStyle.Wall:
                        walls++;
                        break;
                    case PartStyle.Ramp:
                        ramps++;
                        break;
                    default:
                        props++;
                        break;
                }
            }

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "T-08 preview: {0} floors, {1} floor quads, {2} walls, {3} ramps, {4} props, {5} transforms under {6}, written to {7}",
                blueprint.Floors.Count,
                quads,
                walls,
                ramps,
                props,
                root.GetComponentsInChildren<Transform>(true).Length,
                PartNames.Root,
                CapturePath));
        }
    }
}
