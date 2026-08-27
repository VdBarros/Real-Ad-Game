using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class PickupProp : MonoBehaviour
    {
        Transform badge;

        Renderer[] skins;

        Tint gem;

        WorldPart prop;

        WorldPoint ground;

        bool dressed;

        bool begun;

        public int NodeId { get; private set; }

        public Take Reel { get; private set; }

        public bool IsSpent
        {
            get { return Reel.IsSpent; }
        }

        internal void Begin(WorldPart part, int nodeId, Transform hangingBadge, bool wearsAMesh)
        {
            skins = GetComponentsInChildren<Renderer>(true);
            badge = hangingBadge;
            NodeId = nodeId;
            prop = part;
            dressed = wearsAMesh;

            var colour = WorldPalette.Of(part.Style);
            gem = new Tint(colour.r, colour.g, colour.b);
            ground = new WorldPoint(
                part.Position.X, part.Position.Y - part.Scale.Y * 0.5f, part.Position.Z);
            begun = true;

            Wear(Take.None);
        }

        public void Wear(Take reel)
        {
            if (!begun)
            {
                throw new InvalidOperationException(
                    "A pickup collapses onto a pedestal it has not been given. Call Begin.");
            }

            Reel = reel;

            var edge = reel.Edge;
            var height = reel.Height;
            var collapsing = new WorldPart(
                prop.Name,
                prop.Shape,
                prop.Model,
                prop.Style,
                new WorldPoint(ground.X, ground.Y + height * 0.5f, ground.Z),
                prop.Rotation,
                new WorldPoint(edge, height, edge));

            transform.localScale = Vector(dressed ? ModelPose.ScaleOf(collapsing) : collapsing.Scale);
            transform.localPosition = Vector(dressed ? ModelPose.PositionOf(collapsing) : collapsing.Position);

            if (!reel.IsSpent)
            {
                return;
            }

            var wash = reel.Wash(gem);
            foreach (var skin in skins)
            {
                Tints.Wash(skin, wash);
            }

            if (reel.IsSettled && badge != null)
            {
                badge.gameObject.SetActive(false);
            }
        }

        static Vector3 Vector(WorldPoint point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }
    }
}
