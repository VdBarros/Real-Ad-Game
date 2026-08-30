using System;
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

        public const string Backdrop = "Backdrop";

        public const string WorldPrefix = "World_";

        public const string BackdropSkin = Backdrop + "_Skin";

        public static bool IsWorldPrefixed(string name)
        {
            return name != null && name.StartsWith(WorldPrefix, StringComparison.Ordinal);
        }

        public const string GhostPrefix = "Ghost_";

        public const string FadingPrefix = "Fading_";

        public static string Ghosted(string worn)
        {
            return GhostPrefix + worn;
        }

        public static string Fading(string solid)
        {
            return FadingPrefix + solid;
        }

        public const string TrailGroup = "Trail";

        public const string OrbGroup = "Orbs";

        public const string OrbPrefix = "Orb_";

        public const string OrbBurst = "OrbBurst";

        public static string Orb(int index)
        {
            return OrbPrefix + index.ToString(CultureInfo.InvariantCulture);
        }

        public static bool IsOrb(string name)
        {
            return name != null
                && (name.StartsWith(OrbPrefix, StringComparison.Ordinal)
                    || string.Equals(name, OrbBurst, StringComparison.Ordinal));
        }

        public const string GateLeftPost = "GatePost_Left";

        public const string GateRightPost = "GatePost_Right";

        public const string GateLintel = "GateLintel";

        public const string GatePipPrefix = "GatePip_";

        public const string LandmarksGroup = "Landmarks";

        public const string LandmarkPrefix = "Landmark";

        public const string LandmarkPiecePrefix = "LandmarkPiece_";

        public static string LandmarkPiece(int index)
        {
            return LandmarkPiecePrefix + index.ToString(CultureInfo.InvariantCulture);
        }

        public static bool IsLandmarkPiece(string name)
        {
            return name != null && name.StartsWith(LandmarkPiecePrefix, StringComparison.Ordinal);
        }

        public static string Landmark(TilePosition position)
        {
            return LandmarkPrefix + Key(position);
        }

        public static string GatePip(int index)
        {
            return GatePipPrefix + index.ToString(CultureInfo.InvariantCulture);
        }

        public static bool IsGatePip(string name)
        {
            return name != null && name.StartsWith(GatePipPrefix, StringComparison.Ordinal);
        }

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

        public static string Footing(TilePosition position)
        {
            return "Footing" + Key(position);
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

        public const string GuisePrefix = "Guise_";

        public static string Guised(PlayerGuise guise)
        {
            return GuisePrefix + guise;
        }

        public static bool IsGuised(string name)
        {
            return name != null && name.StartsWith(GuisePrefix, StringComparison.Ordinal);
        }

        public const string WornPrefix = "Worn_";

        public const string TrophyPrefix = WornPrefix + "Trophy_";

        public static string Trophy(int slot)
        {
            return TrophyPrefix + slot.ToString(CultureInfo.InvariantCulture);
        }

        public static bool IsTrophy(string name)
        {
            return name != null && name.StartsWith(TrophyPrefix, StringComparison.Ordinal);
        }

        public static string Held(PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weapon), weapon, "An empty hand needs no name because it holds nothing.");
            }

            return WornPrefix + weapon;
        }

        public static bool IsWorn(string name)
        {
            return name != null && name.StartsWith(WornPrefix, StringComparison.Ordinal);
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
