using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class BadgeFactory
    {
        public static NumberBadge Raise(BadgePart part, BadgePlan plan, BadgeAssets assets, Transform parent)
        {
            var instance = new GameObject(part.Name);
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.localPosition = new Vector3(part.Position.X, part.Position.Y, part.Position.Z);
            instance.transform.localEulerAngles = new Vector3(part.Rotation.X, part.Rotation.Y, part.Rotation.Z);

            var badge = instance.AddComponent<NumberBadge>();
            badge.Compose(part, plan, assets);
            return badge;
        }
    }
}
