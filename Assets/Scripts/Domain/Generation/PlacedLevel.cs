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
            int floorRepairPasses,
            PowerEnvelope envelope,
            SolvabilityVerdict verdict)
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

            if (verdict == null)
            {
                throw new ArgumentNullException(nameof(verdict));
            }

            if (!verdict.IsSafe)
            {
                throw new ArgumentException("A placed level is one the validator cleared: " + verdict, nameof(verdict));
            }

            Layout = layout;
            Graph = graph;
            Recipe = recipe;
            Tuning = tuning;
            Plan = new LevelPlan(layout.Preset, recipe, tuning);
            FloorRepairPasses = floorRepairPasses;
            Envelope = envelope;
            Verdict = verdict;
        }

        public LevelPlan Plan { get; }

        public MazeLayout Layout { get; }

        public LevelGraph Graph { get; }

        public ContentRecipe Recipe { get; }

        public PowerTuning Tuning { get; }

        public int FloorRepairPasses { get; }

        public PowerEnvelope Envelope { get; }

        public SolvabilityVerdict Verdict { get; }

        public int BossNodeId
        {
            get { return Verdict.BossNodeId; }
        }

        public long InvariantBBound
        {
            get { return Verdict.Bound; }
        }

        public int ShortestPathPower
        {
            get { return Verdict.BeelinePower; }
        }

        public bool ShortestPathBlocked
        {
            get { return Verdict.BeelineBlocked; }
        }

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
            get { return Verdict.BossPower; }
        }
    }
}
