using System;
using System.Diagnostics;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class WorldBuilder : IDisposable
    {
        const float CameraDrift = 0.01f;

        readonly WorldMaterials materials = new WorldMaterials();

        readonly BadgeAssets badgeAssets = new BadgeAssets();

        public PowerBadge PlayerBadge { get; private set; }

        public GameObject Build(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var startingPower = PowerTuning.For(MazePreset.Named(graph.Preset)).StartingPower;
            var blueprint = LevelBlueprintBuilder.Build(graph);
            var badges = BadgeBlueprintBuilder.Build(graph, startingPower);
            var root = new GameObject(blueprint.RootName);

            PlayerBadge = null;
            WarnIfTheCameraHasTurned();

            foreach (var floor in blueprint.Floors)
            {
                var floorRoot = Group(root.transform, floor.Name);
                var tiles = Group(floorRoot, PartNames.TilesGroup);
                var nodes = Group(floorRoot, PartNames.NodesGroup);

                foreach (var part in floor.Tiles)
                {
                    Raise(part, tiles);
                }

                foreach (var part in floor.Nodes)
                {
                    Raise(part, nodes);
                }

                var group = Group(floorRoot, PartNames.BadgesGroup);

                foreach (var part in badges.Badges)
                {
                    if (part.Floor != floor.Floor)
                    {
                        continue;
                    }

                    Hang(part, badges.Plan, group, startingPower);
                }
            }

            return root;
        }

        void Hang(BadgePart part, BadgePlan plan, Transform parent, int startingPower)
        {
            var badge = BadgeFactory.Raise(part, plan, badgeAssets, parent);
            if (part.Style != BadgeStyle.Player)
            {
                return;
            }

            PlayerBadge = badge.gameObject.AddComponent<PowerBadge>();
            PlayerBadge.Begin(badge, startingPower);
        }

        [Conditional("UNITY_EDITOR")]
        static void WarnIfTheCameraHasTurned()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var expected = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);

            if (Quaternion.Angle(camera.transform.rotation, expected) <= CameraDrift)
            {
                return;
            }

            UnityEngine.Debug.LogWarning(
                "The main camera sits at " + camera.transform.rotation.eulerAngles
                + " rather than the constant framing every badge copies its rotation from at construction. "
                + "Badges do not billboard, so they will face the wrong way until the rig stops rotating.");
        }

        void Raise(WorldPart part, Transform parent)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveOf(part.Shape));
            instance.name = part.Name;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.localPosition = Vector(part.Position);
            instance.transform.localEulerAngles = Vector(part.Rotation);
            instance.transform.localScale = Vector(part.Scale);
            instance.GetComponent<Renderer>().sharedMaterial = materials.Of(part.Style);

            if (part.Style != PartStyle.Floor)
            {
                WorldObjects.Destroy(instance.GetComponent<Collider>());
            }
        }

        static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, worldPositionStays: false);
            return group.transform;
        }

        static PrimitiveType PrimitiveOf(PartShape shape)
        {
            switch (shape)
            {
                case PartShape.Quad:
                    return PrimitiveType.Quad;
                case PartShape.Cube:
                    return PrimitiveType.Cube;
                case PartShape.Capsule:
                    return PrimitiveType.Capsule;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, "No primitive for that shape.");
            }
        }

        static Vector3 Vector(WorldPoint point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }

        public void Dispose()
        {
            materials.Dispose();
            badgeAssets.Dispose();
        }
    }
}
