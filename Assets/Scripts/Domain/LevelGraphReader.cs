using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Domain
{
    public static class LevelGraphReader
    {
        public static LevelGraph Read(string document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var scanner = new JsonScanner(document);
            scanner.Expect('{');

            scanner.ExpectMember("version");
            var version = scanner.ReadInt();
            if (version != LevelGraphWriter.FormatVersion)
            {
                throw new FormatException(
                    "This reader speaks level graph format "
                    + LevelGraphWriter.FormatVersion.ToString(CultureInfo.InvariantCulture)
                    + ", but the document declares " + version.ToString(CultureInfo.InvariantCulture) + ".");
            }

            scanner.Expect(',');
            scanner.ExpectMember("seed");
            var seed = scanner.ReadLong();
            scanner.Expect(',');
            scanner.ExpectMember("preset");
            var preset = scanner.ReadText();
            scanner.Expect(',');

            var builder = new LevelGraphBuilder(seed, preset);

            ReadArray(scanner, "tiles", () =>
            {
                scanner.Expect('{');
                var position = ReadPosition(scanner);
                scanner.Expect(',');
                scanner.ExpectMember("region");
                var region = scanner.ReadInt();
                scanner.Expect('}');
                builder.AddTile(position, region);
            });
            scanner.Expect(',');

            var positionById = new List<TilePosition>();
            ReadArray(scanner, "nodes", () =>
            {
                scanner.Expect('{');
                scanner.ExpectMember("id");
                var id = scanner.ReadInt();
                if (id != positionById.Count)
                {
                    throw new FormatException(
                        "Node ids run from zero in sweep order, so id "
                        + id.ToString(CultureInfo.InvariantCulture) + " cannot sit at position "
                        + positionById.Count.ToString(CultureInfo.InvariantCulture) + ".");
                }

                scanner.Expect(',');
                var position = ReadPosition(scanner);
                scanner.Expect(',');
                scanner.ExpectMember("type");
                var type = NodeTypeNames.TypeNamed(scanner.ReadText());
                scanner.Expect(',');
                scanner.ExpectMember("value");
                var value = scanner.ReadInt();
                scanner.Expect('}');

                if (positionById.Count > 0 && positionById[positionById.Count - 1].CompareTo(position) >= 0)
                {
                    throw new FormatException(
                        "Nodes are listed in the sweep that assigns their ids, so " + position
                        + " cannot follow " + positionById[positionById.Count - 1] + ".");
                }

                positionById.Add(position);
                builder.AddNode(position, type, value);
            });
            scanner.Expect(',');

            ReadArray(scanner, "corridors", () =>
            {
                scanner.Expect('{');
                scanner.ExpectMember("low");
                var low = scanner.ReadInt();
                scanner.Expect(',');
                scanner.ExpectMember("high");
                var high = scanner.ReadInt();
                scanner.Expect(',');

                var path = new List<TilePosition>();
                ReadArray(scanner, "tiles", () =>
                {
                    scanner.Expect('{');
                    path.Add(ReadPosition(scanner));
                    scanner.Expect('}');
                });
                scanner.Expect('}');

                builder.Connect(PositionOf(positionById, low), PositionOf(positionById, high), path);
            });

            scanner.Expect('}');
            scanner.ExpectEnd();

            try
            {
                return builder.Build();
            }
            catch (Exception failure)
            {
                throw new FormatException("The document does not describe a level graph: " + failure.Message, failure);
            }
        }

        public static LevelGraph ReadBytes(byte[] document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return Read(new UTF8Encoding(false).GetString(document));
        }

        static TilePosition PositionOf(List<TilePosition> positionById, int id)
        {
            if (id < 0 || id >= positionById.Count)
            {
                throw new FormatException(
                    "A corridor ends at node " + id.ToString(CultureInfo.InvariantCulture)
                    + ", which the document never declares.");
            }

            return positionById[id];
        }

        static TilePosition ReadPosition(JsonScanner scanner)
        {
            scanner.ExpectMember("elevation");
            var elevation = scanner.ReadInt();
            scanner.Expect(',');
            scanner.ExpectMember("x");
            var x = scanner.ReadInt();
            scanner.Expect(',');
            scanner.ExpectMember("y");
            var y = scanner.ReadInt();
            return new TilePosition(elevation, x, y);
        }

        static void ReadArray(JsonScanner scanner, string name, Action readElement)
        {
            scanner.ExpectMember(name);
            scanner.Expect('[');
            if (scanner.TryExpect(']'))
            {
                return;
            }

            while (true)
            {
                readElement();
                if (scanner.TryExpect(','))
                {
                    continue;
                }

                scanner.Expect(']');
                return;
            }
        }
    }
}
