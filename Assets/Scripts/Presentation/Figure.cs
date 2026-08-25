using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public abstract class Figure : MonoBehaviour
    {
        Transform badge;

        Renderer skin;

        WorldPoint ground;

        bool stood;

        protected float BaseScale { get; private set; }

        protected float TileHeight
        {
            get { return ground.Y; }
        }

        public WorldPoint Ground
        {
            get { return ground; }
        }

        internal void Stand(Transform hangingBadge)
        {
            badge = hangingBadge;
            skin = GetComponent<Renderer>();
            BaseScale = transform.localScale.x;

            var standing = transform.localPosition;
            ground = new WorldPoint(standing.x, standing.y - BaseScale, standing.z);
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

        protected void Wear(float scale, Tint tint)
        {
            transform.localScale = new Vector3(scale, scale, scale);
            Replant();
            Tints.Wash(skin, tint);
        }

        void Replant()
        {
            if (!stood)
            {
                return;
            }

            var scale = transform.localScale.x;
            transform.localPosition = new Vector3(ground.X, ground.Y + scale, ground.Z);

            if (badge != null)
            {
                badge.localPosition = new Vector3(
                    ground.X, BadgeMetrics.AnchorAbove(ground.Y + scale * 2f), ground.Z);
            }
        }
    }
}
