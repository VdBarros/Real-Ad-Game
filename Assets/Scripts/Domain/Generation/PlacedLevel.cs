using System;

namespace Game.Domain
{
    public sealed class PlacedLevel
    {
        public PlacedLevel(
            MazeLayout layout,
            LevelGraph graph,
            ContentRecipe recipe,
            PowerTuning tuning,
            int bossNodeId,
            long invariantBBound,
            int shortestPathPower,
            bool shortestPathBlocked,
            int floorRepairPasses,
            PowerEnvelope envelope)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            Layout = layout;
            Graph = graph;
            Recipe = recipe;
            Tuning = tuning;
            BossNodeId = bossNodeId;
            InvariantBBound = invariantBBound;
            ShortestPathPower = shortestPathPower;
            ShortestPathBlocked = shortestPathBlocked;
            FloorRepairPasses = floorRepairPasses;
            Envelope = envelope;
        }

        public MazeLayout Layout { get; }

        public LevelGraph Graph { get; }

        public ContentRecipe Recipe { get; }

        public PowerTuning Tuning { get; }

        public int BossNodeId { get; }

        public long InvariantBBound { get; }

        public int ShortestPathPower { get; }

        public bool ShortestPathBlocked { get; }

        public int FloorRepairPasses { get; }

        public PowerEnvelope Envelope { get; }

        public long AttemptSeed
        {
            get { return Layout.AttemptSeed; }
        }

        public MazePreset Preset
        {
            get { return Layout.Preset; }
        }

        public TileDistanceMap DistanceFromStart
        {
            get { return Layout.DistanceFromStart; }
        }

        public LayoutMetrics Metrics
        {
            get { return Layout.Metrics; }
        }

        public int StartNodeId
        {
            get { return Layout.StartNodeId; }
        }

        public int StartingPower
        {
            get { return Tuning.StartingPower; }
        }

        public int BossPower
        {
            get { return Graph.Decisions.Node(BossNodeId).Value; }
        }
    }
}
