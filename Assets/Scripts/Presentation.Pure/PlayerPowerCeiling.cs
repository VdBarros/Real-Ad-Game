using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class PlayerPowerCeiling
    {
        public const long Cap = 99999999L;

        public static long Of(LevelGraph graph, int startingPower)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (startingPower < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingPower), startingPower, "A run begins holding power.");
            }

            long gains = startingPower;
            long product = 1;

            foreach (var node in graph.Decisions.Nodes)
            {
                switch (node.Type)
                {
                    case NodeType.Additive:
                    case NodeType.Enemy:
                    case NodeType.Boss:
                        gains += node.Value;
                        break;
                    case NodeType.Multiplier:
                        product = Saturate(product * node.Value);
                        break;
                }
            }

            return Saturate(gains * product);
        }

        static long Saturate(long value)
        {
            return value > Cap || value < 0 ? Cap : value;
        }
    }
}
