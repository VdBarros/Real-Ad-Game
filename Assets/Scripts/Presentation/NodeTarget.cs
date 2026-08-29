using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class NodeTarget : MonoBehaviour
    {
        public const int UnknownPower = 0;

        NumberBadge badge;

        GateProp gate;

        Tint plain;

        BadgeStyle style;

        int value;

        int cells;

        bool borrowed;

        public int NodeId { get; private set; }

        public TargetMark Mark { get; private set; }

        public float Opacity { get; private set; }

        public bool Draws
        {
            get { return Opacity > 0f; }
        }

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

            plain = BadgeTints.Of(part.Style);
            style = part.Style;
            NodeId = part.NodeId;
            value = part.Value;
            cells = part.Cells;
            Mark = TargetMark.Idle;
            Opacity = OpacityOf(TargetMark.Idle, UnknownPower);
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
            Opacity = OpacityOf(TargetMark.Idle, UnknownPower);
            Dress(TargetMark.Idle, value);
        }

        public void Wear(TargetMark mark, int power)
        {
            Wear(mark, power, UnknownPower);
        }

        public void Wear(TargetMark mark, int power, int held)
        {
            if (badge == null && gate == null)
            {
                throw new InvalidOperationException(
                    "A target wears its mark on a subject it has not been given. Call Begin.");
            }

            var aimed = TargetMarks.IsAimed(mark);
            var opacity = OpacityOf(mark, held);

            if (mark == Mark
                && opacity == Opacity
                && (!aimed || badge == null || badge.Value == power))
            {
                return;
            }

            Mark = mark;
            Opacity = opacity;
            Dress(mark, aimed ? power : value);
            borrowed = aimed;
        }

        float OpacityOf(TargetMark mark, int held)
        {
            return badge == null
                ? TargetMarks.Look(mark).Opacity
                : TargetMarks.Opacity(mark, style, value, held);
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

            badge.Wash(Tints.Of(BadgeTints.Washed(plain, look)), Opacity);

            if (!TargetMarks.IsAimed(mark) && !borrowed)
            {
                return;
            }

            badge.Fit(TargetMarks.IsAimed(mark) ? BadgeText.Cells(badge.Style, shown) : cells);
            badge.Show(shown);
        }
    }
}
