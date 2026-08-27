using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class ClipComplaints
    {
        readonly HashSet<string> said = new HashSet<string>(StringComparer.Ordinal);

        public int Said
        {
            get { return said.Count; }
        }

        public bool ShouldSay(string clip)
        {
            if (string.IsNullOrEmpty(clip))
            {
                return false;
            }

            return said.Add(clip);
        }

        public bool HasSaid(string clip)
        {
            return !string.IsNullOrEmpty(clip) && said.Contains(clip);
        }

        public void Forget()
        {
            said.Clear();
        }
    }
}
