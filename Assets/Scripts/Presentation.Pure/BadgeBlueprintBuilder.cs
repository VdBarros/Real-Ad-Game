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

            var ceiling = PowerCeiling.Of(graph, startingPower);
            var plan = new BadgePlan(CapacityFor(graph, ceiling), ceiling);
            var badges = new List<BadgePart>();

            foreach (var node in graph.Decisions.Nodes)
            {
                BadgeStyle style;
                WorldPart prop;
                if (!BadgeStyles.TryOf(node.Type, out style) || !LevelBlueprintBuilder.TryProp(node, out prop))
                {
                    continue;
                }

                var isPlayer = style == BadgeStyle.Player;
                var value = isPlayer ? startingPower : node.Value;
                var tile = IsoProjection.Of(node.Position);

                badges.Add(new BadgePart(
                    PartNames.Badge(node.Id),
                    node.Id,
                    node.Position.Floor,
                    style,
                    value,
                    isPlayer ? plan.Capacity : BadgeText.Cells(style, value),
                    new WorldPoint(tile.X, BadgeMetrics.AnchorHeight(prop), tile.Z),
                    IsoProjection.CameraRotation));
            }

            return new BadgeBlueprint(plan, badges);
        }

        static int CapacityFor(LevelGraph graph, long ceiling)
        {
            var capacity = BadgeText.Digits(ceiling);

            foreach (var node in graph.Decisions.Nodes)
            {
                BadgeStyle style;
                if (!BadgeStyles.TryOf(node.Type, out style) || style == BadgeStyle.Player)
                {
                    continue;
                }

                var cells = BadgeText.Cells(style, node.Value);
                if (cells > capacity)
                {
                    capacity = cells;
                }
            }

            return capacity;
        }
    }
}
