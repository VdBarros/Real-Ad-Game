using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class PlayerKit
    {
        public const int CloakFrom = 1;

        public const float GripHeight = 1.08f;

        static readonly PlayerWeapon[] weaponByTier =
        {
            PlayerWeapon.None,
            PlayerWeapon.Shortsword,
            PlayerWeapon.Axe,
            PlayerWeapon.Spear,
            PlayerWeapon.Greatsword
        };

        static readonly PropLimb[] none = new PropLimb[0];

        static readonly PropLimb[] shortsword =
        {
            new PropLimb(
                new WorldPoint(0.11f, 0.72f, 0.11f),
                new WorldPoint(0f, 0.40f, 0f),
                new WorldPoint(0f, 0f, 0f)),
            new PropLimb(
                new WorldPoint(0.38f, 0.09f, 0.13f),
                new WorldPoint(0f, 0.05f, 0f),
                new WorldPoint(0f, 0f, 0f))
        };

        static readonly PropLimb[] axe =
        {
            new PropLimb(
                new WorldPoint(0.12f, 1.28f, 0.12f),
                new WorldPoint(0f, 0.62f, 0f),
                new WorldPoint(0f, 0f, 0f)),
            new PropLimb(
                new WorldPoint(0.52f, 0.46f, 0.16f),
                new WorldPoint(0.24f, 1.10f, 0f),
                new WorldPoint(0f, 0f, 0f))
        };

        static readonly PropLimb[] spear =
        {
            new PropLimb(
                new WorldPoint(0.09f, 1.94f, 0.09f),
                new WorldPoint(0f, 0.90f, 0f),
                new WorldPoint(0f, 0f, 0f)),
            new PropLimb(
                new WorldPoint(0.20f, 0.34f, 0.20f),
                new WorldPoint(0f, 1.90f, 0f),
                new WorldPoint(0f, 45f, 0f))
        };

        static readonly PropLimb[] greatsword =
        {
            new PropLimb(
                new WorldPoint(0.26f, 2.50f, 0.11f),
                new WorldPoint(0f, 1.40f, 0f),
                new WorldPoint(0f, 0f, 0f)),
            new PropLimb(
                new WorldPoint(0.86f, 0.15f, 0.17f),
                new WorldPoint(0f, 0.12f, 0f),
                new WorldPoint(0f, 0f, 0f))
        };

        static readonly PropLimb[] cloak =
        {
            new PropLimb(
                new WorldPoint(0.86f, 1.12f, 0.16f),
                new WorldPoint(0f, 0.62f, -0.34f),
                new WorldPoint(-8f, 0f, 0f)),
            new PropLimb(
                new WorldPoint(1.02f, 0.20f, 0.20f),
                new WorldPoint(0f, 1.16f, -0.22f),
                new WorldPoint(0f, 0f, 0f))
        };

        public static Tint Steel
        {
            get { return Trophy.Steel; }
        }

        public static Tint Cloth
        {
            get { return new Tint(0.42f, 0.16f, 0.19f); }
        }

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

        public static IReadOnlyList<PropLimb> LimbsOf(PlayerWeapon weapon)
        {
            switch (weapon)
            {
                case PlayerWeapon.None:
                    return none;
                case PlayerWeapon.Shortsword:
                    return shortsword;
                case PlayerWeapon.Axe:
                    return axe;
                case PlayerWeapon.Spear:
                    return spear;
                case PlayerWeapon.Greatsword:
                    return greatsword;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(weapon), weapon, "The kit carries no shape for that weapon.");
            }
        }

        public static IReadOnlyList<PropLimb> CloakLimbs
        {
            get { return cloak; }
        }

        public static float TipOf(PlayerWeapon weapon)
        {
            var limbs = LimbsOf(weapon);
            var top = 0f;

            for (var limb = 0; limb < limbs.Count; limb++)
            {
                if (limbs[limb].Top > top)
                {
                    top = limbs[limb].Top;
                }
            }

            return top == 0f ? 0f : GripHeight + top;
        }

        public static float BreadthOf(PlayerWeapon weapon)
        {
            var limbs = LimbsOf(weapon);
            var widest = 0f;

            for (var limb = 0; limb < limbs.Count; limb++)
            {
                if (limbs[limb].Reach > widest)
                {
                    widest = limbs[limb].Reach;
                }
            }

            return widest;
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
