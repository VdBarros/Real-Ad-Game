using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class ClipTable
    {
        readonly string[] clips;

        public ClipTable(string[] named)
        {
            if (named == null || named.Length != FigureActs.Count)
            {
                throw new ArgumentException(
                    "A clip table names one clip for every act a figure can be cued into.", nameof(named));
            }

            clips = (string[])named.Clone();
        }

        public IReadOnlyList<string> Names
        {
            get { return clips; }
        }

        public int Count
        {
            get { return clips.Length; }
        }

        public string NameOf(FigureAct act)
        {
            var slot = FigureActs.SlotOf(act);

            if (slot < 0)
            {
                throw FigureActs.Stranger(act);
            }

            return clips[slot];
        }

        public bool Loops(FigureAct act)
        {
            return FigureActs.Loops(act);
        }

        public bool Wants(string clip)
        {
            return SlotOf(clip) >= 0;
        }

        public bool LoopsOf(string clip)
        {
            var slot = SlotOf(clip);

            return slot >= 0 && FigureActs.Loops(FigureActs.All[slot]);
        }

        public FigureAct ActOf(string clip)
        {
            var slot = SlotOf(clip);

            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(clip), clip, "No act plays a clip by that name.");
            }

            return FigureActs.All[slot];
        }

        int SlotOf(string clip)
        {
            if (string.IsNullOrEmpty(clip))
            {
                return -1;
            }

            for (var slot = 0; slot < clips.Length; slot++)
            {
                if (string.Equals(clips[slot], clip, StringComparison.Ordinal))
                {
                    return slot;
                }
            }

            return -1;
        }
    }
}
