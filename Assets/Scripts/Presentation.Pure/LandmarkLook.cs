using System;

namespace Game.Presentation.Pure
{
    public static class LandmarkLook
    {
        static readonly Tint[] Washes =
        {
            new Tint(0.74f, 0.76f, 0.80f),
            new Tint(0.96f, 0.49f, 0.20f),
            new Tint(0.87f, 0.70f, 0.26f),
            new Tint(0.42f, 0.58f, 0.74f),
            new Tint(0.45f, 0.73f, 0.45f)
        };

        public static Tint Of(LandmarkKind kind)
        {
            return Washes[Slot(kind)];
        }

        static int Slot(LandmarkKind kind)
        {
            var slot = (int)kind;

            if (slot < 0 || slot >= Washes.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "A landmark only stands in a colour its kind has been given.");
            }

            return slot;
        }
    }
}
