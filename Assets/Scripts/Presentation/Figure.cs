using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public abstract class Figure : MonoBehaviour
    {
        Transform badge;

        Renderer skin;

        float tileHeight;

        protected float BaseScale { get; private set; }

        protected float TileHeight
        {
            get { return tileHeight; }
        }

        internal void Stand(Transform hangingBadge)
        {
            badge = hangingBadge;
            skin = GetComponent<Renderer>();
            BaseScale = transform.localScale.x;
            tileHeight = transform.localPosition.y - BaseScale;
        }

        protected void Wear(float scale, Tint tint)
        {
            transform.localScale = new Vector3(scale, scale, scale);

            var standing = transform.localPosition;
            transform.localPosition = new Vector3(standing.x, tileHeight + scale, standing.z);

            if (badge != null)
            {
                var hanging = badge.localPosition;
                badge.localPosition = new Vector3(
                    hanging.x, BadgeMetrics.AnchorAbove(tileHeight + scale * 2f), hanging.z);
            }

            Tints.Wash(skin, tint);
        }
    }
}
