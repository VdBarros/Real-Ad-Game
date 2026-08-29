using System;

namespace Game.Presentation.Pure
{
    public static class GateWorth
    {
        public const float Negligible = 0.005f;

        public static float ShareOf(int gain, int power)
        {
            if (gain < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gain), gain, "A gate hands power out, never takes it away.");
            }

            if (power < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(power), power, "A run always holds power, so a gain is always a share of something.");
            }

            return gain / (float)power;
        }

        public static bool IsNegligible(int gain, int power)
        {
            return gain >= 0 && power >= 1 && ShareOf(gain, power) < Negligible;
        }
    }
}
