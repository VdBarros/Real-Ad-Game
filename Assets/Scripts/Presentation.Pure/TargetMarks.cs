using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class TargetMarks
    {
        public const float Solid = 1f;

        public const float AsideOpacity = 0.7f;

        public const float UnreachableOpacity = 0.4f;

        static readonly Tint Unpainted = new Tint(1f, 1f, 1f);

        static readonly MarkLook[] looks =
        {
            new MarkLook(Unpainted, 0f, 1f, Solid),
            new MarkLook(Unpainted, 0f, 1f, AsideOpacity),
            new MarkLook(Unpainted, 0f, 0.86f, UnreachableOpacity),
            new MarkLook(new Tint(0.92f, 0.96f, 1f), 0.55f, 1.18f, Solid),
            new MarkLook(new Tint(0.25f, 0.80f, 0.35f), 1f, 1.18f, Solid),
            new MarkLook(new Tint(0.98f, 0.76f, 0.20f), 1f, 1.18f, Solid),
            new MarkLook(new Tint(0.92f, 0.16f, 0.16f), 1f, 0.88f, Solid)
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
