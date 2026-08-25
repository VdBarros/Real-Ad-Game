using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct TapCandidate : IEquatable<TapCandidate>
    {
        public TapCandidate(int nodeId, ScreenPoint point, float depth)
        {
            if (nodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "Node ids are dense and start at zero.");
            }

            NodeId = nodeId;
            Point = point;
            Depth = depth;
        }

        public int NodeId { get; }

        public ScreenPoint Point { get; }

        public float Depth { get; }

        public bool Equals(TapCandidate other)
        {
            return NodeId == other.NodeId && Point.Equals(other.Point) && Depth.Equals(other.Depth);
        }

        public override bool Equals(object obj)
        {
            return obj is TapCandidate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = NodeId;
                hash = (hash * 397) ^ Point.GetHashCode();
                hash = (hash * 397) ^ Depth.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "node ", NodeId.ToString(CultureInfo.InvariantCulture), " at ", Point.ToString());
        }
    }
}
