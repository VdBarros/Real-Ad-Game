using System;

namespace Game.Presentation.Pure
{
    public static class CastLooks
    {
        public const float PeasantScale = 0.5f;

        public const float SkeletonScale = 0.42f;

        public const float QueenScale = 0.62f;

        public const float ChampionScale = 0.6f;

        public static Tint TintOf(CastLook look)
        {
            switch (look)
            {
                case CastLook.Peasant:
                    return new Tint(0.72f, 0.58f, 0.40f);
                case CastLook.Skeleton:
                    return new Tint(0.88f, 0.90f, 0.86f);
                case CastLook.Queen:
                    return new Tint(0.96f, 0.80f, 0.28f);
                case CastLook.Champion:
                    return new Tint(0.62f, 0.66f, 0.74f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(look), look, "No tint for that look.");
            }
        }

        public static float ScaleOf(CastLook look)
        {
            switch (look)
            {
                case CastLook.Peasant:
                    return PeasantScale;
                case CastLook.Skeleton:
                    return SkeletonScale;
                case CastLook.Queen:
                    return QueenScale;
                case CastLook.Champion:
                    return ChampionScale;
                default:
                    throw new ArgumentOutOfRangeException(nameof(look), look, "No scale for that look.");
            }
        }
    }
}
