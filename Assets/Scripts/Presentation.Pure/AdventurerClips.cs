using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class AdventurerClips
    {
        public const string Idle = "Idle_A";

        public const string Walk = "Walking_A";

        public const string Retreat = "Walking_Backwards";

        public const string Strike = "Melee_1H_Attack_Chop";

        public const string Clash = "Melee_Block_Hit";

        public const string Recoil = "Hit_A";

        public const string Take = "PickUp";

        public const string Kick = "Melee_Unarmed_Attack_Kick";

        public const string Slice = "Melee_1H_Attack_Slice_Diagonal";

        public const string Cleave = "Melee_2H_Attack_Chop";

        public const string Loose = "Ranged_Bow_Draw";

        public const string Cast = "Ranged_Magic_Summon";

        public const string Fall = "Death_A";

        static readonly ClipTable table = new ClipTable(new[]
        {
            Idle, Walk, Retreat, Strike, Clash, Recoil, Take, Kick, Slice, Cleave, Loose, Cast, Fall
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
