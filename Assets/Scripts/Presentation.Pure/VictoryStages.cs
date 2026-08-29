using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class VictoryStages
    {
        public const float ClashSeconds = 0.90f;

        public const float DissolveSeconds = 0.30f;

        static readonly VictoryStage[] order = { VictoryStage.Clash, VictoryStage.Dissolve };

        static readonly float[] seconds = { 0f, ClashSeconds, DissolveSeconds, 0f };

        static readonly bool[] blocks = { false, true, true, false };

        static readonly float[] opensAt = Cumulative();

        static readonly float blockingSeconds = LastBlockingCloses();

        public static IReadOnlyList<VictoryStage> Order
        {
            get { return order; }
        }

        public static VictoryStage First
        {
            get { return order[0]; }
        }

        public static float Seconds
        {
            get { return opensAt[(int)VictoryStage.Done]; }
        }

        public static float BlockingSeconds
        {
            get { return blockingSeconds; }
        }

        public static float SecondsOf(VictoryStage stage)
        {
            return seconds[Slot(stage)];
        }

        public static bool BlocksInput(VictoryStage stage)
        {
            return blocks[Slot(stage)];
        }

        public static float OpensAt(VictoryStage stage)
        {
            return opensAt[Slot(stage)];
        }

        public static float ClosesAt(VictoryStage stage)
        {
            return opensAt[Slot(stage)] + seconds[Slot(stage)];
        }

        public static VictoryStage After(VictoryStage stage)
        {
            Slot(stage);

            for (var slot = 0; slot < order.Length; slot++)
            {
                if (order[slot] != stage)
                {
                    continue;
                }

                return slot + 1 < order.Length ? order[slot + 1] : VictoryStage.Done;
            }

            return stage;
        }

        static float[] Cumulative()
        {
            var opens = new float[seconds.Length];
            var running = 0f;

            foreach (var stage in order)
            {
                opens[(int)stage] = running;
                running += seconds[(int)stage];
            }

            opens[(int)VictoryStage.Done] = running;

            return opens;
        }

        static float LastBlockingCloses()
        {
            var closes = 0f;

            foreach (var stage in order)
            {
                if (!blocks[(int)stage])
                {
                    continue;
                }

                var ends = opensAt[(int)stage] + seconds[(int)stage];
                if (ends > closes)
                {
                    closes = ends;
                }
            }

            return closes;
        }

        static int Slot(VictoryStage stage)
        {
            var slot = (int)stage;
            if (slot < 0 || slot >= seconds.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stage), stage, "The victory choreography has no such stage.");
            }

            return slot;
        }
    }
}
