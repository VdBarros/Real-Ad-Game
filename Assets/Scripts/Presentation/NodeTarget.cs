using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [RequireComponent(typeof(NumberBadge))]
    public sealed class NodeTarget : MonoBehaviour
    {
        NumberBadge badge;

        Color plain;

        public int NodeId { get; private set; }

        public TargetMark Mark { get; private set; }

        public WorldPoint Anchor { get; private set; }

        internal void Begin(BadgePart part, WorldPoint anchor)
        {
            badge = GetComponent<NumberBadge>();
            plain = BadgePalette.Of(part.Style);
            NodeId = part.NodeId;
            Anchor = anchor;
            Mark = TargetMark.Idle;
            Wear(TargetMark.Idle, force: true);
        }

        public void Wear(TargetMark mark)
        {
            Wear(mark, force: false);
        }

        void Wear(TargetMark mark, bool force)
        {
            if (badge == null)
            {
                throw new InvalidOperationException(
                    "A target wears its mark on a badge it has not been given. Call Begin.");
            }

            if (!force && mark == Mark)
            {
                return;
            }

            Mark = mark;
            badge.Wash(Color.Lerp(plain, Tints.Of(TargetMarks.TintOf(mark)), TargetMarks.WeightOf(mark)));

            var scale = TargetMarks.ScaleOf(mark);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
