using System;

namespace Game.Presentation.Pure
{
    public static class Trophy
    {
        public const int Cap = 3;

        public const float Reach = 0.82f;

        public const float Shoulder = 0.15f;

        public const float Tilt = 20f;

        public const float Thickness = 0.16f;

        public const float Length = 1.20f;

        static readonly float[] yaws = { 202f, 270f, 338f };

        public static Tint Steel
        {
            get { return new Tint(0.80f, 0.82f, 0.88f); }
        }

        public static WorldPoint Size
        {
            get { return new WorldPoint(Thickness, Length, Thickness); }
        }

        public static WorldPoint PositionOf(int index)
        {
            RequireSlot(index);
            var radians = yaws[index] * (float)Math.PI / 180f;
            return new WorldPoint(
                Reach * (float)Math.Sin(radians),
                Shoulder,
                Reach * (float)Math.Cos(radians));
        }

        public static WorldPoint RotationOf(int index)
        {
            RequireSlot(index);
            return new WorldPoint(Tilt, yaws[index], 0f);
        }

        static void RequireSlot(int index)
        {
            if (index < 0 || index >= Cap)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, "A player carries at most three trophies.");
            }
        }
    }
}
