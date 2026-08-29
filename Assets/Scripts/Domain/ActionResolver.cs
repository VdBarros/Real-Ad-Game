using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class ActionResolver
    {
        public static ActionResult Resolve(RunState state, int targetNodeId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return Along(state, state.RouteTo(targetNodeId));
        }

        public static ActionResult Along(RunState state, IReadOnlyList<int> route)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (route == null || state.IsLevelComplete)
            {
                return ActionResult.Rejected(state);
            }

            if (route.Count == 0 || route[0] != state.PositionNodeId)
            {
                throw new ArgumentException(
                    "A route is walked from the node the run stands on, not from node "
                    + (route.Count == 0 ? "nowhere" : route[0].ToString()) + ".",
                    nameof(route));
            }

            var decisions = state.Level.Decisions;
            var outcome = ActionOutcome.Walked;
            var position = state.PositionNodeId;
            var power = state.Power;
            var consumed = state.CopyConsumed();

            for (var step = 1; step < route.Count; step++)
            {
                var node = decisions.Node(route[step]);
                if (consumed[node.Id])
                {
                    position = node.Id;
                    continue;
                }

                switch (node.Type)
                {
                    case NodeType.Additive:
                        power += node.Value;
                        consumed[node.Id] = true;
                        break;

                    case NodeType.Multiplier:
                        power *= node.Value;
                        consumed[node.Id] = true;
                        break;

                    case NodeType.Enemy:
                    case NodeType.Boss:
                        if (power <= node.Value)
                        {
                            return ActionResult.Of(
                                power == node.Value ? ActionOutcome.Tie : ActionOutcome.Loss,
                                state.After(position, power, consumed),
                                route);
                        }

                        power += node.Value;
                        consumed[node.Id] = true;
                        outcome = ActionOutcome.Win;
                        break;
                }

                position = node.Id;
            }

            return ActionResult.Of(outcome, state.After(position, power, consumed), route);
        }
    }
}
