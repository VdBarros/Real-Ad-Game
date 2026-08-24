using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Domain
{
    public sealed class OracleStall
    {
        readonly List<int> consumed;
        readonly List<StrandedNode> stranded;

        public OracleStall(int power, IReadOnlyList<int> consumed, IReadOnlyList<StrandedNode> stranded)
        {
            if (consumed == null)
            {
                throw new ArgumentNullException(nameof(consumed));
            }

            if (stranded == null)
            {
                throw new ArgumentNullException(nameof(stranded));
            }

            Power = power;
            this.consumed = new List<int>(consumed);
            this.stranded = new List<StrandedNode>(stranded);
        }

        public int Power { get; }

        public IReadOnlyList<int> Consumed
        {
            get { return consumed; }
        }

        public IReadOnlyList<StrandedNode> Stranded
        {
            get { return stranded; }
        }

        public override string ToString()
        {
            var description = new StringBuilder();
            description.Append("stalled at power ");
            description.Append(Power.ToString(CultureInfo.InvariantCulture));
            description.Append(" having consumed ");

            if (consumed.Count == 0)
            {
                description.Append("nothing");
            }
            else
            {
                for (var index = 0; index < consumed.Count; index++)
                {
                    description.Append(index == 0 ? "#" : ", #");
                    description.Append(consumed[index].ToString(CultureInfo.InvariantCulture));
                }
            }

            description.Append(", stranding ");
            for (var index = 0; index < stranded.Count; index++)
            {
                description.Append(index == 0 ? string.Empty : ", ");
                description.Append(stranded[index]);
            }

            return description.ToString();
        }
    }
}
