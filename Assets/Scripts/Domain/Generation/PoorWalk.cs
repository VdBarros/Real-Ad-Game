using System.Collections.Generic;

namespace Game.Domain
{
    static class PoorWalk
    {
        public static int Next(ContentBoard board, List<int> reachable, bool[] consumed, int power)
        {
            var multiplier = -1;
            var additive = -1;
            var affordable = -1;
            var any = -1;

            foreach (var nodeId in reachable)
            {
                if (consumed[nodeId] || !board.IsContent(nodeId))
                {
                    continue;
                }

                if (any < 0)
                {
                    any = nodeId;
                }

                var type = board.TypeOf(nodeId);
                if (type == NodeType.Multiplier && multiplier < 0)
                {
                    multiplier = nodeId;
                }

                if (type == NodeType.Additive && additive < 0)
                {
                    additive = nodeId;
                }

                if (type == NodeType.Enemy
                    && affordable < 0
                    && (!board.IsMinted(nodeId) || power > board.ValueOf(nodeId)))
                {
                    affordable = nodeId;
                }
            }

            if (multiplier >= 0)
            {
                return multiplier;
            }

            if (additive >= 0)
            {
                return additive;
            }

            return affordable >= 0 ? affordable : any;
        }

        public static int NextAffordable(ContentBoard board, List<int> reachable, bool[] consumed, int power)
        {
            var take = Next(board, reachable, consumed, power);
            if (take < 0)
            {
                return -1;
            }

            return board.TypeOf(take) == NodeType.Enemy && power <= board.ValueOf(take) ? -1 : take;
        }
    }
}
