using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct PlayerLook : IEquatable<PlayerLook>
    {
        public const float Growth = 1.15f;

        public const float BaseScale = LevelBlueprintBuilder.FigureScale;

        static readonly Tint[] ramp =
        {
            new Tint(0.30f, 0.72f, 0.78f),
            new Tint(0.36f, 0.80f, 0.52f),
            new Tint(0.82f, 0.86f, 0.36f),
            new Tint(0.95f, 0.68f, 0.24f),
            new Tint(1.00f, 0.44f, 0.16f)
        };

        PlayerLook(int tier, float scale, Tint tint, int trophies)
        {
            Tier = tier;
            Scale = scale;
            Tint = tint;
            Trophies = trophies;
        }

        public static PlayerLook Of(int power)
        {
            var tier = VisualTier.Of(power);
            var scale = BaseScale;
            for (var step = 0; step < tier; step++)
            {
                scale *= Growth;
            }

            return new PlayerLook(tier, scale, ramp[tier], TrophiesAt(tier));
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

        public Tint Tint { get; }

        public int Trophies { get; }

        public bool Equals(PlayerLook other)
        {
            return Tier == other.Tier
                && Scale.Equals(other.Scale)
                && Tint.Equals(other.Tint)
                && Trophies == other.Trophies;
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
                hash = (hash * 397) ^ Tint.GetHashCode();
                hash = (hash * 397) ^ Trophies;
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
                " ",
                Tint.ToString(),
                " carrying ",
                Trophies.ToString());
        }
    }
}
