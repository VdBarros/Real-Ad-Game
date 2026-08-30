using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class WeaponStow
    {
        public const float Tiles = 1f;

        public static float Ride
        {
            get { return FigureFit.StandingScalesOf(PlayerKit.Body) * 0.5f; }
        }

        public const float Back = 0.26f;

        public const float Tuck = 0.38f;

        public static bool Away(Journey journey)
        {
            if (journey == null || journey.State == null)
            {
                return false;
            }

            return Away(journey.State.Level, journey.Walk, journey.Fight);
        }

        public static bool Away(LevelGraph level, Walk walk, Fight fight)
        {
            if (level == null || fight.IsJoined)
            {
                return false;
            }

            return StepsToAGate(level, walk) <= Tiles;
        }

        public static float StepsToAGate(LevelGraph level, Walk walk)
        {
            if (level == null || walk.Route == null)
            {
                return float.MaxValue;
            }

            var route = walk.Route;
            var decisions = level.Decisions;
            var nearest = float.MaxValue;

            for (var step = 0; step < route.Nodes.Count; step++)
            {
                if (decisions.Node(route.Nodes[step]).Type != NodeType.Multiplier)
                {
                    continue;
                }

                var apart = Math.Abs(walk.Travelled - route.TileOf(step));

                if (apart < nearest)
                {
                    nearest = apart;
                }
            }

            return nearest;
        }

        public static float LocalYaw(float restYaw)
        {
            return FigureFacing.Normalised(FigureFacing.RestYaw - restYaw);
        }

        public static WorldPoint PoseOf(PlayerWeapon weapon, float restYaw)
        {
            return PoseOf(PlayerKit.GuiseHolding(weapon), weapon, restYaw);
        }

        public static WorldPoint PoseOf(PlayerGuise guise, PlayerWeapon weapon, float restYaw)
        {
            var behind = FigureFacing.HeadingOf(
                FigureFacing.Normalised(LocalYaw(restYaw) + FigureFacing.HalfTurn));

            return new WorldPoint(behind.X * Back, LiftOf(guise, weapon), behind.Z * Back);
        }

        public static WorldPoint TrophyOf(int slot)
        {
            var carried = Trophy.PositionOf(slot);
            var pulled = Tuck / Trophy.Reach;

            return new WorldPoint(carried.X * pulled, carried.Y, carried.Z * pulled);
        }

        public static float LiftOf(PlayerWeapon weapon)
        {
            return LiftOf(PlayerKit.GuiseHolding(weapon), weapon);
        }

        public static float LiftOf(PlayerGuise guise, PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return Ride;
            }

            var model = PlayerKit.ModelOf(weapon);
            var middle = ArtPacks.MountedBaseOf(model) + ArtPacks.MountedHeightOf(model) * 0.5f;

            return Ride - Standing(guise, middle);
        }

        public static float CrestOf(PlayerWeapon weapon)
        {
            return CrestOf(PlayerKit.GuiseHolding(weapon), weapon);
        }

        public static float CrestOf(PlayerGuise guise, PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            var model = PlayerKit.ModelOf(weapon);

            return LiftOf(guise, weapon)
                + Standing(guise, ArtPacks.MountedBaseOf(model) + ArtPacks.MountedHeightOf(model));
        }

        public static float FootOf(PlayerWeapon weapon)
        {
            return FootOf(PlayerKit.GuiseHolding(weapon), weapon);
        }

        public static float FootOf(PlayerGuise guise, PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            return LiftOf(guise, weapon)
                + Standing(guise, ArtPacks.MountedBaseOf(PlayerKit.ModelOf(weapon)));
        }

        public static float AcrossOf(PlayerWeapon weapon)
        {
            return AcrossOf(PlayerKit.GuiseHolding(weapon), weapon);
        }

        public static float AcrossOf(PlayerGuise guise, PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            return Standing(guise, ArtPacks.MountedWidthOf(PlayerKit.ModelOf(weapon)));
        }

        public static float BehindOf(PlayerWeapon weapon)
        {
            return BehindOf(PlayerKit.GuiseHolding(weapon), weapon);
        }

        public static float BehindOf(PlayerGuise guise, PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            return Back + Standing(guise, ArtPacks.MountedDepthOf(PlayerKit.ModelOf(weapon))) * 0.5f;
        }

        public static float Lintel
        {
            get { return FigureFit.StandingScalesOf(PlayerKit.Body) * GateArch.Headroom; }
        }

        public static float Shoulders
        {
            get { return FigureFit.WidthOf(GateArch.Passer, 1f); }
        }

        public static bool ClearsTheArch(PlayerWeapon weapon)
        {
            return ClearsTheArch(PlayerKit.GuiseHolding(weapon), weapon);
        }

        public static bool ClearsTheArch(PlayerGuise guise, PlayerWeapon weapon)
        {
            if (!TuckedIn())
            {
                return false;
            }

            if (weapon == PlayerWeapon.None)
            {
                return true;
            }

            return FootOf(guise, weapon) > 0f
                && CrestOf(guise, weapon) < Lintel
                && AcrossOf(guise, weapon) < Shoulders
                && 2f * BehindOf(guise, weapon) < FigureFit.DepthOf(GateArch.Passer, 1f);
        }

        static bool TuckedIn()
        {
            return Tuck < Trophy.Reach && 2f * (Tuck + Trophy.Thickness * 0.5f) < Shoulders;
        }

        static float Standing(PlayerGuise guise, float importUnits)
        {
            return importUnits * PlayerKit.StandingPerImportUnitOf(guise);
        }
    }
}
