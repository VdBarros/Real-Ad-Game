using System.Globalization;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class PartNames
    {
        public const string Root = "LevelRoot";

        public const string TilesGroup = "Tiles";

        public const string NodesGroup = "Nodes";

        public static string Floor(int floor)
        {
            return "Floor_" + floor.ToString(CultureInfo.InvariantCulture);
        }

        public static string Tile(TilePosition position)
        {
            return "Tile" + Key(position);
        }

        public static string Wall(TilePosition position, TileSide side)
        {
            return "Wall" + Key(position) + "_" + side;
        }

        public static string Ramp(TilePosition position)
        {
            return "Ramp" + Key(position);
        }

        public static string Node(int nodeId)
        {
            return "Node_" + nodeId.ToString(CultureInfo.InvariantCulture);
        }

        static string Key(TilePosition position)
        {
            return string.Concat(
                "_f",
                position.Floor.ToString(CultureInfo.InvariantCulture),
                "_x",
                position.X.ToString(CultureInfo.InvariantCulture),
                "_y",
                position.Y.ToString(CultureInfo.InvariantCulture));
        }
    }
}
