using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [ExecuteAlways]
    public sealed class EnemyFigure : Figure
    {
        PowerBadge power;

        FigureAnimator acting;

        Material[] ghosts;

        int value;

        public int NodeId { get; private set; }

        public EnemyBand Band { get; private set; }

        public bool HasFallen { get; private set; }

        public bool IsGhost
        {
            get { return ghosts != null; }
        }

        public float GhostAlpha
        {
            get { return ghosts == null ? 1f : Ghosting.AlphaOf(ghosts); }
        }

        public FigureAct Act
        {
            get { return acting == null ? FigureAct.Idle : acting.Act; }
        }

        internal void Begin(Transform badge, PartModel mesh, int nodeId, int enemyValue)
        {
            Stand(badge, mesh);
            acting = GetComponent<FigureAnimator>();
            NodeId = nodeId;
            value = enemyValue;
        }

        internal void Answer(FigureCue cue)
        {
            if (acting != null)
            {
                acting.Cue(cue);
            }
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

            if (fade >= 1f)
            {
                return;
            }

            Haunt(fade);
        }

        void Haunt(float fade)
        {
            if (ghosts == null)
            {
                ghosts = Ghosting.Raise(gameObject);
            }

            Ghosting.Fade(ghosts, fade);
        }

        void Fall()
        {
            if (HasFallen)
            {
                return;
            }

            HasFallen = true;
            Unhook();
            Bury();
            Hide();
        }

        void Bury()
        {
            if (ghosts == null)
            {
                return;
            }

            Ghosting.Lay(ghosts);
            ghosts = null;
        }

        void OnDestroy()
        {
            Unhook();
            Bury();
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
            Wear(BaseScale * EnemyBands.ScaleOf(Band));
        }
    }
}
