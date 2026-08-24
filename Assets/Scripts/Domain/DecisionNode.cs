using System;
using System.Globalization;

namespace Game.Domain
{
    public sealed class DecisionNode : IEquatable<DecisionNode>
    {
        public DecisionNode(int id, TilePosition position, NodeType type, int value)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Node ids are dense and start at zero.");
            }

            Id = id;
            Position = position;
            Type = type;
            Value = value;
        }

        public int Id { get; }

        public TilePosition Position { get; }

        public NodeType Type { get; }

        public int Value { get; }

        public bool Equals(DecisionNode other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Id == other.Id
                && Position.Equals(other.Position)
                && Type == other.Type
                && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DecisionNode);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Id;
                hash = (hash * 397) ^ Position.GetHashCode();
                hash = (hash * 397) ^ (int)Type;
                hash = (hash * 397) ^ Value;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "#",
                Id.ToString(CultureInfo.InvariantCulture),
                " ",
                Type.ToString(),
                "(",
                Value.ToString(CultureInfo.InvariantCulture),
                ") at ",
                Position.ToString());
        }
    }
}
