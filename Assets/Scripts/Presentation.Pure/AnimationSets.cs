using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class AnimationSets
    {
        public const string RigNode = "Rig_Medium";

        public const string General = "Rig_Medium_General";

        public const string MovementBasic = "Rig_Medium_MovementBasic";

        public const string MovementAdvanced = "Rig_Medium_MovementAdvanced";

        public const string CombatMelee = "Rig_Medium_CombatMelee";

        static readonly string[] assets = { General, MovementBasic, MovementAdvanced, CombatMelee };

        public static IReadOnlyList<string> Assets
        {
            get { return assets; }
        }

        public static int Count
        {
            get { return assets.Length; }
        }

        public static bool Carries(string asset)
        {
            if (string.IsNullOrEmpty(asset))
            {
                return false;
            }

            for (var slot = 0; slot < assets.Length; slot++)
            {
                if (string.Equals(assets[slot], asset, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static string SetOf(FigureAct act)
        {
            switch (act)
            {
                case FigureAct.Idle:
                case FigureAct.Recoil:
                case FigureAct.Take:
                case FigureAct.Fall:
                    return General;
                case FigureAct.Walk:
                    return MovementBasic;
                case FigureAct.Retreat:
                    return MovementAdvanced;
                case FigureAct.Strike:
                case FigureAct.Clash:
                case FigureAct.Kick:
                case FigureAct.Slice:
                case FigureAct.Cleave:
                case FigureAct.Thrust:
                case FigureAct.Sweep:
                    return CombatMelee;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(act), act, "No animation set carries a clip for that act.");
            }
        }

        public static IReadOnlyList<string> ActsOf(string asset)
        {
            var carried = new List<string>();

            foreach (var act in FigureActs.All)
            {
                if (string.Equals(SetOf(act), asset, StringComparison.Ordinal))
                {
                    carried.Add(AdventurerClips.NameOf(act));
                }
            }

            return carried;
        }

        public static string Rebound(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (string.Equals(path, RigNode, StringComparison.Ordinal))
            {
                return AdventurerPack.RigNode;
            }

            return path.StartsWith(RigNode + "/", StringComparison.Ordinal)
                ? AdventurerPack.RigNode + path.Substring(RigNode.Length)
                : path;
        }
    }
}
