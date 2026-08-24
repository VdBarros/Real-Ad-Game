using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class InvariantAOracle
    {
        public const int DefaultStateBudget = 200000;

        public const int WidestBoard = 30;

        readonly struct State
        {
            public State(int mask, int power)
            {
                Mask = mask;
                Power = power;
            }

            public int Mask { get; }

            public int Power { get; }
        }

        public static OracleVerdict Sweep(LevelGraph level, PowerTuning tuning)
        {
            return Sweep(level, tuning, DefaultStateBudget);
        }

        public static OracleVerdict Sweep(LevelGraph level, PowerTuning tuning, int stateBudget)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            RequireTuning(tuning);
            return Sweep(ContentBoard.Of(level), tuning, stateBudget);
        }

        internal static OracleVerdict Sweep(ContentBoard board, PowerTuning tuning, int stateBudget)
        {
            if (stateBudget < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stateBudget), stateBudget, "The oracle explores at least the state it starts in.");
            }

            var content = ContentOf(board);
            if (content.Count > WidestBoard)
            {
                throw new ArgumentException(
                    "The oracle indexes content into a " + WidestBoard + "-slot mask, and this level carries "
                    + content.Count + ".",
                    nameof(board));
            }

            var full = (1 << content.Count) - 1;
            var seen = new HashSet<long> { KeyOf(0, tuning.StartingPower) };
            var frontier = new List<State> { new State(0, tuning.StartingPower) };

            var stalls = 0;
            var explored = 0;
            OracleStall firstStall = null;

            while (frontier.Count > 0)
            {
                if (seen.Count > stateBudget)
                {
                    return new OracleVerdict(stalls, firstStall, seen.Count, explored, aborted: true);
                }

                var state = frontier[frontier.Count - 1];
                frontier.RemoveAt(frontier.Count - 1);
                explored++;

                var consumed = ConsumedFlags(board, content, state.Mask);
                var reachable = board.ReachableFlags(consumed);
                var moves = 0;

                for (var slot = 0; slot < content.Count; slot++)
                {
                    var nodeId = content[slot];
                    if ((state.Mask >> slot & 1) == 1 || !reachable[nodeId])
                    {
                        continue;
                    }

                    if (board.TypeOf(nodeId) == NodeType.Enemy && state.Power <= board.ValueOf(nodeId))
                    {
                        continue;
                    }

                    moves++;
                    var next = new State(state.Mask | 1 << slot, board.PowerAfter(state.Power, nodeId));
                    if (seen.Add(KeyOf(next.Mask, next.Power)))
                    {
                        frontier.Add(next);
                    }
                }

                if (moves > 0 || state.Mask == full)
                {
                    continue;
                }

                stalls++;
                if (firstStall == null)
                {
                    firstStall = StallAt(board, consumed, reachable, state.Power);
                }
            }

            return new OracleVerdict(stalls, firstStall, seen.Count, explored, aborted: false);
        }

        static List<int> ContentOf(ContentBoard board)
        {
            var content = new List<int>();
            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (board.IsContent(nodeId))
                {
                    content.Add(nodeId);
                }
            }

            return content;
        }

        static bool[] ConsumedFlags(ContentBoard board, List<int> content, int mask)
        {
            var consumed = new bool[board.Count];
            for (var slot = 0; slot < content.Count; slot++)
            {
                if ((mask >> slot & 1) == 1)
                {
                    consumed[content[slot]] = true;
                }
            }

            return consumed;
        }

        static OracleStall StallAt(ContentBoard board, bool[] consumed, bool[] reachable, int power)
        {
            var consumedIds = new List<int>();
            var stranded = new List<StrandedNode>();

            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (consumed[nodeId])
                {
                    consumedIds.Add(nodeId);
                }
                else if (board.IsContent(nodeId))
                {
                    stranded.Add(new StrandedNode(
                        nodeId, board.TypeOf(nodeId), board.ValueOf(nodeId), reachable[nodeId]));
                }
            }

            return new OracleStall(power, consumedIds, stranded);
        }

        static long KeyOf(int mask, int power)
        {
            return (long)mask << 32 | (uint)power;
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
