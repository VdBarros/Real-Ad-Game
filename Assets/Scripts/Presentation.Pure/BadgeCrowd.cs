using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class BadgeCrowd
    {
        public const float SideGap = 0.06f;

        public const float StackGap = 0.06f;

        public const float Band = 0.2f;

        public const float LiftCeiling = 1f;

        public const float FaintestOpacity = 0.5f;

        public const float FadeSpan = BadgeMetrics.Height;

        public static BadgeStack Resolve(IReadOnlyList<BadgeSpot> spots)
        {
            if (spots == null)
            {
                throw new ArgumentNullException(nameof(spots));
            }

            if (spots.Count == 0)
            {
                return BadgeStack.Empty;
            }

            var queue = Queued(spots);
            var lifts = new float[spots.Count];
            var seats = new BadgeSeat[spots.Count];

            for (var rank = 0; rank < queue.Length; rank++)
            {
                var mine = queue[rank];
                var me = spots[mine];
                var wanted = 0f;

                for (var ahead = 0; ahead < rank; ahead++)
                {
                    var theirs = queue[ahead];
                    var them = spots[theirs];
                    var clearance = ClearanceBetween(me, them);

                    if (clearance <= 0f)
                    {
                        continue;
                    }

                    var floor = them.Up + lifts[theirs] + clearance - me.Up;
                    wanted = floor > wanted ? floor : wanted;
                }

                var lift = wanted > LiftCeiling ? LiftCeiling : wanted;
                lifts[mine] = lift;
                seats[mine] = new BadgeSeat(
                    me.NodeId, lift, OpacityFor(wanted - lift), queue.Length - 1 - rank);
            }

            return new BadgeStack(seats);
        }

        public static float ClearanceBetween(BadgeSpot one, BadgeSpot other)
        {
            var apart = Math.Abs(one.Across - other.Across);
            var touching = (one.Width + other.Width) * 0.5f + SideGap;

            if (apart >= touching + Band)
            {
                return 0f;
            }

            var share = apart <= touching ? 1f : (touching + Band - apart) / Band;

            return ((one.Height + other.Height) * 0.5f + StackGap) * share;
        }

        public static float OpacityFor(float unresolved)
        {
            if (unresolved <= 0f)
            {
                return 1f;
            }

            var share = unresolved >= FadeSpan ? 1f : unresolved / FadeSpan;

            return 1f - (1f - FaintestOpacity) * share;
        }

        public static IReadOnlyList<BadgeSpot> Seated(IReadOnlyList<BadgeSpot> spots, BadgeStack stack)
        {
            if (spots == null)
            {
                throw new ArgumentNullException(nameof(spots));
            }

            if (stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            var seated = new BadgeSpot[spots.Count];

            for (var slot = 0; slot < spots.Count; slot++)
            {
                seated[slot] = spots[slot].Lifted(stack.Of(spots[slot].NodeId).Lift);
            }

            return seated;
        }

        static int[] Queued(IReadOnlyList<BadgeSpot> spots)
        {
            var queue = new int[spots.Count];
            var depths = new float[spots.Count];

            for (var slot = 0; slot < spots.Count; slot++)
            {
                queue[slot] = slot;
                depths[slot] = spots[slot].Depth;
            }

            Array.Sort(queue, (left, right) => Compare(spots, depths, left, right));

            return queue;
        }

        static int Compare(IReadOnlyList<BadgeSpot> spots, float[] depths, int left, int right)
        {
            var nearest = depths[left].CompareTo(depths[right]);
            if (nearest != 0)
            {
                return nearest;
            }

            var highest = spots[right].Elevation.CompareTo(spots[left].Elevation);
            if (highest != 0)
            {
                return highest;
            }

            return spots[left].NodeId.CompareTo(spots[right].NodeId);
        }
    }
}
