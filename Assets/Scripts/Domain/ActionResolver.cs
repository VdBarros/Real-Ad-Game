using System;

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

            var route = state.RouteTo(targetNodeId);
            if (route == null || state.IsLevelComplete)
            {
                return ActionResult.Rejected(state);
            }

            var outcome = ActionOutcome.Walked;
            var position = state.PositionNodeId;
            var power = state.Power;
            var consumed = state.CopyConsumed();

            for (var step = 1; step < route.Count; step++)
            {
                var node = state.Level.Decisions.Node(route[step]);
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
