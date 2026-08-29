using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class CrowdBoard : MonoBehaviour
    {
        public const int Layers = 2;

        readonly List<NumberBadge> badges = new List<NumberBadge>();

        readonly List<int> nodes = new List<int>();

        readonly List<int> elevations = new List<int>();

        readonly List<BadgeSpot> spots = new List<BadgeSpot>();

        public BadgeStack Stack { get; private set; }

        public IReadOnlyList<BadgeSpot> Spots
        {
            get { return spots; }
        }

        public IReadOnlyList<NumberBadge> Badges
        {
            get { return badges; }
        }

        internal void Begin()
        {
            badges.Clear();
            nodes.Clear();
            elevations.Clear();
            spots.Clear();
            Stack = BadgeStack.Empty;
        }

        internal void Adopt(NumberBadge badge, BadgePart part)
        {
            if (badge == null)
            {
                throw new ArgumentNullException(nameof(badge));
            }

            badges.Add(badge);
            nodes.Add(part.NodeId);
            elevations.Add(part.Elevation);
        }

        public void Settle()
        {
            spots.Clear();

            for (var slot = 0; slot < badges.Count; slot++)
            {
                var badge = badges[slot];
                if (badge == null || !badge.gameObject.activeInHierarchy)
                {
                    continue;
                }

                badge.Reseat();
                spots.Add(SpotOf(slot));
            }

            Stack = BadgeCrowd.Resolve(spots);

            for (var slot = 0; slot < badges.Count; slot++)
            {
                var badge = badges[slot];
                if (badge == null)
                {
                    continue;
                }

                BadgeSeat seat;
                if (!Stack.TryOf(nodes[slot], out seat))
                {
                    continue;
                }

                badge.Raise(seat.Lift);
                badge.Fade(seat.Opacity);
                badge.Draw(seat.Order * Layers);
            }
        }

        public void Flatten()
        {
            for (var slot = 0; slot < badges.Count; slot++)
            {
                var badge = badges[slot];
                if (badge == null)
                {
                    continue;
                }

                badge.Reseat();
                badge.Raise(0f);
                badge.Fade(1f);
                badge.Draw(0);
            }

            Stack = BadgeStack.Empty;
        }

        BadgeSpot SpotOf(int slot)
        {
            var badge = badges[slot];
            var home = badge.Home;
            var worn = badge.transform.localScale.x;

            return new BadgeSpot(
                nodes[slot],
                elevations[slot],
                new WorldPoint(home.x, home.y, home.z),
                badge.Width * worn,
                badge.Height * worn);
        }

        void LateUpdate()
        {
            Settle();
        }
    }
}
