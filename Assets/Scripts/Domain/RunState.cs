using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class RunState : IEquatable<RunState>
    {
        const int Unvisited = -1;

        readonly bool[] consumed;
        readonly List<int> consumedNodes;
        int[] arrivedFrom;
        List<int> reachableNodes;

        RunState(LevelGraph level, int positionNodeId, int power, bool[] consumed)
        {
            Level = level;
            PositionNodeId = positionNodeId;
            Power = power;
            this.consumed = consumed;

            consumedNodes = new List<int>();
            var bossHasFallen = false;
            foreach (var node in level.Decisions.Nodes)
            {
                if (!consumed[node.Id])
                {
                    continue;
                }

                consumedNodes.Add(node.Id);
                bossHasFallen |= node.Type == NodeType.Boss;
            }

            IsLevelComplete = bossHasFallen;
        }

        public static RunState Begin(LevelGraph level, int startingPower)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (startingPower < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingPower), startingPower, "A run begins holding power.");
            }

            var startNodeId = Unvisited;
            foreach (var node in level.Decisions.Nodes)
            {
                if (node.Type != NodeType.Start)
                {
                    continue;
                }

                if (startNodeId != Unvisited)
                {
                    throw new ArgumentException("A run needs exactly one Start to begin on.", nameof(level));
                }

                startNodeId = node.Id;
            }

            if (startNodeId == Unvisited)
            {
                throw new ArgumentException("A run needs exactly one Start to begin on.", nameof(level));
            }

            return new RunState(level, startNodeId, startingPower, new bool[level.Decisions.Nodes.Count]);
        }

        public LevelGraph Level { get; }

        public int PositionNodeId { get; }

        public int Power { get; }

        public bool IsLevelComplete { get; }

        public IReadOnlyList<int> ConsumedNodes
        {
            get { return consumedNodes; }
        }

        public IReadOnlyList<int> ReachableNodes
        {
            get
            {
                Explore();
                return reachableNodes;
            }
        }

        public bool IsConsumed(int nodeId)
        {
            RequireNode(nodeId);
            return consumed[nodeId];
        }

        public bool IsReachable(int nodeId)
        {
            RequireNode(nodeId);
            Explore();
            return arrivedFrom[nodeId] != Unvisited;
        }

        public IReadOnlyList<int> RouteTo(int nodeId)
        {
            if (!IsReachable(nodeId))
            {
                return null;
            }

            var route = new List<int>();
            for (var step = nodeId; step != PositionNodeId; step = arrivedFrom[step])
            {
                route.Add(step);
            }

            route.Add(PositionNodeId);
            route.Reverse();
            return route;
        }

        internal bool[] CopyConsumed()
        {
            return (bool[])consumed.Clone();
        }

        internal RunState After(int positionNodeId, int power, bool[] consumedAfterwards)
        {
            return new RunState(Level, positionNodeId, power, consumedAfterwards);
        }

        void Explore()
        {
            if (arrivedFrom != null)
            {
                return;
            }

            var count = Level.Decisions.Nodes.Count;
            var from = new int[count];
            for (var nodeId = 0; nodeId < count; nodeId++)
            {
                from[nodeId] = Unvisited;
            }

            var found = new List<int>();
            var queue = new Queue<int>();
            from[PositionNodeId] = PositionNodeId;
            queue.Enqueue(PositionNodeId);

            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                found.Add(nodeId);
                if (nodeId != PositionNodeId && BlocksPassage(nodeId))
                {
                    continue;
                }

                foreach (var neighbour in Level.Decisions.NeighboursOf(nodeId))
                {
                    if (from[neighbour] != Unvisited)
                    {
                        continue;
                    }

                    from[neighbour] = nodeId;
                    queue.Enqueue(neighbour);
                }
            }

            found.Sort();
            reachableNodes = found;
            arrivedFrom = from;
        }

        bool BlocksPassage(int nodeId)
        {
            if (consumed[nodeId])
            {
                return false;
            }

            var type = Level.Decisions.Node(nodeId).Type;
            return type == NodeType.Enemy || type == NodeType.Boss;
        }

        void RequireNode(int nodeId)
        {
            if (nodeId < 0 || nodeId >= consumed.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "No node carries that id.");
            }
        }

        public bool Equals(RunState other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (!ReferenceEquals(Level, other.Level)
                || PositionNodeId != other.PositionNodeId
                || Power != other.Power
                || consumedNodes.Count != other.consumedNodes.Count)
            {
                return false;
            }

            for (var index = 0; index < consumedNodes.Count; index++)
            {
                if (consumedNodes[index] != other.consumedNodes[index])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RunState);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = PositionNodeId;
                hash = (hash * 397) ^ Power;
                foreach (var nodeId in consumedNodes)
                {
                    hash = (hash * 397) ^ nodeId;
                }

                return hash;
            }
        }
    }
}
