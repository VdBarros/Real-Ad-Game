using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class BadgePlan
    {
        internal BadgePlan(int capacity, long powerCeiling)
        {
            Capacity = capacity;
            PowerCeiling = powerCeiling;
            PlayerWidth = BadgeMetrics.WidthFor(capacity);
            Height = BadgeMetrics.Height;
            FontSize = BadgeMetrics.FontSize;
        }

        public int Capacity { get; }

        public long PowerCeiling { get; }

        public float PlayerWidth { get; }

        public float Height { get; }

        public float FontSize { get; }

        public static BadgePlan For(LevelGraph graph, int startingPower)
        {
            return BadgeBlueprintBuilder.Build(graph, startingPower).Plan;
        }
    }
}
