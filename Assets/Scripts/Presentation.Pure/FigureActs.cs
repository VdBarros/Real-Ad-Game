using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class FigureActs
    {
        static readonly FigureAct[] all =
        {
            FigureAct.Idle,
            FigureAct.Walk,
            FigureAct.Retreat,
            FigureAct.Strike,
            FigureAct.Clash,
            FigureAct.Recoil,
            FigureAct.Take,
            FigureAct.Kick,
            FigureAct.Slice,
            FigureAct.Cleave,
            FigureAct.Loose,
            FigureAct.Cast,
            FigureAct.Fall
        };

        static readonly FigureAct[] looped =
        {
            FigureAct.Idle,
            FigureAct.Walk,
            FigureAct.Retreat
        };

        public static IReadOnlyList<FigureAct> All
        {
            get { return all; }
        }

        public static int Count
        {
            get { return all.Length; }
        }

        public static int SlotOf(FigureAct act)
        {
            for (var slot = 0; slot < all.Length; slot++)
            {
                if (all[slot] == act)
                {
                    return slot;
                }
            }

            return -1;
        }

        public static bool Loops(FigureAct act)
        {
            if (SlotOf(act) < 0)
            {
                throw Stranger(act);
            }

            for (var slot = 0; slot < looped.Length; slot++)
            {
                if (looped[slot] == act)
                {
                    return true;
                }
            }

            return false;
        }

        public static ArgumentOutOfRangeException Stranger(FigureAct act)
        {
            return new ArgumentOutOfRangeException(nameof(act), act, "No clip name for that act.");
        }
    }
}
