using System.Globalization;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorTooling
{
    public static class WorldPreviewCommand
    {
        const long PreviewSeed = 20250824L;

        const float CameraDistance = 60f;

        const string CapturePath = "dev/scratch/t-08-world-preview.png";

        const string BadgeCapturePath = "dev/scratch/t-09-badge-preview.png";

        const string TierCapturePath = "dev/scratch/t-10-tier-preview.png";

        const int TopTierPower = 420;

        const float BadgeCameraDistance = 20f;

        const float BadgeOrthographicSize = 4.2f;

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
            Shoot(CapturePath, CameraDistance, IsoProjection.OrthographicSize, false, PowerTuning.Ship.StartingPower);
        }

        public static void CaptureBadges()
        {
            Shoot(BadgeCapturePath, BadgeCameraDistance, BadgeOrthographicSize, true, PowerTuning.Ship.StartingPower);
        }

        public static void CaptureTiers()
        {
            Shoot(TierCapturePath, BadgeCameraDistance, BadgeOrthographicSize, true, TopTierPower);
        }

        static void Shoot(string path, float distance, float orthographicSize, bool onTheStart, int power)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = LevelGenerator.Generate(PreviewSeed, MazePreset.Ship);
            var blueprint = LevelBlueprintBuilder.Build(level.Graph);

            var builder = new WorldBuilder();
            var root = builder.Build(level.Graph);
            PowerPump.Settle(builder.PlayerBadge, power);
            var camera = PreviewFilm.Rig(
                onTheStart ? Start(level.Graph) : Centre(blueprint), distance, orthographicSize);
            PreviewFilm.Sun();

            PreviewFilm.Warm(camera);
            PreviewFilm.Shoot(camera, path);

            Report(blueprint, BadgeBlueprintBuilder.Build(level.Graph, PowerTuning.Ship.StartingPower), root, path);

            builder.Dispose();
        }

        static Vector3 Centre(LevelBlueprint blueprint)
        {
            var minimum = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maximum = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var terrace in blueprint.Terraces)
            {
                foreach (var part in terrace.Tiles)
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

            throw new System.InvalidOperationException("A level always has one start to look at.");
        }

        static void Report(LevelBlueprint blueprint, BadgeBlueprint badges, GameObject root, string path)
        {
            var quads = 0;
            var walls = 0;
            var stairs = 0;
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
                    case PartStyle.Staircase:
                        stairs++;
                        break;
                    default:
                        props++;
                        break;
                }
            }

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "preview: {0} terraces, {1} floor quads, {2} walls, {13} staircases, {3} props, {4} badges at "
                + "{5} glyph cells ({6:0.###} x {7:0.###} units, font {8:0.###}, ceiling {9}), "
                + "{10} transforms under {11}, written to {12}",
                blueprint.Terraces.Count,
                quads,
                walls,
                props,
                badges.Badges.Count,
                badges.Plan.Capacity,
                badges.Plan.PlayerWidth,
                badges.Plan.Height,
                badges.Plan.FontSize,
                badges.Plan.PowerCeiling,
                root.GetComponentsInChildren<Transform>(true).Length,
                PartNames.Root,
                path,
                stairs));
        }
    }
}
