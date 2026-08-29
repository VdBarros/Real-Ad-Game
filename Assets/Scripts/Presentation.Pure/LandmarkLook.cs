using System;

namespace Game.Presentation.Pure
{
    public static class LandmarkLook
    {
        static readonly Tint[] Crowns =
        {
            new Tint(0.29f, 0.60f, 0.26f),
            new Tint(0.88f, 0.85f, 0.76f),
            new Tint(0.55f, 0.84f, 0.96f),
            new Tint(0.96f, 0.28f, 0.60f),
            new Tint(0.90f, 0.73f, 0.28f)
        };

        static readonly Tint[] Footings =
        {
            new Tint(0.37f, 0.26f, 0.17f),
            new Tint(0.46f, 0.44f, 0.42f),
            new Tint(0.52f, 0.55f, 0.59f),
            new Tint(0.27f, 0.22f, 0.33f),
            new Tint(0.31f, 0.30f, 0.37f)
        };

        public static Tint Of(LandmarkKind kind)
        {
            return Crowns[Slot(kind)];
        }

        public static Tint FootingOf(LandmarkKind kind)
        {
            return Footings[Slot(kind)];
        }

        static int Slot(LandmarkKind kind)
        {
            var slot = (int)kind;

            if (slot < 0 || slot >= Crowns.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "A landmark only stands in a colour its kind has been given.");
            }

            return slot;
        }
    }
}
