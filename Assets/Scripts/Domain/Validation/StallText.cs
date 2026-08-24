using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Domain
{
    static class StallText
    {
        public static void Ids(StringBuilder description, IReadOnlyList<int> nodeIds)
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

        public static void Nodes(StringBuilder description, IReadOnlyList<StrandedNode> stranded)
        {
            for (var index = 0; index < stranded.Count; index++)
            {
                description.Append(index == 0 ? string.Empty : ", ");
                description.Append(stranded[index]);
            }
        }
    }
}
