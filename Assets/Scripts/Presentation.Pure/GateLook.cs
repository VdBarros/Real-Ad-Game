using System;

namespace Game.Presentation.Pure
{
    public static class GateLook
    {
        public const float WashShare = 0.5f;

        static readonly Tint[] ByFactor =
        {
            new Tint(0.28f, 0.88f, 0.96f),
            new Tint(0.62f, 0.45f, 0.99f),
            new Tint(0.99f, 0.58f, 0.20f),
            new Tint(0.33f, 0.95f, 0.52f)
        };

        public static Tint Of(int factor)
        {
            if (factor < GateArch.SmallestFactor || factor > GateArch.MostPips)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(factor), factor, "A gate only glows in a colour its factor has been given.");
            }

            return ByFactor[factor - GateArch.SmallestFactor];
        }

        public static Tint Washed(int factor, MarkLook look)
        {
            return Washed(Of(factor), look);
        }

        public static Tint Washed(Tint plain, MarkLook look)
        {
            return Tint.Lerp(plain, look.Tint, look.Weight * WashShare);
        }
    }
}
