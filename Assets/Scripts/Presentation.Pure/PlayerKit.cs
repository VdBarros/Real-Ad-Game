using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class PlayerKit
    {
        public const int CloakFrom = 1;

        public const float KnightGripHeight = 0.63728f;

        public const float BarbarianGripHeight = 0.65008f;

        public const float RogueGripHeight = 0.70786f;

        public const float MageGripHeight = 0.57883f;

        readonly struct Rung
        {
            public Rung(PlayerGuise guise, PlayerWeapon weapon)
            {
                Guise = guise;
                Weapon = weapon;
            }

            public PlayerGuise Guise { get; }

            public PlayerWeapon Weapon { get; }
        }

        readonly struct Wielded
        {
            public Wielded(PartModel model, float tip, float breadth)
            {
                Model = model;
                Tip = tip;
                Breadth = breadth;
            }

            public PartModel Model { get; }

            public float Tip { get; }

            public float Breadth { get; }
        }

        static readonly Rung[] rampByTier =
        {
            new Rung(PlayerGuise.Knight, PlayerWeapon.None),
            new Rung(PlayerGuise.Knight, PlayerWeapon.Shortsword),
            new Rung(PlayerGuise.Barbarian, PlayerWeapon.Axe),
            new Rung(PlayerGuise.Rogue, PlayerWeapon.Bow),
            new Rung(PlayerGuise.Mage, PlayerWeapon.Staff)
        };

        public static PlayerWeapon WeaponOf(int tier)
        {
            return Climbed(tier).Weapon;
        }

        public static PlayerGuise GuiseOf(int tier)
        {
            return Climbed(tier).Guise;
        }

        public static PlayerGuise GuiseHolding(PlayerWeapon weapon)
        {
            for (var tier = 0; tier < rampByTier.Length; tier++)
            {
                if (rampByTier[tier].Weapon == weapon)
                {
                    return rampByTier[tier].Guise;
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(weapon), weapon, "No rung of the ramp hands out that weapon.");
        }

        public static bool CloakedAt(int tier)
        {
            var rung = Climbed(tier);

            return tier >= CloakFrom && PlayerGuises.Drapes(rung.Guise);
        }

        public static string CapeOf(int tier)
        {
            return PlayerGuises.CapeOf(GuiseOf(tier));
        }

        public static IReadOnlyList<PlayerWeapon> Weapons
        {
            get
            {
                var carried = new PlayerWeapon[rampByTier.Length];

                for (var tier = 0; tier < rampByTier.Length; tier++)
                {
                    carried[tier] = rampByTier[tier].Weapon;
                }

                return carried;
            }
        }

        public static IReadOnlyList<PlayerGuise> Guises
        {
            get
            {
                var worn = new PlayerGuise[rampByTier.Length];

                for (var tier = 0; tier < rampByTier.Length; tier++)
                {
                    worn[tier] = rampByTier[tier].Guise;
                }

                return worn;
            }
        }

        public static PartModel ModelOf(PlayerWeapon weapon)
        {
            return Held(weapon).Model;
        }

        public static PartModel Body
        {
            get { return BodyOf(GuiseOf(0)); }
        }

        public static PartModel BodyOf(PlayerGuise guise)
        {
            return PlayerGuises.MeshOf(guise);
        }

        public static float StandingPerImportUnit
        {
            get { return StandingPerImportUnitOf(GuiseOf(0)); }
        }

        public static float StandingPerImportUnitOf(PlayerGuise guise)
        {
            return FigureFit.ScaleOf(BodyOf(guise));
        }

        public static float GripHeightOf(PlayerGuise guise)
        {
            switch (guise)
            {
                case PlayerGuise.Knight:
                    return KnightGripHeight;
                case PlayerGuise.Barbarian:
                    return BarbarianGripHeight;
                case PlayerGuise.Rogue:
                    return RogueGripHeight;
                case PlayerGuise.Mage:
                    return MageGripHeight;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(guise), guise, "No guise grips its weapon at that height.");
            }
        }

        public static float TipOf(PlayerWeapon weapon)
        {
            return weapon == PlayerWeapon.None ? 0f : Held(weapon).Tip;
        }

        public static float BreadthOf(PlayerWeapon weapon)
        {
            return weapon == PlayerWeapon.None ? 0f : Held(weapon).Breadth;
        }

        public static float ReachOf(PlayerWeapon weapon)
        {
            return weapon == PlayerWeapon.None ? 0f : ReachOf(GuiseHolding(weapon), weapon);
        }

        public static float ReachOf(PlayerGuise guise, PlayerWeapon weapon)
        {
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            var model = Held(weapon).Model;
            var width = ArtPacks.WidthOf(model);
            var height = ArtPacks.HeightOf(model);
            var depth = ArtPacks.DepthOf(model);

            return StandingPerImportUnitOf(guise)
                * (float)Math.Sqrt(width * width + height * height + depth * depth);
        }

        static Rung Climbed(int tier)
        {
            RequireTier(tier);

            return tier < rampByTier.Length ? rampByTier[tier] : rampByTier[rampByTier.Length - 1];
        }

        static Wielded Held(PlayerWeapon weapon)
        {
            switch (weapon)
            {
                case PlayerWeapon.Shortsword:
                    return new Wielded(PartModel.SwordA, 0.67635f, 1.02282f);
                case PlayerWeapon.Axe:
                    return new Wielded(PartModel.AxeB, 0.72764f, 1.23311f);
                case PlayerWeapon.Bow:
                    return new Wielded(PartModel.BowA, 0.73877f, 1.12256f);
                case PlayerWeapon.Staff:
                    return new Wielded(PartModel.StaffB, 0.6542f, 1.14406f);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(weapon), weapon, "The kit hangs no mesh on an empty hand.");
            }
        }

        static void RequireTier(int tier)
        {
            if (tier < 0 || tier >= PlayerTier.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tier), tier, "The player climbs no tier outside the tier table.");
            }
        }
    }
}
