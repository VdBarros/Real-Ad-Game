using System;
using System.Collections.Generic;
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

        readonly List<int> badgeFloors = new List<int>();

        readonly List<Transform> badgeGroups = new List<Transform>();

        public PowerBadge PlayerBadge { get; private set; }

        public GameObject Build(LevelGraph graph, int startingPower)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var blueprint = LevelBlueprintBuilder.Build(graph);
            var badges = BadgeBlueprintBuilder.Build(graph, startingPower);
            var root = new GameObject(blueprint.RootName);

            badgeFloors.Clear();
            badgeGroups.Clear();
            PlayerBadge = null;

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

                badgeFloors.Add(floor.Floor);
                badgeGroups.Add(Group(floorRoot, PartNames.BadgesGroup));
            }

            WarnIfTheCameraHasTurned();

            foreach (var part in badges.Badges)
            {
                var badge = BadgeFactory.Raise(part, badges.Plan, badgeAssets, GroupFor(part.Floor));
                if (part.Style != BadgeStyle.Player)
                {
                    continue;
                }

                PlayerBadge = badge.gameObject.AddComponent<PowerBadge>();
                PlayerBadge.Begin(badge, startingPower);
            }

            return root;
        }

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

            Debug.LogWarning(
                "The main camera sits at " + camera.transform.rotation.eulerAngles
                + " rather than the constant framing every badge copies its rotation from at construction. "
                + "Badges do not billboard, so they will face the wrong way until the rig stops rotating.");
        }

        Transform GroupFor(int floor)
        {
            for (var slot = 0; slot < badgeFloors.Count; slot++)
            {
                if (badgeFloors[slot] == floor)
                {
                    return badgeGroups[slot];
                }
            }

            throw new InvalidOperationException("A badge stands on floor " + floor + ", which the level has not built.");
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
