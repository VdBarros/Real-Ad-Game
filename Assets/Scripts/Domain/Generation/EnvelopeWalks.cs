namespace Game.Domain
{
    static class EnvelopeWalks
    {
        public static int CheapestUnlock(ContentBoard board, PowerTuning tuning, int regionId)
        {
            var consumed = new bool[board.Count];
            var power = tuning.StartingPower;

            for (var step = 0; step <= board.Count; step++)
            {
                var reachable = board.ReachableFlags(consumed);
                for (var nodeId = 0; nodeId < board.Count; nodeId++)
                {
                    if (reachable[nodeId] && board.RegionOf(nodeId) == regionId)
                    {
                        return power;
                    }
                }

                var take = -1;
                var cheapestGain = 0;
                for (var nodeId = 0; nodeId < board.Count; nodeId++)
                {
                    if (!reachable[nodeId] || consumed[nodeId] || !board.IsContent(nodeId))
                    {
                        continue;
                    }

                    var value = board.ValueOf(nodeId);
                    int gain;
                    if (board.TypeOf(nodeId) == NodeType.Multiplier)
                    {
                        gain = power * value - power;
                    }
                    else if (board.TypeOf(nodeId) == NodeType.Additive)
                    {
                        gain = value;
                    }
                    else if (power > value)
                    {
                        gain = value;
                    }
                    else
                    {
                        continue;
                    }

                    if (take < 0 || gain < cheapestGain)
                    {
                        take = nodeId;
                        cheapestGain = gain;
                    }
                }

                if (take < 0)
                {
                    return power;
                }

                power = board.PowerAfter(power, take);
                consumed[take] = true;
            }

            return power;
        }

        public static int RichestEntry(ContentBoard board, PowerTuning tuning, int regionId)
        {
            var consumed = new bool[board.Count];
            var power = tuning.StartingPower;

            for (var step = 0; step <= board.Count; step++)
            {
                var reachable = board.ReachableFlags(consumed);
                var additive = -1;
                var enemy = -1;
                var multiplier = -1;

                for (var nodeId = 0; nodeId < board.Count; nodeId++)
                {
                    if (!reachable[nodeId]
                        || consumed[nodeId]
                        || !board.IsContent(nodeId)
                        || board.RegionOf(nodeId) == regionId)
                    {
                        continue;
                    }

                    var value = board.ValueOf(nodeId);
                    switch (board.TypeOf(nodeId))
                    {
                        case NodeType.Additive:
                            if (additive < 0 || value > board.ValueOf(additive))
                            {
                                additive = nodeId;
                            }

                            break;

                        case NodeType.Multiplier:
                            if (multiplier < 0 || value > board.ValueOf(multiplier))
                            {
                                multiplier = nodeId;
                            }

                            break;

                        case NodeType.Enemy:
                            if (power > value && (enemy < 0 || value < board.ValueOf(enemy)))
                            {
                                enemy = nodeId;
                            }

                            break;
                    }
                }

                var take = additive >= 0 ? additive : enemy >= 0 ? enemy : multiplier;
                if (take < 0)
                {
                    return power;
                }

                power = board.PowerAfter(power, take);
                consumed[take] = true;
            }

            return power;
        }

        public static int ShortestPathPower(ContentBoard board, PowerTuning tuning, int bossNodeId, out bool blocked)
        {
            blocked = false;
            var route = board.ShortestRouteTo(bossNodeId);
            var power = tuning.StartingPower;
            if (route == null)
            {
                blocked = true;
                return power;
            }

            foreach (var nodeId in route)
            {
                if (nodeId == bossNodeId || !board.IsContent(nodeId))
                {
                    continue;
                }

                if (board.TypeOf(nodeId) == NodeType.Enemy && power <= board.ValueOf(nodeId))
                {
                    blocked = true;
                    return power;
                }

                power = board.PowerAfter(power, nodeId);
            }

            return power;
        }
    }
}
