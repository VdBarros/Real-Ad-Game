using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Domain
{
    public sealed class StallReport
    {
        readonly List<int> consumed;
        readonly List<int> reachable;
        readonly List<StrandedNode> stranded;

        public StallReport(
            AdversaryPolicy policy,
            int power,
            IReadOnlyList<int> consumed,
            IReadOnlyList<int> reachable,
            IReadOnlyList<StrandedNode> stranded)
        {
            if (consumed == null)
            {
                throw new ArgumentNullException(nameof(consumed));
            }

            if (reachable == null)
            {
                throw new ArgumentNullException(nameof(reachable));
            }

            if (stranded == null)
            {
                throw new ArgumentNullException(nameof(stranded));
            }

            Policy = policy;
            Power = power;
            this.consumed = new List<int>(consumed);
            this.reachable = new List<int>(reachable);
            this.stranded = new List<StrandedNode>(stranded);
        }

        public AdversaryPolicy Policy { get; }

        public int Power { get; }

        public IReadOnlyList<int> Consumed
        {
            get { return consumed; }
        }

        public IReadOnlyList<int> Reachable
        {
            get { return reachable; }
        }

        public IReadOnlyList<StrandedNode> Stranded
        {
            get { return stranded; }
        }

        public override string ToString()
        {
            var description = new StringBuilder();
            description.Append(Policy);
            description.Append(" stalled at power ");
            description.Append(Power.ToString(CultureInfo.InvariantCulture));
            description.Append(" having consumed ");
            Append(description, consumed);
            description.Append(", reaching ");
            Append(description, reachable);
            description.Append(", stranding ");

            for (var index = 0; index < stranded.Count; index++)
            {
                description.Append(index == 0 ? string.Empty : ", ");
                description.Append(stranded[index]);
            }

            return description.ToString();
        }

        static void Append(StringBuilder description, List<int> nodeIds)
        {
            if (nodeIds.Count == 0)
            {
                description.Append("nothing");
                return;
            }

            for (var index = 0; index < nodeIds.Count; index++)
            {
                description.Append(index == 0 ? "#" : ", #");
                description.Append(nodeIds[index].ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
