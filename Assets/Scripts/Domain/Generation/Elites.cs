using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class Elites
    {
        public static IReadOnlyList<int> Of(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            var cheapestEntry = new Dictionary<int, int>();
            foreach (var region in PowerEnvelope.Of(level, tuning).Regions)
            {
                cheapestEntry[region.RegionId] = region.CheapestEntry;
            }

            var locked = new List<int>();
            foreach (var node in level.Decisions.Nodes)
            {
                if (node.Type != NodeType.Enemy)
                {
                    continue;
                }

                int entry;
                if (cheapestEntry.TryGetValue(level.RegionOf(node.Id), out entry) && entry <= node.Value)
                {
                    locked.Add(node.Id);
                }
            }

            return locked;
        }
    }
}
