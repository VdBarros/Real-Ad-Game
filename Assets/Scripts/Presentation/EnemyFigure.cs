using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class EnemyFigure : Figure
    {
        static readonly Tint Ash = new Tint(0.88f, 0.88f, 0.90f);

        PowerBadge power;

        int value;

        public int NodeId { get; private set; }

        public EnemyBand Band { get; private set; }

        public bool HasFallen { get; private set; }

        internal void Begin(Transform badge, int nodeId, int enemyValue)
        {
            Stand(badge);
            NodeId = nodeId;
            value = enemyValue;
        }

        internal void Follow(PowerBadge playerPower)
        {
            power = playerPower;
            power.Changed += Reband;
            Reband(power.Power);
        }

        public void Doom()
        {
            Unhook();
        }

        public void Dissolve(float fade)
        {
            if (HasFallen)
            {
                return;
            }

            if (fade <= 0f)
            {
                Fall();
                return;
            }

            Wear(
                BaseScale * EnemyBands.ScaleOf(Band) * fade,
                Tint.Lerp(EnemyBands.TintOf(Band), Ash, 1f - fade));
        }

        void Fall()
        {
            if (HasFallen)
            {
                return;
            }

            HasFallen = true;
            Unhook();
            Hide();
        }

        void OnDestroy()
        {
            Unhook();
        }

        void Unhook()
        {
            if (power == null)
            {
                return;
            }

            power.Changed -= Reband;
            power = null;
        }

        void Reband(int playerPower)
        {
            if (HasFallen)
            {
                return;
            }

            Band = EnemyBands.Of(value, playerPower);
            Wear(BaseScale * EnemyBands.ScaleOf(Band), EnemyBands.TintOf(Band));
        }
    }
}
