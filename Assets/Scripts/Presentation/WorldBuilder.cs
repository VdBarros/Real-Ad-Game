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

        readonly Dictionary<string, PartModel> worn = new Dictionary<string, PartModel>();

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
            var playerWorn = PartModel.None;
            var enemies = new List<EnemyFigure>();
            var pickups = new List<PickupProp>();
            var groundByName = new Dictionary<string, TilePosition>(graph.Tiles.Tiles.Count);
            var gatesByName = new Dictionary<string, DecisionNode>();
            var landmarksByName = new Dictionary<string, LandmarkKind>();
            worn.Clear();
            WarnIfTheCameraHasTurned();

            foreach (var tile in graph.Tiles.Tiles)
            {
                groundByName.Add(
                    LevelBlueprintBuilder.WalkingSurfaceOf(graph.Tiles, tile.Position), tile.Position);
            }

            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Multiplier)
                {
                    gatesByName.Add(PartNames.Node(node.Id), node);
                }
            }

            foreach (var spot in Landmarks.Of(graph))
            {
                landmarksByName.Add(PartNames.Landmark(spot.Tile), spot.Kind);
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
                    var instance = Raise(part, nodes);

                    DecisionNode gate;
                    if (gatesByName.TryGetValue(part.Name, out gate))
                    {
                        pickups.Add(Arch(instance, gate));
                    }
                }

                var marks = Group(terraceRoot, PartNames.LandmarksGroup);

                foreach (var part in terrace.Landmarks)
                {
                    Stand(Raise(part, marks), landmarksByName[part.Name]);
                }

                var group = Group(terraceRoot, PartNames.BadgesGroup);

                foreach (var part in badges.Badges)
                {
                    if (part.Elevation != terrace.Elevation)
                    {
                        continue;
                    }

                    var badge = BadgeFactory.Raise(part, badgeAssets, group);
                    var prop = nodes.Find(PartNames.Node(part.NodeId));
                    var target = badge.gameObject.AddComponent<NodeTarget>();
                    target.Begin(part);
                    Targets.Adopt(target);

                    if (part.Style == BadgeStyle.Player)
                    {
                        var worn = Worn(part.NodeId);
                        playerWorn = worn;
                        playerNumber = badge;
                        playerFigure = prop.gameObject.AddComponent<PlayerFigure>();
                        playerFigure.Stand(badge.transform, worn);
                        FigureAnimator.Raise(playerFigure.gameObject, worn, models);
                    }
                    else if (part.Style == BadgeStyle.Enemy || part.Style == BadgeStyle.Boss)
                    {
                        var carried = Worn(part.NodeId);
                        var enemy = prop.gameObject.AddComponent<EnemyFigure>();
                        FigureAnimator.Raise(enemy.gameObject, carried, models);
                        enemy.Begin(badge.transform, carried, part.NodeId, part.Value);
                        enemies.Add(enemy);
                    }
                    else if (part.Style == BadgeStyle.Additive)
                    {
                        WorldPart gem;
                        if (LevelBlueprintBuilder.TryProp(graph.Decisions.Node(part.NodeId), out gem))
                        {
                            var pickup = prop.gameObject.AddComponent<PickupProp>();
                            pickup.Begin(gem, part.NodeId, badge.transform, models.Of(gem.Model) != null);
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
                PlayerBadge.Begin(playerNumber, playerFigure, playerWorn, startingPower);

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

        PartModel Worn(int nodeId)
        {
            PartModel mesh;

            return worn.TryGetValue(PartNames.Node(nodeId), out mesh) ? mesh : PartModel.None;
        }

        static LandmarkProp Stand(GameObject instance, LandmarkKind kind)
        {
            var pieces = LandmarkForm.Pieces(kind);

            foreach (var piece in pieces)
            {
                var block = GameObject.CreatePrimitive(PrimitiveOf(piece.Part.Shape));
                block.name = piece.Part.Name;
                block.transform.SetParent(instance.transform, worldPositionStays: false);
                block.transform.localPosition = Vector(piece.Part.Position);
                block.transform.localEulerAngles = Vector(piece.Part.Rotation);
                block.transform.localScale = Vector(piece.Part.Scale);
                WorldObjects.Destroy(block.GetComponent<Collider>());
            }

            var prop = instance.AddComponent<LandmarkProp>();
            prop.Begin(kind, pieces);

            return prop;
        }

        PickupProp Arch(GameObject instance, DecisionNode node)
        {
            var pieces = GateArch.Pieces(node.Value);

            foreach (var piece in pieces)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = piece.Name;
                block.transform.SetParent(instance.transform, worldPositionStays: false);
                block.transform.localPosition = Vector(piece.Position);
                block.transform.localEulerAngles = Vector(piece.Rotation);
                block.transform.localScale = Vector(piece.Scale);
                WorldObjects.Destroy(block.GetComponent<Collider>());
            }

            var glow = instance.AddComponent<GateProp>();
            glow.Begin(node.Value, pieces);

            var target = instance.AddComponent<NodeTarget>();
            target.Begin(glow, node.Id, node.Value);
            Targets.Adopt(target);

            WorldPart gate;
            LevelBlueprintBuilder.TryProp(node, out gate);
            var pickup = instance.AddComponent<PickupProp>();
            pickup.Begin(gate, node.Id, null, wearsAMesh: false);

            return pickup;
        }

        GameObject Raise(WorldPart part, Transform parent)
        {
            var model = models.Of(part.Model);
            var raised = model != null;

            if (CharacterCast.IsRole(part.Style))
            {
                worn[part.Name] = raised ? part.Model : PartModel.None;
            }

            var instance = raised
                ? UnityEngine.Object.Instantiate(model)
                : part.Shape == PartShape.Gate || part.Shape == PartShape.Landmark
                    ? new GameObject()
                    : GameObject.CreatePrimitive(PrimitiveOf(part.Shape));

            if (raised && ArtPacks.IsRigged(part.Model))
            {
                CharacterDress.Bare(instance);
            }

            instance.name = part.Name;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.localPosition = Vector(raised ? ModelPose.PositionOf(part) : part.Position);
            instance.transform.localEulerAngles = Vector(raised ? ModelPose.RotationOf(part) : part.Rotation);
            instance.transform.localScale = Vector(raised ? ModelPose.ScaleOf(part) : part.Scale);
            Dress(instance, materials.Of(part.Style));

            if (!LevelBlueprintBuilder.IsWalkingSurface(part.Style))
            {
                WorldObjects.Destroy(instance.GetComponent<Collider>());
            }
            else if (raised)
            {
                Enclose(instance);
            }

            return instance;
        }

        static void Dress(GameObject instance, Material material)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }

        static void Enclose(GameObject instance)
        {
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                {
                    continue;
                }

                var bounds = filter.sharedMesh.bounds;
                var box = filter.gameObject.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
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
                case PartShape.Sphere:
                    return PrimitiveType.Sphere;
                case PartShape.Cylinder:
                    return PrimitiveType.Cylinder;
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
