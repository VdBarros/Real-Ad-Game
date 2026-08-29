using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct PlayerLook : IEquatable<PlayerLook>
    {
        public const float Growth = 1.15f;

        public const float BaseScale = LevelBlueprintBuilder.FigureScale;

        PlayerLook(int tier, float scale, int trophies, PlayerWeapon weapon, bool cloak)
        {
            Tier = tier;
            Scale = scale;
            Trophies = trophies;
            Weapon = weapon;
            Cloak = cloak;
        }

        public static PlayerLook Of(int power)
        {
            var tier = PlayerTier.Of(power);
            var scale = BaseScale;
            for (var step = 0; step < tier; step++)
            {
                scale *= Growth;
            }

            return new PlayerLook(
                tier, scale, TrophiesAt(tier), PlayerKit.WeaponOf(tier), PlayerKit.CloakedAt(tier));
        }

        static int TrophiesAt(int tier)
        {
            if (tier < 2)
            {
                return 0;
            }

            return tier - 1 < Trophy.Cap ? tier - 1 : Trophy.Cap;
        }

        public int Tier { get; }

        public float Scale { get; }

        public int Trophies { get; }

        public PlayerWeapon Weapon { get; }

        public bool Cloak { get; }

        public bool Equals(PlayerLook other)
        {
            return Tier == other.Tier
                && Scale.Equals(other.Scale)
                && Trophies == other.Trophies
                && Weapon == other.Weapon
                && Cloak == other.Cloak;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerLook other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Tier;
                hash = (hash * 397) ^ Scale.GetHashCode();
                hash = (hash * 397) ^ Trophies;
                hash = (hash * 397) ^ (int)Weapon;
                hash = (hash * 397) ^ (Cloak ? 1 : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(
                "tier ",
                Tier.ToString(),
                " at ",
                Scale.ToString("0.###", CultureInfo.InvariantCulture),
                " carrying ",
                Trophies.ToString(),
                ", wielding ",
                Weapon.ToString(),
                Cloak ? " under a cloak" : " bare shouldered");
        }
    }
}
