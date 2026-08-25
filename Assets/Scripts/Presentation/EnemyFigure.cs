using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class EnemyFigure : Figure
    {
        PowerBadge power;

        int value;

        public EnemyBand Band { get; private set; }

        internal void Begin(Transform badge, int enemyValue)
        {
            Stand(badge);
            value = enemyValue;
        }

        internal void Follow(PowerBadge playerPower)
        {
            power = playerPower;
            power.Changed += Reband;
            Reband(power.Power);
        }

        void OnDestroy()
        {
            if (power != null)
            {
                power.Changed -= Reband;
            }
        }

        void Reband(int playerPower)
        {
            Band = EnemyBands.Of(value, playerPower);
            Wear(BaseScale * EnemyBands.ScaleOf(Band), EnemyBands.TintOf(Band));
        }
    }
}
