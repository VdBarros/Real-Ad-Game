namespace Game.Presentation.Pure
{
    public static class CameraJolt
    {
        public const float Drop = 0.42f;

        public const float Yield = 0.18f;

        public static float Clamped(float impulse)
        {
            return impulse < 0f ? 0f : impulse > 1f ? 1f : impulse;
        }

        public static WorldPoint Offset(float impulse)
        {
            var kick = Clamped(impulse);

            return new WorldPoint(Yield * kick, -Drop * kick, Yield * kick);
        }

        public static WorldPoint Jolted(WorldPoint framing, float impulse)
        {
            var offset = Offset(impulse);

            return new WorldPoint(
                framing.X + offset.X, framing.Y + offset.Y, framing.Z + offset.Z);
        }
    }
}
