using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct BadgeSeat : IEquatable<BadgeSeat>
    {
        internal BadgeSeat(int nodeId, float lift, float opacity, int order)
        {
            NodeId = nodeId;
            Lift = lift;
            Opacity = opacity;
            Order = order;
        }

        public int NodeId { get; }

        public float Lift { get; }

        public float Opacity { get; }

        public int Order { get; }

        public bool IsStacked
        {
            get { return Lift > 0f; }
        }

        public bool IsFaded
        {
            get { return Opacity < 1f; }
        }

        public WorldPoint Rise
        {
            get
            {
                var up = IsoProjection.CameraUp;
                return new WorldPoint(up.X * Lift, up.Y * Lift, up.Z * Lift);
            }
        }

        public bool Equals(BadgeSeat other)
        {
            return NodeId == other.NodeId
                && Lift.Equals(other.Lift)
                && Opacity.Equals(other.Opacity)
                && Order == other.Order;
        }

        public override bool Equals(object obj)
        {
            return obj is BadgeSeat other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = NodeId;
                hash = (hash * 397) ^ Lift.GetHashCode();
                hash = (hash * 397) ^ Opacity.GetHashCode();
                hash = (hash * 397) ^ Order;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "node ",
                NodeId.ToString(CultureInfo.InvariantCulture),
                " lifted ",
                Lift.ToString("0.###", CultureInfo.InvariantCulture),
                " at ",
                Opacity.ToString("0.###", CultureInfo.InvariantCulture),
                " opacity, drawn ",
                Order.ToString(CultureInfo.InvariantCulture));
        }
    }
}
