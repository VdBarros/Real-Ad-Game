using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class BadgeStack
    {
        static readonly BadgeStack nothing = new BadgeStack(new BadgeSeat[0]);

        readonly IReadOnlyList<BadgeSeat> seats;

        internal BadgeStack(IReadOnlyList<BadgeSeat> taken)
        {
            seats = taken;
        }

        public static BadgeStack Empty
        {
            get { return nothing; }
        }

        public IReadOnlyList<BadgeSeat> Seats
        {
            get { return seats; }
        }

        public int Stacked
        {
            get
            {
                var count = 0;
                for (var slot = 0; slot < seats.Count; slot++)
                {
                    if (seats[slot].IsStacked)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int Faded
        {
            get
            {
                var count = 0;
                for (var slot = 0; slot < seats.Count; slot++)
                {
                    if (seats[slot].IsFaded)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool TryOf(int nodeId, out BadgeSeat seat)
        {
            for (var slot = 0; slot < seats.Count; slot++)
            {
                if (seats[slot].NodeId == nodeId)
                {
                    seat = seats[slot];
                    return true;
                }
            }

            seat = default(BadgeSeat);
            return false;
        }

        public BadgeSeat Of(int nodeId)
        {
            BadgeSeat seat;
            if (TryOf(nodeId, out seat))
            {
                return seat;
            }

            throw new ArgumentOutOfRangeException(
                nameof(nodeId), nodeId, "No badge of that node was seated in this crowd.");
        }
    }
}
