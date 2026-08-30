using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class WeaponStow
    {
        public const float Tiles = 1f;

        public const float Ride = AdventurerPack.StandingScales * 0.5f;

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
            var behind = FigureFacing.HeadingOf(
                FigureFacing.Normalised(LocalYaw(restYaw) + FigureFacing.HalfTurn));

            return new WorldPoint(behind.X * Back, LiftOf(weapon), behind.Z * Back);
        }

        public static WorldPoint TrophyOf(int slot)
        {
            var carried = Trophy.PositionOf(slot);
            var pulled = Tuck / Trophy.Reach;

            return new WorldPoint(carried.X * pulled, carried.Y, carried.Z * pulled);
        }

        public static float LiftOf(PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return Ride;
            }

            var model = PlayerKit.ModelOf(weapon);
            var middle = AdventurerPack.PackBaseOf(model) + AdventurerPack.PackHeightOf(model) * 0.5f;

            return Ride - Standing(middle);
        }

        public static float CrestOf(PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            var model = PlayerKit.ModelOf(weapon);

            return LiftOf(weapon)
                + Standing(AdventurerPack.PackBaseOf(model) + AdventurerPack.PackHeightOf(model));
        }

        public static float FootOf(PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            return LiftOf(weapon) + Standing(AdventurerPack.PackBaseOf(PlayerKit.ModelOf(weapon)));
        }

        public static float AcrossOf(PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            return Standing(AdventurerPack.PackWidthOf(PlayerKit.ModelOf(weapon)));
        }

        public static float BehindOf(PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            return Back + Standing(AdventurerPack.PackDepthOf(PlayerKit.ModelOf(weapon))) * 0.5f;
        }

        public static float Lintel
        {
            get { return AdventurerPack.StandingScales * GateArch.Headroom; }
        }

        public static float Shoulders
        {
            get { return FigureFit.WidthOf(GateArch.Passer, 1f); }
        }

        public static bool ClearsTheArch(PlayerWeapon weapon)
        {
            if (!TuckedIn())
            {
                return false;
            }

            if (weapon == PlayerWeapon.None)
            {
                return true;
            }

            return FootOf(weapon) > 0f
                && CrestOf(weapon) < Lintel
                && AcrossOf(weapon) < Shoulders
                && 2f * BehindOf(weapon) < FigureFit.DepthOf(GateArch.Passer, 1f);
        }

        static bool TuckedIn()
        {
            return Tuck < Trophy.Reach && 2f * (Tuck + Trophy.Thickness * 0.5f) < Shoulders;
        }

        static float Standing(float packUnits)
        {
            return packUnits * AdventurerPack.StandingPerPackUnit;
        }
    }
}
