using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public static class MazeLayoutGenerator
    {
        public const int MaximumAttempts = 50;

        const int RejectionCount = (int)LayoutRejection.TooFewOffPathSlots + 1;

        public static MazeLayout Generate(long seed, MazePreset preset)
        {
            MazeGenerationReport report;
            return Generate(seed, preset, out report);
        }

        public static MazeLayout Generate(long seed, MazePreset preset, out MazeGenerationReport report)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            var countByRejection = new int[RejectionCount];

            for (var attempt = 0; attempt < MaximumAttempts; attempt++)
            {
                MazeLayout layout;
                LayoutRejection rejection;
                if (TryGenerate(SeedOfAttempt(seed, attempt), preset, out layout, out rejection))
                {
                    report = new MazeGenerationReport(preset, attempt + 1, countByRejection);
                    return layout;
                }

                countByRejection[(int)rejection]++;
            }

            throw new MazeGenerationException(
                new MazeGenerationReport(preset, MaximumAttempts, countByRejection));
        }

        public static bool TryGenerate(
            long seed, MazePreset preset, out MazeLayout layout, out LayoutRejection rejection)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            layout = null;
            rejection = LayoutRejection.None;

            var carved = MazeCarver.Carve(seed, preset);
            var geometry = new TileGrid(Unpainted(carved.Tiles), carved.Stairs);
            var topology = new TileTopology(geometry);

            var start = ChooseStart(StageRandom.ForStage(seed, "start"), topology);
            if (topology.ReachedFrom(start) != topology.Count)
            {
                rejection = LayoutRejection.TilesDisconnected;
                return false;
            }

            var plan = new LayoutPlan(topology, start);
            RegionPainter.Paint(seed, preset, plan);

            rejection = SlotSelector.Fill(
                seed, preset, plan, CorridorExtractor.Extract(topology.Neighbours, plan.NodeTiles));
            if (rejection != LayoutRejection.None)
            {
                return false;
            }

            var runs = CorridorExtractor.ExtractWithoutSelfLoopsOrParallelRuns(
                topology.Neighbours, plan.NodeTiles);

            var graph = Assemble(seed, preset, plan, carved, runs);
            RequireEmptyCorridorsToJoinStairs(graph);

            var distanceFromStart = TileDistanceMap.From(graph.Tiles, topology[start]);
            var metrics = LayoutMetrics.Of(graph, distanceFromStart);

            if (metrics.BossDepth < preset.MinimumBossDepth)
            {
                rejection = LayoutRejection.BossTooShallow;
                return false;
            }

            if (metrics.OffPathSlotCount < preset.MinimumOffPathSlots)
            {
                rejection = LayoutRejection.TooFewOffPathSlots;
                return false;
            }

            layout = new MazeLayout(seed, preset, graph, distanceFromStart, metrics);
            return true;
        }

        public static long SeedOfAttempt(long seed, int attempt)
        {
            unchecked
            {
                return seed + attempt * (long)0x9E3779B97F4A7C15UL;
            }
        }

        static IEnumerable<Tile> Unpainted(IReadOnlyList<TilePosition> positions)
        {
            var tiles = new List<Tile>(positions.Count);
            foreach (var position in positions)
            {
                tiles.Add(new Tile(position, 0));
            }

            return tiles;
        }

        static int ChooseStart(StageRandom random, TileTopology topology)
        {
            var ground = topology.TilesOnFloor(0);
            var rim = new List<int>();
            foreach (var tile in ground)
            {
                if (topology.Degree(tile) == 1)
                {
                    rim.Add(tile);
                }
            }

            return rim.Count > 0 ? random.Pick(rim) : ground[0];
        }

        static LevelGraph Assemble(
            long seed,
            MazePreset preset,
            LayoutPlan plan,
            CarvedMaze carved,
            IReadOnlyList<CorridorRun> runs)
        {
            var topology = plan.Topology;
            var builder = new LevelGraphBuilder(seed, preset.Name);

            for (var tile = 0; tile < topology.Count; tile++)
            {
                builder.AddTile(topology[tile], plan.RegionOf(tile));
            }

            foreach (var stair in carved.Stairs)
            {
                builder.AddStair(stair.Lower, stair.Upper);
            }

            for (var tile = 0; tile < topology.Count; tile++)
            {
                if (plan.IsNode(tile))
                {
                    builder.AddNode(topology[tile], plan.TypeOf(tile));
                }
            }

            foreach (var run in runs)
            {
                var path = new List<TilePosition>(run.Path.Count);
                foreach (var tile in run.Path)
                {
                    path.Add(topology[tile]);
                }

                builder.Connect(topology[run.LowTile], topology[run.HighTile], path);
            }

            return builder.Build();
        }

        static void RequireEmptyCorridorsToJoinStairs(LevelGraph graph)
        {
            foreach (var corridor in graph.Decisions.Corridors)
            {
                if (corridor.TilePath.Count != 0)
                {
                    continue;
                }

                var low = graph.Decisions.Node(corridor.LowNodeId);
                var high = graph.Decisions.Node(corridor.HighNodeId);
                if (low.Type != NodeType.Empty || high.Type != NodeType.Empty)
                {
                    continue;
                }

                if (graph.Tiles.CarriesStair(low.Position) && graph.Tiles.CarriesStair(high.Position))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    "Corridor " + corridor + " joins two Empty nodes with no tiles between them, "
                    + "which only a stair may do.");
            }
        }
    }
}
