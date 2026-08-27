using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class AdventurerClips
    {
        public const string Idle = "Idle";

        public const string Walk = "Walking_A";

        public const string Retreat = "Walking_Backwards";

        public const string Strike = "1H_Melee_Attack_Chop";

        public const string Clash = "Block_Hit";

        public const string Recoil = "Hit_A";

        public const string Take = "PickUp";

        static readonly string[] names = { Idle, Walk, Retreat, Strike, Clash, Recoil, Take };

        static readonly bool[] loops = { true, true, true, false, false, false, false };

        public static IReadOnlyList<string> Names
        {
            get { return names; }
        }

        public static int Count
        {
            get { return names.Length; }
        }

        public static string NameOf(FigureAct act)
        {
            return names[Slot(act)];
        }

        public static bool Loops(FigureAct act)
        {
            return loops[Slot(act)];
        }

        public static bool Wants(string clip)
        {
            return Named(clip) >= 0;
        }

        public static bool LoopsOf(string clip)
        {
            var slot = Named(clip);

            return slot >= 0 && loops[slot];
        }

        public static bool Carries(PartModel model)
        {
            return AdventurerPack.Carries(model);
        }

        static int Named(string clip)
        {
            if (string.IsNullOrEmpty(clip))
            {
                return -1;
            }

            for (var slot = 0; slot < names.Length; slot++)
            {
                if (string.Equals(names[slot], clip, StringComparison.Ordinal))
                {
                    return slot;
                }
            }

            return -1;
        }

        static int Slot(FigureAct act)
        {
            var slot = (int)act;

            if (slot < 0 || slot >= names.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(act), act, "No clip name for that act.");
            }

            return slot;
        }
    }
}
