using System;

namespace Game.Presentation.Pure
{
    public static class FigureFacing
    {
        public const float FullTurn = 360f;

        public const float HalfTurn = 180f;

        const float Length = 1e-5f;

        const double Degrees = 180.0 / Math.PI;

        static readonly WorldPoint RestHeading = Flattened(
            Reversed(IsoProjection.CameraForward));

        public static WorldPoint Rest
        {
            get { return RestHeading; }
        }

        public static float RestYaw
        {
            get { return YawOf(RestHeading); }
        }

        public static bool IsAimed(WorldPoint heading)
        {
            return heading.X * heading.X + heading.Z * heading.Z > Length;
        }

        public static WorldPoint Reversed(WorldPoint heading)
        {
            return new WorldPoint(-heading.X, -heading.Y, -heading.Z);
        }

        public static WorldPoint Between(WorldPoint from, WorldPoint to)
        {
            return Flattened(new WorldPoint(to.X - from.X, 0f, to.Z - from.Z));
        }

        public static float YawOf(WorldPoint heading)
        {
            if (!IsAimed(heading))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heading), heading, "A heading of no length points nowhere to face.");
            }

            return Normalised((float)(Math.Atan2(heading.X, heading.Z) * Degrees));
        }

        public static float Swing(WorldPoint heading)
        {
            return Normalised(YawOf(heading) - RestYaw);
        }

        public static float Composed(float restYaw, WorldPoint heading)
        {
            return Normalised(restYaw + Swing(heading));
        }

        public static float Of(PartModel model, WorldPoint heading)
        {
            return Composed(ArtPacks.FacingOf(model), heading);
        }

        public static float Normalised(float degrees)
        {
            var wrapped = degrees % FullTurn;

            return wrapped < 0f ? wrapped + FullTurn : wrapped;
        }

        public static float Shortest(float from, float to)
        {
            var swing = (to - from) % FullTurn;

            if (swing > HalfTurn)
            {
                swing -= FullTurn;
            }

            if (swing <= -HalfTurn)
            {
                swing += FullTurn;
            }

            return swing;
        }

        public static WorldPoint HeadingOf(float yaw)
        {
            var turn = yaw / Degrees;

            return new WorldPoint((float)Math.Sin(turn), 0f, (float)Math.Cos(turn));
        }

        static WorldPoint Flattened(WorldPoint heading)
        {
            var length = (float)Math.Sqrt(heading.X * heading.X + heading.Z * heading.Z);

            return length <= 0f
                ? default(WorldPoint)
                : new WorldPoint(heading.X / length, 0f, heading.Z / length);
        }
    }
}
