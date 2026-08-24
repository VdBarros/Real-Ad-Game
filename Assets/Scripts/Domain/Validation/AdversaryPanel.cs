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
            AdversaryPolicy.BiggestMultiplierFirst,
            AdversaryPolicy.AdditiveLast
        };

        static readonly NodeType[] MultiplierLed =
        {
            NodeType.Multiplier, NodeType.Additive, NodeType.Enemy
        };

        static readonly NodeType[] AdditiveLed =
        {
            NodeType.Additive, NodeType.Multiplier, NodeType.Enemy
        };

        static readonly NodeType[] MultiplierLedEnemySecond =
        {
            NodeType.Multiplier, NodeType.Enemy, NodeType.Additive
        };

        static readonly NodeType[] EnemyLed =
        {
            NodeType.Enemy, NodeType.Multiplier, NodeType.Additive
        };

        readonly struct Appetite
        {
            public Appetite(NodeType[] order, bool biggestWins)
            {
                Order = order;
                BiggestWins = biggestWins;
            }

            public NodeType[] Order { get; }

            public bool BiggestWins { get; }
        }

        public static IReadOnlyList<AdversaryPolicy> Policies
        {
            get { return All; }
        }

        public static StallReport FirstStall(LevelGraph level, PowerTuning tuning)
        {
            RequireTuning(tuning);
            return FirstStall(ContentBoard.Of(level), tuning);
        }

        public static StallReport Walk(LevelGraph level, PowerTuning tuning, AdversaryPolicy policy)
        {
            RequireTuning(tuning);
            return Walk(ContentBoard.Of(level), tuning, policy);
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

            while (true)
            {
                var reachable = board.ReachableFlags(consumed);
                var take = NextUnder(policy, board, reachable, consumed, power);
                if (take < 0)
                {
                    return StallIfStranded(policy, board, reachable, consumed, power);
                }

                power = board.PowerAfter(power, take);
                consumed[take] = true;
            }
        }

        static int NextUnder(
            AdversaryPolicy policy,
            ContentBoard board,
            bool[] reachable,
            bool[] consumed,
            int power)
        {
            var appetite = AppetiteOf(policy);

            foreach (var wanted in appetite.Order)
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
                        || (appetite.BiggestWins
                            ? value > board.ValueOf(take)
                            : value < board.ValueOf(take)))
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

        static Appetite AppetiteOf(AdversaryPolicy policy)
        {
            switch (policy)
            {
                case AdversaryPolicy.AdditiveFirst:
                    return new Appetite(AdditiveLed, biggestWins: false);

                case AdversaryPolicy.EnemyFirst:
                    return new Appetite(EnemyLed, biggestWins: false);

                case AdversaryPolicy.BiggestAdditiveFirst:
                    return new Appetite(AdditiveLed, biggestWins: true);

                case AdversaryPolicy.BiggestMultiplierFirst:
                    return new Appetite(MultiplierLed, biggestWins: true);

                case AdversaryPolicy.AdditiveLast:
                    return new Appetite(MultiplierLedEnemySecond, biggestWins: false);

                default:
                    return new Appetite(MultiplierLed, biggestWins: false);
            }
        }

        static StallReport StallIfStranded(
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

        static void RequireTuning(PowerTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }
        }
    }
}
