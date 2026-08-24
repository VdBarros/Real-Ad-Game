using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class AdversaryPanel
    {
        static readonly AdversaryPolicy[] All =
        {
            AdversaryPolicy.MultiplierFirst,
            AdversaryPolicy.AdditiveFirst,
            AdversaryPolicy.EnemyFirst,
            AdversaryPolicy.BiggestAdditiveFirst,
            AdversaryPolicy.BiggestMultiplierFirst
        };

        static readonly NodeType[] MultiplierLed =
        {
            NodeType.Multiplier, NodeType.Additive, NodeType.Enemy
        };

        static readonly NodeType[] AdditiveLed =
        {
            NodeType.Additive, NodeType.Multiplier, NodeType.Enemy
        };

        static readonly NodeType[] EnemyLed =
        {
            NodeType.Enemy, NodeType.Multiplier, NodeType.Additive
        };

        public static IReadOnlyList<AdversaryPolicy> Policies
        {
            get { return All; }
        }

        public static StallReport FirstStall(LevelGraph level, PowerTuning tuning)
        {
            return FirstStall(BoardOf(level, tuning), tuning);
        }

        public static StallReport Walk(LevelGraph level, PowerTuning tuning, AdversaryPolicy policy)
        {
            return Walk(BoardOf(level, tuning), tuning, policy);
        }

        internal static StallReport FirstStall(ContentBoard board, PowerTuning tuning)
        {
            foreach (var policy in All)
            {
                var stall = Walk(board, tuning, policy);
                if (stall != null)
                {
                    return stall;
                }
            }

            return null;
        }

        internal static StallReport Walk(ContentBoard board, PowerTuning tuning, AdversaryPolicy policy)
        {
            var consumed = new bool[board.Count];
            var power = tuning.StartingPower;

            for (var step = 0; step <= board.Count; step++)
            {
                var reachable = board.ReachableFlags(consumed);
                var take = NextUnder(policy, board, reachable, consumed, power);
                if (take < 0)
                {
                    return Reported(policy, board, reachable, consumed, power);
                }

                power = board.PowerAfter(power, take);
                consumed[take] = true;
            }

            throw new InvalidOperationException(
                "An adversary walk consumes a node a step, so it cannot outrun the board.");
        }

        static int NextUnder(
            AdversaryPolicy policy,
            ContentBoard board,
            bool[] reachable,
            bool[] consumed,
            int power)
        {
            var biggestWins = policy == AdversaryPolicy.BiggestAdditiveFirst
                || policy == AdversaryPolicy.BiggestMultiplierFirst;

            foreach (var wanted in PriorityOf(policy))
            {
                var take = -1;
                for (var nodeId = 0; nodeId < board.Count; nodeId++)
                {
                    if (consumed[nodeId] || !reachable[nodeId] || board.TypeOf(nodeId) != wanted)
                    {
                        continue;
                    }

                    var value = board.ValueOf(nodeId);
                    if (wanted == NodeType.Enemy)
                    {
                        if (power > value && (take < 0 || value < board.ValueOf(take)))
                        {
                            take = nodeId;
                        }
                    }
                    else if (take < 0
                        || (biggestWins ? value > board.ValueOf(take) : value < board.ValueOf(take)))
                    {
                        take = nodeId;
                    }
                }

                if (take >= 0)
                {
                    return take;
                }
            }

            return -1;
        }

        static NodeType[] PriorityOf(AdversaryPolicy policy)
        {
            switch (policy)
            {
                case AdversaryPolicy.AdditiveFirst:
                case AdversaryPolicy.BiggestAdditiveFirst:
                    return AdditiveLed;

                case AdversaryPolicy.EnemyFirst:
                    return EnemyLed;

                default:
                    return MultiplierLed;
            }
        }

        static StallReport Reported(
            AdversaryPolicy policy,
            ContentBoard board,
            bool[] reachable,
            bool[] consumed,
            int power)
        {
            var consumedIds = new List<int>();
            var reachableIds = new List<int>();
            var stranded = new List<StrandedNode>();

            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (consumed[nodeId])
                {
                    consumedIds.Add(nodeId);
                }

                if (reachable[nodeId])
                {
                    reachableIds.Add(nodeId);
                }

                if (!consumed[nodeId] && board.IsContent(nodeId))
                {
                    stranded.Add(new StrandedNode(
                        nodeId, board.TypeOf(nodeId), board.ValueOf(nodeId), reachable[nodeId]));
                }
            }

            return stranded.Count == 0
                ? null
                : new StallReport(policy, power, consumedIds, reachableIds, stranded);
        }

        static ContentBoard BoardOf(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return ContentBoard.Of(level);
        }
    }
}
