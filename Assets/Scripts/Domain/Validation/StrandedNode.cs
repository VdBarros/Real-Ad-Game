using System.Globalization;

namespace Game.Domain
{
    public sealed class StrandedNode
    {
        public StrandedNode(int nodeId, NodeType type, int value, bool reachable)
        {
            NodeId = nodeId;
            Type = type;
            Value = value;
            Reachable = reachable;
        }

        public int NodeId { get; }

        public NodeType Type { get; }

        public int Value { get; }

        public bool Reachable { get; }

        public override string ToString()
        {
            return string.Concat(
                "#",
                NodeId.ToString(CultureInfo.InvariantCulture),
                " ",
                Type.ToString(),
                "(",
                Value.ToString(CultureInfo.InvariantCulture),
                Reachable ? ") in reach" : ") out of reach");
        }
    }
}
