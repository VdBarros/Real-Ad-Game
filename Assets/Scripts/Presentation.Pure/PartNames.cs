using System.Globalization;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class PartNames
    {
        public const string Root = "LevelRoot";

        public const string TilesGroup = "Tiles";

        public const string NodesGroup = "Nodes";

        public const string BadgesGroup = "Badges";

        public const string Weapon = "Weapon";

        public const string Rig = "CameraRig";

        public const string TrailGroup = "Trail";

        public const string Spark = "Spark";

        public static string Terrace(int elevation)
        {
            return "Terrace_" + elevation.ToString(CultureInfo.InvariantCulture);
        }

        public static string Tile(TilePosition position)
        {
            return "Tile" + Key(position);
        }

        public static string Wall(TilePosition position, TileSide side)
        {
            return "Wall" + Key(position) + "_" + side;
        }

        public static string Stair(TilePosition position)
        {
            return "Stair" + Key(position);
        }

        public static string Node(int nodeId)
        {
            return "Node_" + nodeId.ToString(CultureInfo.InvariantCulture);
        }

        public static string Badge(int nodeId)
        {
            return "Badge_" + nodeId.ToString(CultureInfo.InvariantCulture);
        }

        public static string Dot(int index)
        {
            return "Dot_" + index.ToString(CultureInfo.InvariantCulture);
        }

        public static string Trophy(int slot)
        {
            return "Trophy_" + slot.ToString(CultureInfo.InvariantCulture);
        }

        static string Key(TilePosition position)
        {
            return string.Concat(
                "_e",
                position.Elevation.ToString(CultureInfo.InvariantCulture),
                "_x",
                position.X.ToString(CultureInfo.InvariantCulture),
                "_y",
                position.Y.ToString(CultureInfo.InvariantCulture));
        }
    }
}
