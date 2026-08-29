using System;

namespace Game.Domain
{
    public static class MultiplierProduct
    {
        public static long Of(LevelGraph level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            long product = 1;

            foreach (var node in level.Decisions.Nodes)
            {
                if (node.Type == NodeType.Multiplier)
                {
                    product = Saturate(product * node.Value);
                }
            }

            return product;
        }

        internal static long Of(ContentBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            long product = 1;

            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (board.TypeOf(nodeId) == NodeType.Multiplier)
                {
                    product = Saturate(product * board.ValueOf(nodeId));
                }
            }

            return product;
        }

        static long Saturate(long value)
        {
            return value > PowerCeiling.Cap || value < 0 ? PowerCeiling.Cap : value;
        }
    }
}
