using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class PickupProp : MonoBehaviour
    {
        Transform badge;

        Renderer skin;

        Tint gem;

        WorldPoint ground;

        bool begun;

        public int NodeId { get; private set; }

        public Take Reel { get; private set; }

        public bool IsSpent
        {
            get { return Reel.IsSpent; }
        }

        internal void Begin(WorldPart part, int nodeId, Transform hangingBadge)
        {
            skin = GetComponentInChildren<Renderer>();
            badge = hangingBadge;
            NodeId = nodeId;

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

            transform.localScale = new Vector3(edge, height, edge);
            transform.localPosition = new Vector3(ground.X, ground.Y + height * 0.5f, ground.Z);

            if (!reel.IsSpent)
            {
                return;
            }

            Tints.Wash(skin, reel.Wash(gem));

            if (reel.IsSettled && badge != null)
            {
                badge.gameObject.SetActive(false);
            }
        }
    }
}
