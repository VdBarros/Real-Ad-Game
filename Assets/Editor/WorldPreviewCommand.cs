using System;
using System.Collections.Generic;
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

        const string BadgeCapturePath = "dev/scratch/t-09-badge-preview.png";

        const float BadgeCameraDistance = 20f;

        const float BadgeOrthographicSize = 4.2f;

        static WorldBuilder previewBuilder;

        [MenuItem("Tools/Real Ad Game/Build Preview Level")]
        public static void BuildPreview()
        {
            Clear();

            previewBuilder = new WorldBuilder();
            previewBuilder.Build(
                LevelGenerator.Generate(PreviewSeed, MazePreset.Ship).Graph, PowerTuning.Ship.StartingPower);
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
                if (material.name.StartsWith(WorldMaterials.NamePrefix, System.StringComparison.Ordinal)
                    || material.name.StartsWith(BadgeAssets.NamePrefix, System.StringComparison.Ordinal))
                {
                    WorldObjects.Destroy(material);
                }
            }

            foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite.name.StartsWith(BadgeAssets.NamePrefix, System.StringComparison.Ordinal))
                {
                    WorldObjects.Destroy(sprite);
                }
            }

            foreach (var texture in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                if (texture.name.StartsWith(BadgeAssets.NamePrefix, System.StringComparison.Ordinal))
                {
                    WorldObjects.Destroy(texture);
                }
            }
        }

        public static void Capture()
        {
            Shoot(CapturePath, CameraDistance, IsoProjection.OrthographicSize, false);
        }

        public static void CaptureBadges()
        {
            Shoot(BadgeCapturePath, BadgeCameraDistance, BadgeOrthographicSize, true);
        }

        public static void CheckBadgeAssets()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var builder = new WorldBuilder();
            var first = builder.Build(LevelGenerator.Generate(PreviewSeed, MazePreset.Ship).Graph, PowerTuning.Ship.StartingPower);
            var firstAssets = Assets(first);

            WorldObjects.Destroy(first);
            var second = builder.Build(LevelGenerator.Generate(PreviewSeed + 1, MazePreset.Ship).Graph, PowerTuning.Ship.StartingPower);
            var secondAssets = Assets(second);

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "badge assets: {0} distinct sprites and {1} distinct materials over {2} badges; "
                + "the second level reuses the first's: sprites {3}, materials {4}",
                Distinct(firstAssets.Item1),
                Distinct(firstAssets.Item2),
                firstAssets.Item1.Count,
                Distinct(Union(firstAssets.Item1, secondAssets.Item1)),
                Distinct(Union(firstAssets.Item2, secondAssets.Item2))));

            var sprite = secondAssets.Item1[0];
            var material = secondAssets.Item2[0];

            WorldObjects.Destroy(second);
            Debug.Log("after the level is destroyed the sprite is " + (sprite == null ? "gone" : "still alive")
                + " and the material is " + (material == null ? "gone" : "still alive"));

            builder.Dispose();
            Debug.Log("after the builder is disposed the sprite is " + (sprite == null ? "gone" : "still alive")
                + " and the material is " + (material == null ? "gone" : "still alive"));
        }

        static ValueTuple<List<Sprite>, List<Material>> Assets(GameObject root)
        {
            var sprites = new List<Sprite>();
            var materials = new List<Material>();

            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprites.Add(renderer.sprite);
                materials.Add(renderer.sharedMaterial);
            }

            return new ValueTuple<List<Sprite>, List<Material>>(sprites, materials);
        }

        static List<T> Union<T>(List<T> first, List<T> second)
        {
            var all = new List<T>(first);
            all.AddRange(second);
            return all;
        }

        static int Distinct<T>(List<T> items) where T : UnityEngine.Object
        {
            var seen = new List<T>();
            foreach (var item in items)
            {
                if (!seen.Contains(item))
                {
                    seen.Add(item);
                }
            }

            return seen.Count;
        }

        static void Shoot(string path, float distance, float orthographicSize, bool onTheStart)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = LevelGenerator.Generate(PreviewSeed, MazePreset.Ship);
            var blueprint = LevelBlueprintBuilder.Build(level.Graph);

            var builder = new WorldBuilder();
            var root = builder.Build(level.Graph, PowerTuning.Ship.StartingPower);
            var camera = Rig(onTheStart ? Start(level.Graph) : Centre(blueprint), distance, orthographicSize);
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

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, frame.EncodeToPNG());

            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(frame);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);

            Report(blueprint, BadgeBlueprintBuilder.Build(level.Graph, PowerTuning.Ship.StartingPower), root, path);

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

        static Camera Rig(Vector3 centre, float distance, float orthographicSize)
        {
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);
            camera.transform.position = centre - camera.transform.forward * distance;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.aspect = (float)CaptureWidth / CaptureHeight;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = distance * 3f;
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

        static Vector3 Start(LevelGraph graph)
        {
            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type != NodeType.Start)
                {
                    continue;
                }

                var point = IsoProjection.Of(node.Position);
                return new Vector3(point.X, point.Y, point.Z);
            }

            throw new InvalidOperationException("A level always has one start to look at.");
        }

        static void Report(LevelBlueprint blueprint, BadgeBlueprint badges, GameObject root, string path)
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
                "preview: {0} floors, {1} floor quads, {2} walls, {3} ramps, {4} props, {5} badges at "
                + "{6} glyph cells ({7:0.###} x {8:0.###} units, font {9:0.###}, ceiling {10}), "
                + "{11} transforms under {12}, written to {13}",
                blueprint.Floors.Count,
                quads,
                walls,
                ramps,
                props,
                badges.Badges.Count,
                badges.Plan.Capacity,
                badges.Plan.PlayerWidth,
                badges.Plan.Height,
                badges.Plan.FontSize,
                badges.Plan.PowerCeiling,
                root.GetComponentsInChildren<Transform>(true).Length,
                PartNames.Root,
                path));
        }
    }
}
