using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class SolvabilityValidator
    {
        public static SolvabilityVerdict Validate(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            int bossNodeId;
            int offendingNodeId;
            var malformed = Structure(level, out bossNodeId, out offendingNodeId);
            if (malformed != SolvabilityReason.None)
            {
                return SolvabilityVerdict.Malformed(malformed, offendingNodeId);
            }

            return Validate(ContentBoard.Of(level), tuning, bossNodeId);
        }

        internal static SolvabilityVerdict Validate(ContentBoard board, PowerTuning tuning, int bossNodeId)
        {
            var bossPower = board.ValueOf(bossNodeId);
            var bound = PowerBound.Of(board, tuning);

            bool beelineBlocked;
            var beelinePower = EnvelopeWalks.ShortestPathPower(board, tuning, bossNodeId, out beelineBlocked);

            var offendingNodeId = FirstGatedBehindTheBoss(board, bossNodeId);
            var stall = AdversaryPanel.FirstStall(board, tuning);

            var reason = SolvabilityReason.None;
            if (offendingNodeId >= 0)
            {
                reason = SolvabilityReason.GatedBehindBoss;
            }
            else if (bossPower >= bound)
            {
                reason = SolvabilityReason.BossBeyondBound;
            }
            else if (bossPower <= beelinePower)
            {
                reason = SolvabilityReason.BossWithinReach;
            }
            else if (stall != null)
            {
                reason = SolvabilityReason.AdversaryStalled;
            }
            else if (MultiplierProduct.Of(board) > tuning.MultiplierProductCap)
            {
                reason = SolvabilityReason.MultiplierProductBeyondCap;
            }
            else if (DeadWalk.LongestOf(board) > Pace.DeadWalkBudgetSteps)
            {
                reason = SolvabilityReason.DeadWalkBeyondBudget;
            }

            return new SolvabilityVerdict(
                reason, offendingNodeId, bossNodeId, bossPower, bound, beelinePower, beelineBlocked, stall);
        }

        static SolvabilityReason Structure(LevelGraph level, out int bossNodeId, out int offendingNodeId)
        {
            bossNodeId = -1;
            offendingNodeId = -1;

            var starts = 0;
            var bosses = 0;
            var startNodeId = -1;
            var unassigned = -1;
            var outOfRange = -1;

            foreach (var node in level.Decisions.Nodes)
            {
                switch (node.Type)
                {
                    case NodeType.Start:
                        starts++;
                        startNodeId = node.Id;
                        break;

                    case NodeType.Boss:
                        bosses++;
                        bossNodeId = node.Id;
                        break;

                    case NodeType.Unassigned:
                        if (unassigned < 0)
                        {
                            unassigned = node.Id;
                        }

                        break;
                }

                if (outOfRange < 0 && !ValueFits(node))
                {
                    outOfRange = node.Id;
                }
            }

            if (starts == 0)
            {
                return SolvabilityReason.NoStart;
            }

            if (starts > 1)
            {
                return SolvabilityReason.ManyStarts;
            }

            if (bosses == 0)
            {
                return SolvabilityReason.NoBoss;
            }

            if (bosses > 1)
            {
                return SolvabilityReason.ManyBosses;
            }

            if (unassigned >= 0)
            {
                offendingNodeId = unassigned;
                return SolvabilityReason.NodeUnassigned;
            }

            if (outOfRange >= 0)
            {
                offendingNodeId = outOfRange;
                return SolvabilityReason.ValueOutOfRange;
            }

            var adrift = FirstAdrift(level, startNodeId);
            if (adrift >= 0)
            {
                offendingNodeId = adrift;
                return SolvabilityReason.NodeUnreachable;
            }

            return SolvabilityReason.None;
        }

        static bool ValueFits(DecisionNode node)
        {
            switch (node.Type)
            {
                case NodeType.Multiplier:
                    return node.Value >= 2;

                case NodeType.Enemy:
                case NodeType.Boss:
                case NodeType.Additive:
                    return node.Value >= 1;

                default:
                    return node.Value == 0;
            }
        }

        static int FirstAdrift(LevelGraph level, int startNodeId)
        {
            var decisions = level.Decisions;
            var seen = new bool[decisions.Nodes.Count];
            var order = new List<int> { startNodeId };
            seen[startNodeId] = true;

            for (var head = 0; head < order.Count; head++)
            {
                foreach (var neighbour in decisions.NeighboursOf(order[head]))
                {
                    if (seen[neighbour])
                    {
                        continue;
                    }

                    seen[neighbour] = true;
                    order.Add(neighbour);
                }
            }

            for (var nodeId = 0; nodeId < seen.Length; nodeId++)
            {
                if (!seen[nodeId])
                {
                    return nodeId;
                }
            }

            return -1;
        }

        static int FirstGatedBehindTheBoss(ContentBoard board, int bossNodeId)
        {
            var reachable = board.ReachableAround(bossNodeId);
            for (var nodeId = 0; nodeId < board.Count; nodeId++)
            {
                if (board.IsContent(nodeId) && !reachable[nodeId])
                {
                    return nodeId;
                }
            }

            return -1;
        }
    }
}
