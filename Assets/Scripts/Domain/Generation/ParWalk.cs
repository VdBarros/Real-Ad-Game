using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class ParWalk
    {
        public const int BeamWidth = 6;

        readonly List<int> targets;

        ParWalk(int finish, bool beatsTheBoss, List<int> targets)
        {
            Finish = finish;
            BeatsTheBoss = beatsTheBoss;
            this.targets = targets;
        }

        public int Finish { get; }

        public bool BeatsTheBoss { get; }

        public IReadOnlyList<int> Targets
        {
            get { return targets; }
        }

        public static ParWalk Richest(LevelGraph level, PowerTuning tuning)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
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
                throw new ArgumentException("A level with no boss has no richest walk.", nameof(level));
            }

            return Richest(level, tuning, bossNodeId);
        }

        internal static ParWalk Richest(LevelGraph level, PowerTuning tuning, int bossNodeId)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            var decisions = level.Decisions;
            var count = decisions.Nodes.Count;
            if (bossNodeId < 0 || bossNodeId >= count || decisions.Node(bossNodeId).Type != NodeType.Boss)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bossNodeId), bossNodeId, "A richest walk finishes on a boss.");
            }

            var types = new NodeType[count];
            var values = new int[count];
            var startNodeId = -1;
            foreach (var node in decisions.Nodes)
            {
                types[node.Id] = node.Type;
                values[node.Id] = node.Value;
                if (node.Type == NodeType.Start)
                {
                    startNodeId = node.Id;
                }
            }

            if (startNodeId < 0)
            {
                throw new ArgumentException("A richest walk begins on a start.", nameof(level));
            }

            var board = new Board(decisions, types, values, bossNodeId);
            var opening = new Step(null, -1, startNodeId, tuning.StartingPower, new bool[count]);
            var frontier = new List<Step> { opening };
            var grown = new List<Step>();
            Step landed = null;

            while (frontier.Count > 0)
            {
                grown.Clear();

                foreach (var step in frontier)
                {
                    board.Explore(step);

                    for (var nodeId = 0; nodeId < count; nodeId++)
                    {
                        if (nodeId != bossNodeId && (step.Consumed[nodeId] || !IsContent(types[nodeId])))
                        {
                            continue;
                        }

                        var taken = board.Reached(step, nodeId);
                        if (taken == null)
                        {
                            continue;
                        }

                        if (taken.Consumed[bossNodeId])
                        {
                            if (landed == null || taken.Power > landed.Power)
                            {
                                landed = taken;
                            }

                            continue;
                        }

                        if (taken.Power > step.Power)
                        {
                            taken.Rank = RankOf(taken, types, values);
                            grown.Add(taken);
                        }
                    }
                }

                frontier = Widest(grown, BeamWidth);
            }

            return landed == null
                ? new ParWalk(opening.Power, false, new List<int>())
                : new ParWalk(landed.Power, true, Chain(landed));
        }

        static bool IsContent(NodeType type)
        {
            return type == NodeType.Enemy || type == NodeType.Additive || type == NodeType.Multiplier;
        }

        static double RankOf(Step step, NodeType[] types, int[] values)
        {
            var reach = (double)step.Power;
            var product = 1.0;

            for (var nodeId = 0; nodeId < step.Consumed.Length; nodeId++)
            {
                if (step.Consumed[nodeId])
                {
                    continue;
                }

                switch (types[nodeId])
                {
                    case NodeType.Multiplier:
                        product *= values[nodeId];
                        break;

                    case NodeType.Additive:
                    case NodeType.Enemy:
                        reach += values[nodeId];
                        break;
                }
            }

            return reach * product;
        }

        static List<Step> Widest(List<Step> grown, int width)
        {
            var kept = new List<Step>(width);

            foreach (var step in grown)
            {
                var already = false;
                foreach (var held in kept)
                {
                    if (held.Position == step.Position && held.Power == step.Power)
                    {
                        already = true;
                        break;
                    }
                }

                if (already)
                {
                    continue;
                }

                var slot = kept.Count;
                while (slot > 0 && kept[slot - 1].Rank < step.Rank)
                {
                    slot--;
                }

                if (slot >= width)
                {
                    continue;
                }

                kept.Insert(slot, step);
                if (kept.Count > width)
                {
                    kept.RemoveAt(kept.Count - 1);
                }
            }

            return kept;
        }

        static List<int> Chain(Step landed)
        {
            var walked = new List<int>();
            for (var step = landed; step != null && step.Target >= 0; step = step.From)
            {
                walked.Add(step.Target);
            }

            walked.Reverse();
            return walked;
        }

        sealed class Board
        {
            readonly DecisionGraph decisions;
            readonly NodeType[] types;
            readonly int[] values;
            readonly int bossNodeId;
            readonly int[] arrivedFrom;
            readonly Queue<int> queue;
            readonly List<int> route;

            public Board(DecisionGraph decisions, NodeType[] types, int[] values, int bossNodeId)
            {
                this.decisions = decisions;
                this.types = types;
                this.values = values;
                this.bossNodeId = bossNodeId;
                arrivedFrom = new int[types.Length];
                queue = new Queue<int>();
                route = new List<int>();
            }

            public void Explore(Step step)
            {
                for (var nodeId = 0; nodeId < arrivedFrom.Length; nodeId++)
                {
                    arrivedFrom[nodeId] = -1;
                }

                queue.Clear();
                arrivedFrom[step.Position] = step.Position;
                queue.Enqueue(step.Position);

                while (queue.Count > 0)
                {
                    var nodeId = queue.Dequeue();
                    if (nodeId != step.Position && Blocks(step.Consumed, nodeId))
                    {
                        continue;
                    }

                    foreach (var neighbour in decisions.NeighboursOf(nodeId))
                    {
                        if (arrivedFrom[neighbour] >= 0)
                        {
                            continue;
                        }

                        arrivedFrom[neighbour] = nodeId;
                        queue.Enqueue(neighbour);
                    }
                }
            }

            public Step Reached(Step step, int targetNodeId)
            {
                if (arrivedFrom[targetNodeId] < 0)
                {
                    return null;
                }

                route.Clear();
                for (var walked = targetNodeId; walked != step.Position; walked = arrivedFrom[walked])
                {
                    route.Add(walked);
                }

                var consumed = (bool[])step.Consumed.Clone();
                var power = step.Power;
                var position = step.Position;

                for (var index = route.Count - 1; index >= 0; index--)
                {
                    var nodeId = route[index];
                    if (consumed[nodeId])
                    {
                        position = nodeId;
                        continue;
                    }

                    switch (types[nodeId])
                    {
                        case NodeType.Additive:
                            power += values[nodeId];
                            consumed[nodeId] = true;
                            break;

                        case NodeType.Multiplier:
                            power *= values[nodeId];
                            consumed[nodeId] = true;
                            break;

                        case NodeType.Enemy:
                        case NodeType.Boss:
                            if (power <= values[nodeId])
                            {
                                return new Step(step, targetNodeId, position, power, consumed);
                            }

                            power += values[nodeId];
                            consumed[nodeId] = true;
                            break;
                    }

                    position = nodeId;
                }

                return new Step(step, targetNodeId, position, power, consumed);
            }

            bool Blocks(bool[] consumed, int nodeId)
            {
                if (consumed[nodeId])
                {
                    return false;
                }

                return types[nodeId] == NodeType.Enemy || nodeId == bossNodeId;
            }
        }

        sealed class Step
        {
            public Step(Step from, int target, int position, int power, bool[] consumed)
            {
                From = from;
                Target = target;
                Position = position;
                Power = power;
                Consumed = consumed;
            }

            public Step From { get; }

            public int Target { get; }

            public int Position { get; }

            public int Power { get; }

            public bool[] Consumed { get; }

            public double Rank { get; set; }
        }
    }
}
