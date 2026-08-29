using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public abstract class Figure : MonoBehaviour
    {
        Transform badge;

        WorldPoint ground;

        PartModel worn;

        bool stood;

        protected float BaseScale { get; private set; }

        protected float Scale { get; private set; }

        protected PartModel Worn
        {
            get { return worn; }
        }

        protected float CapsuleUnit
        {
            get { return 1f / FigureFit.ScaleOf(worn); }
        }

        protected float TileHeight
        {
            get { return ground.Y; }
        }

        public WorldPoint Ground
        {
            get { return ground; }
        }

        internal void Stand(Transform hangingBadge, PartModel mesh)
        {
            badge = hangingBadge;
            worn = mesh;
            BaseScale = transform.localScale.x / FigureFit.ScaleOf(worn);
            Scale = BaseScale;

            var standing = transform.localPosition;
            ground = new WorldPoint(
                standing.x, standing.y - BaseScale * FigureFit.LiftOf(worn), standing.z);
            stood = true;
        }

        public void StandOn(WorldPoint tile)
        {
            ground = tile;
            Replant();
        }

        protected void Hide()
        {
            if (badge != null)
            {
                badge.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        protected void Wear(float scale)
        {
            Scale = scale;
            transform.localScale = Vector3.one * (scale * FigureFit.ScaleOf(worn));
            Replant();
        }

        void Replant()
        {
            if (!stood)
            {
                return;
            }

            transform.localPosition = new Vector3(
                ground.X, ground.Y + Scale * FigureFit.LiftOf(worn), ground.Z);

            if (badge != null)
            {
                badge.localPosition = new Vector3(
                    ground.X,
                    BadgeMetrics.AnchorAbove(ground.Y + FigureFit.StandingHeight(worn, Scale)),
                    ground.Z);
            }
        }
    }
}
