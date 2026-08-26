using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Domain
{
    public static class LevelGraphWriter
    {
        public const int FormatVersion = 1;

        public static string Write(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var document = new StringBuilder();
            document.Append("{\n");
            document.Append("  \"version\": ").Append(Number(FormatVersion)).Append(",\n");
            document.Append("  \"seed\": ").Append(Number(graph.Seed)).Append(",\n");
            document.Append("  \"preset\": ").Append(Quoted(graph.Preset)).Append(",\n");
            WriteTiles(document, graph.Tiles.Tiles);
            WriteNodes(document, graph.Decisions.Nodes);
            WriteCorridors(document, graph.Decisions.Corridors);
            document.Append("}\n");
            return document.ToString();
        }

        public static byte[] WriteBytes(LevelGraph graph)
        {
            return new UTF8Encoding(false).GetBytes(Write(graph));
        }

        static void WriteTiles(StringBuilder document, IReadOnlyList<Tile> tiles)
        {
            OpenSection(document, "tiles", tiles.Count);
            for (var index = 0; index < tiles.Count; index++)
            {
                document.Append("    { ");
                AppendPosition(document, tiles[index].Position);
                document.Append(", \"region\": ").Append(Number(tiles[index].RegionId));
                document.Append(" }");
                CloseElement(document, index, tiles.Count);
            }

            CloseSection(document, tiles.Count, last: false);
        }

        static void WriteNodes(StringBuilder document, IReadOnlyList<DecisionNode> nodes)
        {
            OpenSection(document, "nodes", nodes.Count);
            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                document.Append("    { \"id\": ").Append(Number(node.Id)).Append(", ");
                AppendPosition(document, node.Position);
                document.Append(", \"type\": ").Append(Quoted(NodeTypeNames.NameOf(node.Type)));
                document.Append(", \"value\": ").Append(Number(node.Value));
                document.Append(" }");
                CloseElement(document, index, nodes.Count);
            }

            CloseSection(document, nodes.Count, last: false);
        }

        static void WriteCorridors(StringBuilder document, IReadOnlyList<Corridor> corridors)
        {
            OpenSection(document, "corridors", corridors.Count);
            for (var index = 0; index < corridors.Count; index++)
            {
                var corridor = corridors[index];
                document.Append("    { \"low\": ").Append(Number(corridor.LowNodeId));
                document.Append(", \"high\": ").Append(Number(corridor.HighNodeId));
                document.Append(", \"tiles\": [");
                for (var step = 0; step < corridor.TilePath.Count; step++)
                {
                    document.Append(step == 0 ? " { " : ", { ");
                    AppendPosition(document, corridor.TilePath[step]);
                    document.Append(" }");
                }

                document.Append(corridor.TilePath.Count == 0 ? "]" : " ]");
                document.Append(" }");
                CloseElement(document, index, corridors.Count);
            }

            CloseSection(document, corridors.Count, last: true);
        }

        static void OpenSection(StringBuilder document, string name, int count)
        {
            document.Append("  ").Append(Quoted(name)).Append(": [");
            if (count > 0)
            {
                document.Append('\n');
            }
        }

        static void CloseElement(StringBuilder document, int index, int count)
        {
            document.Append(index == count - 1 ? "\n" : ",\n");
        }

        static void CloseSection(StringBuilder document, int count, bool last)
        {
            if (count > 0)
            {
                document.Append("  ");
            }

            document.Append(last ? "]\n" : "],\n");
        }

        static void AppendPosition(StringBuilder document, TilePosition position)
        {
            document.Append("\"elevation\": ").Append(Number(position.Elevation));
            document.Append(", \"x\": ").Append(Number(position.X));
            document.Append(", \"y\": ").Append(Number(position.Y));
        }

        static string Number(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        static string Quoted(string value)
        {
            var quoted = new StringBuilder(value.Length + 2);
            quoted.Append('"');
            foreach (var symbol in value)
            {
                switch (symbol)
                {
                    case '"':
                        quoted.Append("\\\"");
                        break;
                    case '\\':
                        quoted.Append("\\\\");
                        break;
                    case '\b':
                        quoted.Append("\\b");
                        break;
                    case '\f':
                        quoted.Append("\\f");
                        break;
                    case '\n':
                        quoted.Append("\\n");
                        break;
                    case '\r':
                        quoted.Append("\\r");
                        break;
                    case '\t':
                        quoted.Append("\\t");
                        break;
                    default:
                        if (symbol < ' ')
                        {
                            quoted.Append("\\u").Append(((int)symbol).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            quoted.Append(symbol);
                        }

                        break;
                }
            }

            quoted.Append('"');
            return quoted.ToString();
        }
    }
}
