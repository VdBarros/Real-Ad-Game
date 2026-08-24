using System;

namespace Game.Domain
{
    public static class PowerBound
    {
        public static long Of(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            long additiveTotal = 0;
            long multiplierProduct = 1;
            foreach (var node in level.Decisions.Nodes)
            {
                if (node.Type == NodeType.Multiplier)
                {
                    multiplierProduct *= node.Value;
                }
                else if (node.Type == NodeType.Enemy || node.Type == NodeType.Additive)
                {
                    additiveTotal += node.Value;
                }
            }

            return tuning.StartingPower * multiplierProduct + additiveTotal;
        }

        internal static long Of(ContentBoard board, PowerTuning tuning)
        {
            long additiveTotal = 0;
            long multiplierProduct = 1;
            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                var type = board.TypeOf(nodeId);
                if (type == NodeType.Multiplier)
                {
                    multiplierProduct *= board.ValueOf(nodeId);
                }
                else if (type == NodeType.Enemy || type == NodeType.Additive)
                {
                    additiveTotal += board.ValueOf(nodeId);
                }
            }

            return tuning.StartingPower * multiplierProduct + additiveTotal;
        }
    }
}
