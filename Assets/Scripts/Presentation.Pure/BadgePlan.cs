using System;
using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class BadgePlan
    {
        BadgePlan(int capacity, long powerCeiling)
        {
            Capacity = capacity;
            PowerCeiling = powerCeiling;
            PlayerWidth = BadgeMetrics.WidthFor(capacity);
            Height = BadgeMetrics.Height;
            FontSize = BadgeMetrics.FontSizeFor(capacity);
        }

        public int Capacity { get; }

        public long PowerCeiling { get; }

        public float PlayerWidth { get; }

        public float Height { get; }

        public float FontSize { get; }

        public static BadgePlan For(LevelGraph graph, int startingPower)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var ceiling = PlayerPowerCeiling.Of(graph, startingPower);
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

            return new BadgePlan(capacity, ceiling);
        }
    }
}
