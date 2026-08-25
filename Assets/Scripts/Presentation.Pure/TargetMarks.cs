using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class TargetMarks
    {
        static readonly MarkLook[] looks =
        {
            new MarkLook(new Tint(1f, 1f, 1f), 0f, 1f),
            new MarkLook(new Tint(0.24f, 0.26f, 0.30f), 0.45f, 1f),
            new MarkLook(new Tint(0.10f, 0.10f, 0.12f), 0.92f, 0.86f),
            new MarkLook(new Tint(0.92f, 0.96f, 1f), 0.55f, 1.18f),
            new MarkLook(new Tint(0.25f, 0.80f, 0.35f), 1f, 1.18f),
            new MarkLook(new Tint(0.98f, 0.76f, 0.20f), 1f, 1.18f),
            new MarkLook(new Tint(0.92f, 0.16f, 0.16f), 1f, 0.88f)
        };

        public static TargetMark Of(RunState state, int nodeId, TargetPreview preview)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            if (nodeId == state.PositionNodeId)
            {
                return TargetMark.Idle;
            }

            if (preview.IsAimed && preview.NodeId == nodeId)
            {
                return OutcomeOf(preview.Outcome);
            }

            if (!state.IsReachable(nodeId))
            {
                return TargetMark.Unreachable;
            }

            return preview.IsAimed ? TargetMark.Aside : TargetMark.Idle;
        }

        public static bool IsAimed(TargetMark mark)
        {
            switch (mark)
            {
                case TargetMark.Walk:
                case TargetMark.Win:
                case TargetMark.Tie:
                case TargetMark.Loss:
                    return true;
                default:
                    Look(mark);
                    return false;
            }
        }

        public static MarkLook Look(TargetMark mark)
        {
            var slot = (int)mark;
            if (slot < 0 || slot >= looks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(mark), mark, "No look for that mark.");
            }

            return looks[slot];
        }

        static TargetMark OutcomeOf(ActionOutcome outcome)
        {
            switch (outcome)
            {
                case ActionOutcome.Walked:
                    return TargetMark.Walk;
                case ActionOutcome.Win:
                    return TargetMark.Win;
                case ActionOutcome.Tie:
                    return TargetMark.Tie;
                case ActionOutcome.Loss:
                    return TargetMark.Loss;
                default:
                    return TargetMark.Unreachable;
            }
        }
    }
}
