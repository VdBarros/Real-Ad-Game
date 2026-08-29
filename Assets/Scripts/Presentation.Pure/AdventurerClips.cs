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

        public const string Kick = "Unarmed_Melee_Attack_Kick";

        public const string Slice = "1H_Melee_Attack_Slice_Diagonal";

        public const string Cleave = "2H_Melee_Attack_Chop";

        public const string Thrust = "2H_Melee_Attack_Stab";

        public const string Sweep = "2H_Melee_Attack_Spin";

        public const string Fall = "Death_A";

        readonly struct Playing
        {
            public Playing(FigureAct act, string clip, bool loops)
            {
                Act = act;
                Clip = clip;
                Loops = loops;
            }

            public FigureAct Act { get; }

            public string Clip { get; }

            public bool Loops { get; }
        }

        static readonly Playing[] table =
        {
            new Playing(FigureAct.Idle, Idle, true),
            new Playing(FigureAct.Walk, Walk, true),
            new Playing(FigureAct.Retreat, Retreat, true),
            new Playing(FigureAct.Strike, Strike, false),
            new Playing(FigureAct.Clash, Clash, false),
            new Playing(FigureAct.Recoil, Recoil, false),
            new Playing(FigureAct.Take, Take, false),
            new Playing(FigureAct.Kick, Kick, false),
            new Playing(FigureAct.Slice, Slice, false),
            new Playing(FigureAct.Cleave, Cleave, false),
            new Playing(FigureAct.Thrust, Thrust, false),
            new Playing(FigureAct.Sweep, Sweep, false),
            new Playing(FigureAct.Fall, Fall, false)
        };

        static readonly string[] names = Named();

        public static IReadOnlyList<string> Names
        {
            get { return names; }
        }

        public static int Count
        {
            get { return table.Length; }
        }

        public static string NameOf(FigureAct act)
        {
            return Of(act).Clip;
        }

        public static bool Loops(FigureAct act)
        {
            return Of(act).Loops;
        }

        public static bool Wants(string clip)
        {
            return Slot(clip) >= 0;
        }

        public static bool LoopsOf(string clip)
        {
            var slot = Slot(clip);

            return slot >= 0 && table[slot].Loops;
        }

        static string[] Named()
        {
            var named = new string[table.Length];

            for (var slot = 0; slot < table.Length; slot++)
            {
                named[slot] = table[slot].Clip;
            }

            return named;
        }

        static Playing Of(FigureAct act)
        {
            foreach (var playing in table)
            {
                if (playing.Act == act)
                {
                    return playing;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(act), act, "No clip name for that act.");
        }

        static int Slot(string clip)
        {
            if (string.IsNullOrEmpty(clip))
            {
                return -1;
            }

            for (var slot = 0; slot < table.Length; slot++)
            {
                if (string.Equals(table[slot].Clip, clip, StringComparison.Ordinal))
                {
                    return slot;
                }
            }

            return -1;
        }
    }
}
