using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class WorldBuilder : IDisposable
    {
        const float CameraDrift = 0.01f;

        readonly WorldModels models = new WorldModels();

        readonly WorldMaterials materials;

        readonly BadgeAssets badgeAssets = new BadgeAssets();

        public WorldBuilder()
        {
            materials = new WorldMaterials(models);
        }

        public PowerBadge PlayerBadge { get; private set; }

        public PlayerFigure Player { get; private set; }

        public FloorState Floor { get; private set; }

        public TargetBoard Targets { get; private set; }

        public TrailBoard Trail { get; private set; }

        public FightBoard Fights { get; private set; }

        public PickupBoard Pickups { get; private set; }

        public GameObject Build(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            return Build(graph, LevelPlan.For(MazePreset.Named(graph.Preset), 1).StartingPower);
        }

        public GameObject Build(LevelGraph graph, int startingPower)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var blueprint = LevelBlueprintBuilder.Build(graph);
            var badges = BadgeBlueprintBuilder.Build(graph, startingPower);
            var root = new GameObject(blueprint.RootName);

            PlayerBadge = null;
            Player = null;
            Floor = root.AddComponent<FloorState>();
            Floor.Dress(materials.Of(PartStyle.Floor), materials.Of(PartStyle.Cleared));
            Targets = root.AddComponent<TargetBoard>();
            Targets.Begin(graph.Decisions.Nodes.Count);
            Fights = root.AddComponent<FightBoard>();
            Fights.Dress(materials.Of(PartStyle.Spark));
            Pickups = root.AddComponent<PickupBoard>();
            Trail = Group(root.transform, PartNames.TrailGroup).gameObject.AddComponent<TrailBoard>();
            Trail.Dress(materials.Of(PartStyle.Trail));
            NumberBadge playerNumber = null;
            PlayerFigure playerFigure = null;
            var enemies = new List<EnemyFigure>();
            var pickups = new List<PickupProp>();
            var groundByName = new Dictionary<string, TilePosition>(graph.Tiles.Tiles.Count);
            WarnIfTheCameraHasTurned();

            foreach (var tile in graph.Tiles.Tiles)
            {
                groundByName.Add(PartNames.Tile(tile.Position), tile.Position);
            }

            foreach (var terrace in blueprint.Terraces)
            {
                var terraceRoot = Group(root.transform, terrace.Name);
                var tiles = Group(terraceRoot, PartNames.TilesGroup);
                var nodes = Group(terraceRoot, PartNames.NodesGroup);

                foreach (var part in terrace.Tiles)
                {
                    var instance = Raise(part, tiles);

                    TilePosition position;
                    if (groundByName.TryGetValue(part.Name, out position))
                    {
                        Floor.Adopt(position, instance.GetComponentInChildren<Renderer>());
                    }
                }

                foreach (var part in terrace.Nodes)
                {
                    Raise(part, nodes);
                }

                var group = Group(terraceRoot, PartNames.BadgesGroup);

                foreach (var part in badges.Badges)
                {
                    if (part.Elevation != terrace.Elevation)
                    {
                        continue;
                    }

                    var badge = BadgeFactory.Raise(part, badges.Plan, badgeAssets, group);
                    var prop = nodes.Find(PartNames.Node(part.NodeId));
                    var target = badge.gameObject.AddComponent<NodeTarget>();
                    target.Begin(part);
                    Targets.Adopt(target);

                    if (part.Style == BadgeStyle.Player)
                    {
                        playerNumber = badge;
                        playerFigure = prop.gameObject.AddComponent<PlayerFigure>();
                        playerFigure.Stand(badge.transform);
                    }
                    else if (part.Style == BadgeStyle.Enemy || part.Style == BadgeStyle.Boss)
                    {
                        var enemy = prop.gameObject.AddComponent<EnemyFigure>();
                        enemy.Begin(badge.transform, part.NodeId, part.Value);
                        enemies.Add(enemy);
                    }
                    else if (part.Style == BadgeStyle.Additive || part.Style == BadgeStyle.Multiplier)
                    {
                        WorldPart gem;
                        if (LevelBlueprintBuilder.TryProp(graph.Decisions.Node(part.NodeId), out gem))
                        {
                            var pickup = prop.gameObject.AddComponent<PickupProp>();
                            pickup.Begin(gem, part.NodeId, badge.transform);
                            pickups.Add(pickup);
                        }
                    }
                }
            }

            var opening = RunState.Begin(graph, startingPower);
            Fights.Begin(graph.Decisions.Nodes.Count, playerFigure, enemies);
            Pickups.Begin(graph.Decisions.Nodes.Count, pickups, opening);
            Floor.Begin(opening);
            Targets.Show(opening, TargetPreview.None);

            if (playerNumber != null)
            {
                Player = playerFigure;
                PlayerBadge = playerNumber.gameObject.AddComponent<PowerBadge>();
                PlayerBadge.Begin(playerNumber, playerFigure, startingPower);

                foreach (var enemy in enemies)
                {
                    enemy.Follow(PlayerBadge);
                }
            }

            return root;
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

        GameObject Raise(WorldPart part, Transform parent)
        {
            var model = models.Of(part.Model);
            var raised = model != null;
            var instance = raised
                ? UnityEngine.Object.Instantiate(model)
                : GameObject.CreatePrimitive(PrimitiveOf(part.Shape));

            instance.name = part.Name;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.localPosition = Vector(part.Position);
            instance.transform.localEulerAngles = Vector(raised ? ModelPose.RotationOf(part) : part.Rotation);
            instance.transform.localScale = Vector(raised ? ModelPose.ScaleOf(part) : part.Scale);
            instance.GetComponentInChildren<Renderer>().sharedMaterial = materials.Of(part.Style);

            if (part.Style != PartStyle.Floor)
            {
                WorldObjects.Destroy(instance.GetComponent<Collider>());
            }
            else if (raised)
            {
                Enclose(instance);
            }

            return instance;
        }

        static void Enclose(GameObject instance)
        {
            var filter = instance.GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            var bounds = filter.sharedMesh.bounds;
            var box = filter.gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
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
            models.Dispose();
            badgeAssets.Dispose();
        }
    }
}
