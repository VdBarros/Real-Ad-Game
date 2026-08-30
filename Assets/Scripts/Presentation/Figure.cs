using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public abstract class Figure : MonoBehaviour
    {
        Transform badge;

        WorldPoint ground;

        WorldPoint restPose;

        PartModel worn;

        FigureTurn turning;

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

        public float RestYaw
        {
            get { return restPose.Y; }
        }

        public float Yaw
        {
            get { return turning.Yaw; }
        }

        public float FacingYaw
        {
            get { return turning.Wanted; }
        }

        public bool IsTurning
        {
            get { return !turning.IsSettled; }
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

            var angles = transform.localEulerAngles;
            restPose = new WorldPoint(angles.x, FigureFacing.Normalised(angles.y), angles.z);
            turning = FigureTurn.Facing(restPose.Y);
            stood = true;
        }

        public void StandOn(WorldPoint tile)
        {
            ground = tile;
            Replant();
        }

        public void Face(WorldPoint heading)
        {
            if (!stood || !FigureFacing.IsAimed(heading))
            {
                return;
            }

            turning = turning.Toward(FigureFacing.Composed(restPose.Y, heading));
        }

        public void Confront(Figure other)
        {
            if (other == null)
            {
                return;
            }

            Face(FigureFacing.Between(ground, other.Ground));
        }

        public void Turn(float deltaSeconds)
        {
            if (!stood)
            {
                return;
            }

            turning = turning.Advanced(deltaSeconds);
            transform.localEulerAngles = new Vector3(restPose.X, turning.Yaw, restPose.Z);
        }

        protected void Hide()
        {
            if (badge != null)
            {
                badge.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        protected void Refit(PartModel mesh)
        {
            if (!stood || worn == mesh)
            {
                return;
            }

            worn = mesh;
            Wear(Scale);
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
