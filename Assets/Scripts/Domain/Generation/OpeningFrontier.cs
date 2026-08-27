using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class OpeningFrontier
    {
        readonly List<int> nodeIds;

        OpeningFrontier(List<int> nodeIds)
        {
            this.nodeIds = nodeIds;
        }

        public IReadOnlyList<int> NodeIds
        {
            get { return nodeIds; }
        }

        public int Count
        {
            get { return nodeIds.Count; }
        }

        public bool Holds(int nodeId)
        {
            return nodeIds.Contains(nodeId);
        }

        public static OpeningFrontier Of(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return Of(ContentBoard.Of(level), tuning);
        }

        internal static OpeningFrontier Of(ContentBoard board, PowerTuning tuning)
        {
            var power = tuning.StartingPower;
            var choices = new List<int>();

            foreach (var nodeId in board.ReachableFrom(new bool[board.Count]))
            {
                if (board.TypeOf(nodeId) == NodeType.Enemy && power > board.ValueOf(nodeId))
                {
                    choices.Add(nodeId);
                }
            }

            return new OpeningFrontier(choices);
        }
    }
}
