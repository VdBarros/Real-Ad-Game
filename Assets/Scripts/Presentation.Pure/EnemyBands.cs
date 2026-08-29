using System;

namespace Game.Presentation.Pure
{
    public static class EnemyBands
    {
        public const int TrivialShare = 2;

        public const int CloseReach = 2;

        static readonly float[] scales = { 0.85f, 0.93f, 1.01f, 1.09f };

        public static EnemyBand Of(int enemyValue, int playerPower)
        {
            if (enemyValue < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyValue), enemyValue, "An enemy always holds power.");
            }

            if (playerPower < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerPower), playerPower, "A run always holds power.");
            }

            long value = enemyValue;
            long power = playerPower;

            if (value * TrivialShare <= power)
            {
                return EnemyBand.Trivial;
            }

            if (value < power)
            {
                return EnemyBand.Edible;
            }

            return value <= power * CloseReach ? EnemyBand.Close : EnemyBand.OutOfReach;
        }

        public static bool IsBeatable(EnemyBand band)
        {
            return band == EnemyBand.Trivial || band == EnemyBand.Edible;
        }

        public static float ScaleOf(EnemyBand band)
        {
            return scales[Slot(band)];
        }

        static int Slot(EnemyBand band)
        {
            var slot = (int)band;
            if (slot < 0 || slot >= scales.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(band), band, "No look for that band.");
            }

            return slot;
        }
    }
}
