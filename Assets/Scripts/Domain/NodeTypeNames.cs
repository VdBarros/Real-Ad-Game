using System;

namespace Game.Domain
{
    public static class NodeTypeNames
    {
        static readonly string[] ByValue =
        {
            "Unassigned",
            "Start",
            "Empty",
            "Enemy",
            "Boss",
            "Additive",
            "Multiplier"
        };

        public static string NameOf(NodeType type)
        {
            var value = (int)type;
            if (value < 0 || value >= ByValue.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "No written name for that node type.");
            }

            return ByValue[value];
        }

        public static NodeType TypeNamed(string name)
        {
            var value = Array.IndexOf(ByValue, name);
            if (value < 0)
            {
                throw new FormatException("\"" + name + "\" is not a node type.");
            }

            return (NodeType)value;
        }
    }
}
