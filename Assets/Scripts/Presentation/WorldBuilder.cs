using System;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class WorldBuilder : IDisposable
    {
        readonly WorldMaterials materials = new WorldMaterials();

        public GameObject Build(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var blueprint = LevelBlueprintBuilder.Build(graph);
            var root = new GameObject(blueprint.RootName);

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
            }

            return root;
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
        }
    }
}
