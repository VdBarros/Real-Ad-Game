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

        int value;

        int cells;

        bool borrowed;

        public int NodeId { get; private set; }

        public TargetMark Mark { get; private set; }

        internal void Begin(BadgePart part)
        {
            badge = GetComponent<NumberBadge>();
            plain = BadgePalette.Of(part.Style);
            NodeId = part.NodeId;
            value = part.Value;
            cells = part.Cells;
            Mark = TargetMark.Idle;
            Dress(TargetMark.Idle, value);
        }

        public void Wear(TargetMark mark, int power)
        {
            if (badge == null)
            {
                throw new InvalidOperationException(
                    "A target wears its mark on a badge it has not been given. Call Begin.");
            }

            var aimed = TargetMarks.IsAimed(mark);
            if (mark == Mark && (!aimed || badge.Value == power))
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

            badge.Wash(Color.Lerp(plain, Tints.Of(look.Tint), look.Weight));
            transform.localScale = new Vector3(look.Scale, look.Scale, look.Scale);

            if (!TargetMarks.IsAimed(mark) && !borrowed)
            {
                return;
            }

            badge.Fit(TargetMarks.IsAimed(mark) ? BadgeText.Cells(badge.Style, shown) : cells);
            badge.Show(shown);
        }
    }
}
