using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class Spine
    {
        readonly List<int> nodeIds;
        readonly List<int> arrivals;

        Spine(List<int> nodeIds, List<int> arrivals, bool reachesTheBoss)
        {
            this.nodeIds = nodeIds;
            this.arrivals = arrivals;
            ReachesTheBoss = reachesTheBoss;
        }

        public IReadOnlyList<int> NodeIds
        {
            get { return nodeIds; }
        }

        public bool ReachesTheBoss { get; }

        public int Length
        {
            get { return nodeIds.Count; }
        }

        public bool Holds(int nodeId)
        {
            return nodeIds.Contains(nodeId);
        }

        public int ArrivalPowerAt(int index)
        {
            return arrivals[index];
        }

        public int ArrivalPowerOn(int nodeId)
        {
            var index = nodeIds.IndexOf(nodeId);
            return index < 0 ? -1 : arrivals[index];
        }

        public static Spine Of(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            var bossNodeId = -1;
            foreach (var node in level.Decisions.Nodes)
            {
                if (node.Type == NodeType.Boss)
                {
                    bossNodeId = node.Id;
                }
            }

            if (bossNodeId < 0)
            {
                throw new ArgumentException("A level with no boss has no Spine to truncate.", nameof(level));
            }

            return Of(ContentBoard.Of(level), tuning, bossNodeId);
        }

        internal static Spine Of(ContentBoard board, PowerTuning tuning, int bossNodeId)
        {
            var consumed = new bool[board.Count];
            var power = tuning.StartingPower;
            var bossPower = board.ValueOf(bossNodeId);
            var walked = new List<int>();
            var arrivals = new List<int>();

            for (var step = 0; step < board.Count; step++)
            {
                if (power > bossPower)
                {
                    return new Spine(walked, arrivals, true);
                }

                var take = PoorWalk.NextAffordable(board, board.ReachableFrom(consumed), consumed, power);
                if (take < 0)
                {
                    return new Spine(walked, arrivals, false);
                }

                walked.Add(take);
                arrivals.Add(power);
                power = board.PowerAfter(power, take);
                consumed[take] = true;
            }

            return new Spine(walked, arrivals, power > bossPower);
        }
    }
}
