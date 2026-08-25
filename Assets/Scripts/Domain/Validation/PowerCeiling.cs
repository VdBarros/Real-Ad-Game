using System;

namespace Game.Domain
{
    public static class PowerCeiling
    {
        public const long Cap = 99999999L;

        public static long Of(LevelGraph level, int startingPower)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (startingPower < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingPower), startingPower, "A run begins holding power.");
            }

            long gains = startingPower;
            long product = 1;

            foreach (var node in level.Decisions.Nodes)
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
