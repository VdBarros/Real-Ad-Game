using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class NodeTarget : MonoBehaviour
    {
        NumberBadge badge;

        GateProp gate;

        Color plain;

        int value;

        int cells;

        bool borrowed;

        public int NodeId { get; private set; }

        public TargetMark Mark { get; private set; }

        public NumberBadge Badge
        {
            get { return badge; }
        }

        public GateProp Gate
        {
            get { return gate; }
        }

        internal void Begin(BadgePart part)
        {
            badge = GetComponent<NumberBadge>();

            if (badge == null)
            {
                throw new InvalidOperationException(
                    "A badge target wears its mark on a badge that is not there.");
            }

            plain = BadgePalette.Of(part.Style);
            NodeId = part.NodeId;
            value = part.Value;
            cells = part.Cells;
            Mark = TargetMark.Idle;
            Dress(TargetMark.Idle, value);
        }

        internal void Begin(GateProp arch, int nodeId, int factor)
        {
            if (arch == null)
            {
                throw new ArgumentNullException(nameof(arch));
            }

            gate = arch;
            NodeId = nodeId;
            value = factor;
            cells = 0;
            Mark = TargetMark.Idle;
            Dress(TargetMark.Idle, value);
        }

        public void Wear(TargetMark mark, int power)
        {
            if (badge == null && gate == null)
            {
                throw new InvalidOperationException(
                    "A target wears its mark on a subject it has not been given. Call Begin.");
            }

            var aimed = TargetMarks.IsAimed(mark);
            if (mark == Mark && (!aimed || badge == null || badge.Value == power))
            {
                return;
            }

            Mark = mark;
            Dress(mark, aimed ? power : value);
            borrowed = aimed;
        }

        void Dress(TargetMark mark, int shown)
        {
            var look = TargetMarks.Look(mark);

            transform.localScale = new Vector3(look.Scale, look.Scale, look.Scale);

            if (gate != null)
            {
                gate.Wash(Tints.Of(GateLook.Washed(gate.Tint, look)));
                return;
            }

            badge.Wash(Color.Lerp(plain, Tints.Of(look.Tint), look.Weight));

            if (!TargetMarks.IsAimed(mark) && !borrowed)
            {
                return;
            }

            badge.Fit(TargetMarks.IsAimed(mark) ? BadgeText.Cells(badge.Style, shown) : cells);
            badge.Show(shown);
        }
    }
}
