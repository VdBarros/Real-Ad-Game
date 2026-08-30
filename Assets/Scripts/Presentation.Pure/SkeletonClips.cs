using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class SkeletonClips
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

        public const string Loose = "2H_Ranged_Shoot";

        public const string Sweep = "2H_Melee_Attack_Spin";

        public const string Fall = "Death_A";

        static readonly ClipTable table = new ClipTable(new[]
        {
            Idle, Walk, Retreat, Strike, Clash, Recoil, Take, Kick, Slice, Cleave, Loose, Sweep, Fall
        });

        public static ClipTable Table
        {
            get { return table; }
        }

        public static IReadOnlyList<string> Names
        {
            get { return table.Names; }
        }

        public static int Count
        {
            get { return table.Count; }
        }

        public static string NameOf(FigureAct act)
        {
            return table.NameOf(act);
        }

        public static bool Loops(FigureAct act)
        {
            return table.Loops(act);
        }

        public static bool Wants(string clip)
        {
            return table.Wants(clip);
        }

        public static bool LoopsOf(string clip)
        {
            return table.LoopsOf(clip);
        }
    }
}
