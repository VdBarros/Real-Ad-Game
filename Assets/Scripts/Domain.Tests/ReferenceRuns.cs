namespace Game.Domain.Tests
{
    static class ReferenceRuns
    {
        public static int Stingy(PlacedLevel level)
        {
            var decisions = level.Graph.Decisions;
            var spine = Spine.Of(level.Graph, level.Tuning);
            var power = level.StartingPower;

            for (var index = 0; index < spine.Length; index++)
            {
                var node = decisions.Node(spine.NodeIds[index]);
                power = node.Type == NodeType.Multiplier ? power * node.Value : power + node.Value;
            }

            return power + level.BossPower;
        }

        public static int Routed(PlacedLevel level)
        {
            var decisions = level.Graph.Decisions;
            var run = RunState.Begin(level.Graph, level.StartingPower);

            while (true)
            {
                var take = Dearest(run, decisions, NodeType.Additive);
                if (take < 0)
                {
                    take = Cheapest(run, decisions, NodeType.Enemy);
                }

                if (take < 0)
                {
                    take = Dearest(run, decisions, NodeType.Multiplier);
                }

                if (take < 0)
                {
                    return Finished(run, decisions);
                }

                var result = ActionResolver.Resolve(run, take);
                if (result.Outcome == ActionOutcome.Rejected || result.State.Power <= run.Power)
                {
                    return Finished(run, decisions);
                }

                run = result.State;
            }
        }

        static int Finished(RunState run, DecisionGraph decisions)
        {
            var boss = Cheapest(run, decisions, NodeType.Boss);
            return boss < 0 ? run.Power : ActionResolver.Resolve(run, boss).State.Power;
        }

        static int Dearest(RunState run, DecisionGraph decisions, NodeType wanted)
        {
            var take = -1;
            foreach (var nodeId in run.ReachableNodes)
            {
                var node = decisions.Node(nodeId);
                if (node.Type != wanted || run.IsConsumed(nodeId))
                {
                    continue;
                }

                if (take < 0 || node.Value > decisions.Node(take).Value)
                {
                    take = nodeId;
                }
            }

            return take;
        }

        static int Cheapest(RunState run, DecisionGraph decisions, NodeType wanted)
        {
            var take = -1;
            foreach (var nodeId in run.ReachableNodes)
            {
                var node = decisions.Node(nodeId);
                if (node.Type != wanted || run.IsConsumed(nodeId) || run.Power <= node.Value)
                {
                    continue;
                }

                if (take < 0 || node.Value < decisions.Node(take).Value)
                {
                    take = nodeId;
                }
            }

            return take;
        }
    }
}
