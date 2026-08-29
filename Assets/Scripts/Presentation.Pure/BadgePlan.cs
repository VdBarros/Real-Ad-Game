using Game.Domain;

namespace Game.Presentation.Pure
{
    public sealed class BadgePlan
    {
        internal BadgePlan(int capacity)
        {
            Capacity = capacity;
            Height = BadgeMetrics.Height;
            FontSize = BadgeMetrics.FontSize;
        }

        public int Capacity { get; }

        public float Height { get; }

        public float FontSize { get; }

        public static BadgePlan For(LevelGraph graph, int startingPower)
        {
            return BadgeBlueprintBuilder.Build(graph, startingPower).Plan;
        }
    }
}
