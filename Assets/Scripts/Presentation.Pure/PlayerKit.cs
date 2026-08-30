using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class PlayerKit
    {
        public const int CloakFrom = 1;

        public const float GripHeight = 0.63728f;

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

        static readonly PlayerWeapon[] weaponByTier =
        {
            PlayerWeapon.None,
            PlayerWeapon.Shortsword,
            PlayerWeapon.Axe,
            PlayerWeapon.Spear,
            PlayerWeapon.Greatsword
        };

        public static PlayerWeapon WeaponOf(int tier)
        {
            RequireTier(tier);

            return tier < weaponByTier.Length ? weaponByTier[tier] : weaponByTier[weaponByTier.Length - 1];
        }

        public static bool CloakedAt(int tier)
        {
            RequireTier(tier);

            return tier >= CloakFrom;
        }

        public static IReadOnlyList<PlayerWeapon> Weapons
        {
            get { return weaponByTier; }
        }

        public static PartModel ModelOf(PlayerWeapon weapon)
        {
            return Held(weapon).Model;
        }

        public static string CloakNode
        {
            get { return AdventurerPack.CloakNode; }
        }

        public static PartModel Body
        {
            get { return CharacterCast.MeshOf(PartStyle.Start); }
        }

        public static float StandingPerImportUnit
        {
            get { return FigureFit.ScaleOf(Body); }
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
            if (weapon == PlayerWeapon.None)
            {
                return 0f;
            }

            var model = Held(weapon).Model;
            var width = ArtPacks.WidthOf(model);
            var height = ArtPacks.HeightOf(model);
            var depth = ArtPacks.DepthOf(model);

            return StandingPerImportUnit
                * (float)Math.Sqrt(width * width + height * height + depth * depth);
        }

        static Wielded Held(PlayerWeapon weapon)
        {
            switch (weapon)
            {
                case PlayerWeapon.Shortsword:
                    return new Wielded(PartModel.SwordA, 0.67635f, 1.02282f);
                case PlayerWeapon.Axe:
                    return new Wielded(PartModel.AxeB, 0.71331f, 1.20882f);
                case PlayerWeapon.Spear:
                    return new Wielded(PartModel.StaffA, 0.67315f, 0.97671f);
                case PlayerWeapon.Greatsword:
                    return new Wielded(PartModel.Sword2Handed, 0.7126f, 1.37637f);
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
