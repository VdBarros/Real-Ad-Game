using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class TargetMarks
    {
        static readonly Tint[] tints =
        {
            new Tint(1f, 1f, 1f),
            new Tint(0.16f, 0.17f, 0.20f),
            new Tint(0.16f, 0.17f, 0.20f),
            new Tint(0.92f, 0.96f, 1f),
            new Tint(0.25f, 0.80f, 0.35f),
            new Tint(0.98f, 0.76f, 0.20f),
            new Tint(0.92f, 0.16f, 0.16f)
        };

        static readonly float[] weights = { 0f, 0.45f, 0.92f, 0.55f, 1f, 1f, 1f };

        static readonly float[] scales = { 1f, 1f, 0.86f, 1.18f, 1.18f, 1.18f, 0.88f };

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

        public static bool IsTappable(TargetMark mark)
        {
            return Slot(mark) != Slot(TargetMark.Unreachable);
        }

        public static Tint TintOf(TargetMark mark)
        {
            return tints[Slot(mark)];
        }

        public static float WeightOf(TargetMark mark)
        {
            return weights[Slot(mark)];
        }

        public static float ScaleOf(TargetMark mark)
        {
            return scales[Slot(mark)];
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

        static int Slot(TargetMark mark)
        {
            var slot = (int)mark;
            if (slot < 0 || slot >= tints.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(mark), mark, "No look for that mark.");
            }

            return slot;
        }
    }
}
