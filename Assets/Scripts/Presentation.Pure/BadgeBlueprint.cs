using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public sealed class BadgeBlueprint
    {
        public BadgeBlueprint(BadgePlan plan, IReadOnlyList<BadgePart> badges)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (badges == null)
            {
                throw new ArgumentNullException(nameof(badges));
            }

            Plan = plan;
            Badges = badges;
        }

        public BadgePlan Plan { get; }

        public IReadOnlyList<BadgePart> Badges { get; }
    }
}
