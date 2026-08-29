using System;
using System.Collections.Generic;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public static class BadgeBlueprintBuilder
    {
        public static BadgeBlueprint Build(LevelGraph graph, int startingPower)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var badges = new List<BadgePart>();

            foreach (var node in graph.Decisions.Nodes)
            {
                BadgeStyle style;
                WorldPart prop;
                if (!BadgeStyles.TryOf(node.Type, out style) || !LevelBlueprintBuilder.TryProp(node, out prop))
                {
                    continue;
                }

                var value = style == BadgeStyle.Player ? startingPower : node.Value;
                var tile = IsoProjection.Of(node.Position);

                badges.Add(new BadgePart(
                    PartNames.Badge(node.Id),
                    node.Id,
                    node.Position.Elevation,
                    style,
                    value,
                    BadgeText.Cells(style, value),
                    WorldParts.WidthOf(prop),
                    new WorldPoint(tile.X, BadgeMetrics.AnchorHeight(prop), tile.Z),
                    IsoProjection.CameraRotation));
            }

            return new BadgeBlueprint(new BadgePlan(CapacityOf(badges)), badges);
        }

        static int CapacityOf(IReadOnlyList<BadgePart> badges)
        {
            var capacity = (int)BadgeMetrics.MinimumCells;

            for (var slot = 0; slot < badges.Count; slot++)
            {
                if (badges[slot].Cells > capacity)
                {
                    capacity = badges[slot].Cells;
                }
            }

            return capacity;
        }
    }
}
