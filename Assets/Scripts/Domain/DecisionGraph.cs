using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class DecisionGraph : IEquatable<DecisionGraph>
    {
        readonly List<DecisionNode> nodes;
        readonly List<Corridor> corridors;
        readonly Dictionary<TilePosition, DecisionNode> nodeByPosition;
        readonly List<List<Corridor>> corridorsByNode;
        readonly List<List<int>> neighboursByNode;

        public DecisionGraph(IEnumerable<DecisionNode> nodes, IEnumerable<Corridor> corridors)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (corridors == null)
            {
                throw new ArgumentNullException(nameof(corridors));
            }

            this.nodes = new List<DecisionNode>(nodes);
            this.corridors = new List<Corridor>(corridors);

            for (var index = 0; index < this.nodes.Count; index++)
            {
                if (this.nodes[index].Id != index)
                {
                    throw new ArgumentException(
                        "Node ids are dense and index their own list, but node at index "
                        + index + " carries id " + this.nodes[index].Id + ".",
                        nameof(nodes));
                }

                if (index > 0 && this.nodes[index - 1].Position.CompareTo(this.nodes[index].Position) >= 0)
                {
                    throw new ArgumentException(
                        "Ids are assigned by a (floor, y, x) sweep, so node " + index + " at "
                        + this.nodes[index].Position + " cannot follow "
                        + this.nodes[index - 1].Position + ".",
                        nameof(nodes));
                }
            }

            this.corridors.Sort(CompareCorridors);

            nodeByPosition = new Dictionary<TilePosition, DecisionNode>();
            foreach (var node in this.nodes)
            {
                nodeByPosition.Add(node.Position, node);
            }

            corridorsByNode = new List<List<Corridor>>(this.nodes.Count);
            neighboursByNode = new List<List<int>>(this.nodes.Count);
            for (var index = 0; index < this.nodes.Count; index++)
            {
                corridorsByNode.Add(new List<Corridor>());
                neighboursByNode.Add(new List<int>());
            }

            for (var index = 0; index < this.corridors.Count; index++)
            {
                var corridor = this.corridors[index];
                if (corridor.HighNodeId >= this.nodes.Count)
                {
                    throw new ArgumentException(
                        "Corridor " + corridor + " ends at a node that does not exist.",
                        nameof(corridors));
                }

                if (index > 0 && CompareCorridors(this.corridors[index - 1], corridor) == 0)
                {
                    throw new ArgumentException(
                        "Two corridors join the same pair of nodes: " + corridor + ".",
                        nameof(corridors));
                }

                corridorsByNode[corridor.LowNodeId].Add(corridor);
                corridorsByNode[corridor.HighNodeId].Add(corridor);
                neighboursByNode[corridor.LowNodeId].Add(corridor.HighNodeId);
                neighboursByNode[corridor.HighNodeId].Add(corridor.LowNodeId);
            }

            foreach (var neighbours in neighboursByNode)
            {
                neighbours.Sort();
            }

            foreach (var attached in corridorsByNode)
            {
                attached.Sort(CompareCorridors);
            }
        }

        public IReadOnlyList<DecisionNode> Nodes
        {
            get { return nodes; }
        }

        public IReadOnlyList<Corridor> Corridors
        {
            get { return corridors; }
        }

        public DecisionNode Node(int nodeId)
        {
            RequireNode(nodeId);
            return nodes[nodeId];
        }

        public DecisionNode NodeAt(TilePosition position)
        {
            DecisionNode node;
            return nodeByPosition.TryGetValue(position, out node) ? node : null;
        }

        public IReadOnlyList<int> NeighboursOf(int nodeId)
        {
            RequireNode(nodeId);
            return neighboursByNode[nodeId];
        }

        public IReadOnlyList<Corridor> CorridorsOf(int nodeId)
        {
            RequireNode(nodeId);
            return corridorsByNode[nodeId];
        }

        public Corridor CorridorBetween(int firstNodeId, int secondNodeId)
        {
            RequireNode(firstNodeId);
            RequireNode(secondNodeId);

            foreach (var corridor in corridorsByNode[firstNodeId])
            {
                if (corridor.Joins(firstNodeId, secondNodeId))
                {
                    return corridor;
                }
            }

            return null;
        }

        void RequireNode(int nodeId)
        {
            if (nodeId < 0 || nodeId >= nodes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "No node carries that id.");
            }
        }

        public bool Equals(DecisionGraph other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (nodes.Count != other.nodes.Count || corridors.Count != other.corridors.Count)
            {
                return false;
            }

            for (var index = 0; index < nodes.Count; index++)
            {
                if (!nodes[index].Equals(other.nodes[index]))
                {
                    return false;
                }
            }

            for (var index = 0; index < corridors.Count; index++)
            {
                if (!corridors[index].Equals(other.corridors[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DecisionGraph);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = nodes.Count;
                foreach (var node in nodes)
                {
                    hash = (hash * 397) ^ node.GetHashCode();
                }

                foreach (var corridor in corridors)
                {
                    hash = (hash * 397) ^ corridor.GetHashCode();
                }

                return hash;
            }
        }

        static int CompareCorridors(Corridor left, Corridor right)
        {
            var byLow = left.LowNodeId.CompareTo(right.LowNodeId);
            return byLow != 0 ? byLow : left.HighNodeId.CompareTo(right.HighNodeId);
        }
    }
}
