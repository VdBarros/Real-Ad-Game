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
                    return layout;
                }

                countByRejection[(int)rejection]++;
            }

            throw new MazeGenerationException(preset, MaximumAttempts, countByRejection);
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
            var index = new TileIndex(carved.Tiles);
            var geometry = new TileGrid(Unpainted(carved.Tiles), carved.Stairs);
            var adjacency = Adjacency(geometry, index);

            var isStair = new bool[index.Count];
            foreach (var stairTile in carved.StairTiles)
            {
                isStair[index.Of(stairTile)] = true;
            }

            var start = ChooseStart(StageRandom.ForStage(seed, "start"), index, adjacency);
            if (ReachedCount(adjacency, start) != index.Count)
            {
                rejection = LayoutRejection.TilesDisconnected;
                return false;
            }

            var regions = PaintRegions(seed, preset, index, adjacency);

            var isNode = new bool[index.Count];
            for (var tile = 0; tile < index.Count; tile++)
            {
                isNode[tile] = adjacency[tile].Length != 2 || isStair[tile] || tile == start;
            }

            var runs = CorridorExtractor.Extract(adjacency, isNode);

            var isSlot = new bool[index.Count];
            rejection = ChooseSlots(seed, preset, index, adjacency, regions, runs, isNode, start, isSlot);
            if (rejection != LayoutRejection.None)
            {
                return false;
            }

            for (var tile = 0; tile < index.Count; tile++)
            {
                if (isSlot[tile])
                {
                    isNode[tile] = true;
                }
            }

            runs = ExtractCorridorsTheGraphModelAccepts(adjacency, isNode);

            var graph = Assemble(seed, preset, index, carved, regions, isNode, isSlot, start, runs);
            RequireEmptyCorridorsToJoinStairs(graph);

            var distanceFromStart = TileDistanceMap.From(graph.Tiles, index[start]);
            var metrics = Measure(graph, distanceFromStart);

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

        static long SeedOfAttempt(long seed, int attempt)
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

        static int[][] Adjacency(TileGrid geometry, TileIndex index)
        {
            var adjacency = new int[index.Count][];
            for (var tile = 0; tile < index.Count; tile++)
            {
                var neighbours = geometry.Neighbours(index[tile]);
                var mapped = new int[neighbours.Count];
                for (var slot = 0; slot < neighbours.Count; slot++)
                {
                    mapped[slot] = index.Of(neighbours[slot]);
                }

                adjacency[tile] = mapped;
            }

            return adjacency;
        }

        static int ChooseStart(StageRandom random, TileIndex index, int[][] adjacency)
        {
            var rim = new List<int>();
            var ground = new List<int>();
            for (var tile = 0; tile < index.Count; tile++)
            {
                if (index[tile].Floor != 0)
                {
                    continue;
                }

                ground.Add(tile);
                if (adjacency[tile].Length == 1)
                {
                    rim.Add(tile);
                }
            }

            return rim.Count > 0 ? random.Pick(rim) : ground[0];
        }

        static int ReachedCount(int[][] adjacency, int source)
        {
            var seen = new bool[adjacency.Length];
            var queue = new List<int> { source };
            seen[source] = true;

            for (var head = 0; head < queue.Count; head++)
            {
                foreach (var neighbour in adjacency[queue[head]])
                {
                    if (seen[neighbour])
                    {
                        continue;
                    }

                    seen[neighbour] = true;
                    queue.Add(neighbour);
                }
            }

            return queue.Count;
        }

        static int[] PaintRegions(long seed, MazePreset preset, TileIndex index, int[][] adjacency)
        {
            var region = new int[index.Count];
            for (var tile = 0; tile < region.Length; tile++)
            {
                region[tile] = -1;
            }

            var perFloor = preset.RegionsPerFloor;

            for (var floor = 0; floor < preset.Floors; floor++)
            {
                var onThisFloor = new List<int>();
                for (var tile = 0; tile < index.Count; tile++)
                {
                    if (index[tile].Floor == floor)
                    {
                        onThisFloor.Add(tile);
                    }
                }

                var random = StageRandom.ForStage(seed, "regions:" + floor);
                var sources = FarthestApart(random, index, onThisFloor, perFloor);
                var baseRegionId = floor * perFloor;

                var queue = new List<int>();
                for (var source = 0; source < sources.Count; source++)
                {
                    region[sources[source]] = baseRegionId + source;
                    queue.Add(sources[source]);
                }

                for (var head = 0; head < queue.Count; head++)
                {
                    var current = queue[head];
                    foreach (var neighbour in adjacency[current])
                    {
                        if (index[neighbour].Floor != floor || region[neighbour] >= 0)
                        {
                            continue;
                        }

                        region[neighbour] = region[current];
                        queue.Add(neighbour);
                    }
                }

                foreach (var tile in onThisFloor)
                {
                    if (region[tile] < 0)
                    {
                        region[tile] = baseRegionId;
                    }
                }
            }

            return region;
        }

        static List<int> FarthestApart(StageRandom random, TileIndex index, IReadOnlyList<int> candidates, int wanted)
        {
            var taken = new bool[index.Count];
            var sources = new List<int> { random.Pick(candidates) };
            taken[sources[0]] = true;

            while (sources.Count < wanted)
            {
                var best = -1;
                var bestDistance = -1;

                foreach (var candidate in candidates)
                {
                    if (taken[candidate])
                    {
                        continue;
                    }

                    var nearest = int.MaxValue;
                    foreach (var source in sources)
                    {
                        var distance = Manhattan(index[source], index[candidate]);
                        if (distance < nearest)
                        {
                            nearest = distance;
                        }
                    }

                    if (nearest > bestDistance)
                    {
                        bestDistance = nearest;
                        best = candidate;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                taken[best] = true;
                sources.Add(best);
            }

            return sources;
        }

        static int Manhattan(TilePosition first, TilePosition second)
        {
            return Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
        }

        static List<CorridorRun> ExtractCorridorsTheGraphModelAccepts(int[][] adjacency, bool[] isNode)
        {
            while (true)
            {
                var runs = CorridorExtractor.Extract(adjacency, isNode);
                var offender = CorridorExtractor.FirstThatBreaksTheGraphModel(runs);
                if (offender == null)
                {
                    return runs;
                }

                isNode[offender.Path[offender.Path.Count / 2]] = true;
            }
        }

        static LayoutRejection ChooseSlots(
            long seed,
            MazePreset preset,
            TileIndex index,
            int[][] adjacency,
            int[] regions,
            IReadOnlyList<CorridorRun> runs,
            bool[] isNode,
            int start,
            bool[] isSlot)
        {
            var pockets = new List<int>();
            var junctions = new List<int>();
            for (var tile = 0; tile < index.Count; tile++)
            {
                if (!isNode[tile] || tile == start)
                {
                    continue;
                }

                if (adjacency[tile].Length == 1)
                {
                    pockets.Add(tile);
                }
                else
                {
                    junctions.Add(tile);
                }
            }

            if (pockets.Count > preset.ContentSlots)
            {
                return LayoutRejection.PocketOverflow;
            }

            var candidates = new List<int>();
            foreach (var run in runs)
            {
                for (var step = 0; step < run.Path.Count; step += 2)
                {
                    candidates.Add(run.Path[step]);
                }
            }

            candidates.AddRange(junctions);

            var chosen = 0;
            foreach (var tile in pockets)
            {
                isSlot[tile] = true;
                chosen++;
            }

            var pool = StageRandom.ForStage(seed, "slots").Shuffled(candidates);

            for (var regionId = 0; regionId < preset.Regions && chosen < preset.ContentSlots; regionId++)
            {
                if (AnySlotInRegion(regions, isSlot, regionId))
                {
                    continue;
                }

                foreach (var tile in pool)
                {
                    if (isSlot[tile] || regions[tile] != regionId)
                    {
                        continue;
                    }

                    isSlot[tile] = true;
                    chosen++;
                    break;
                }
            }

            foreach (var tile in pool)
            {
                if (chosen >= preset.ContentSlots)
                {
                    break;
                }

                if (isSlot[tile])
                {
                    continue;
                }

                isSlot[tile] = true;
                chosen++;
            }

            return chosen < preset.ContentSlots ? LayoutRejection.SlotShortfall : LayoutRejection.None;
        }

        static bool AnySlotInRegion(int[] regions, bool[] isSlot, int regionId)
        {
            for (var tile = 0; tile < isSlot.Length; tile++)
            {
                if (isSlot[tile] && regions[tile] == regionId)
                {
                    return true;
                }
            }

            return false;
        }

        static LevelGraph Assemble(
            long seed,
            MazePreset preset,
            TileIndex index,
            CarvedMaze carved,
            int[] regions,
            bool[] isNode,
            bool[] isSlot,
            int start,
            IReadOnlyList<CorridorRun> runs)
        {
            var builder = new LevelGraphBuilder(seed, preset.Name);

            for (var tile = 0; tile < index.Count; tile++)
            {
                builder.AddTile(index[tile], regions[tile]);
            }

            foreach (var stair in carved.Stairs)
            {
                builder.AddStair(stair.Lower, stair.Upper);
            }

            for (var tile = 0; tile < index.Count; tile++)
            {
                if (!isNode[tile])
                {
                    continue;
                }

                builder.AddNode(index[tile], TypeOfNode(tile, start, isSlot));
            }

            foreach (var run in runs)
            {
                var path = new List<TilePosition>(run.Path.Count);
                foreach (var tile in run.Path)
                {
                    path.Add(index[tile]);
                }

                builder.Connect(index[run.First], index[run.Second], path);
            }

            return builder.Build();
        }

        static NodeType TypeOfNode(int tile, int start, bool[] isSlot)
        {
            if (tile == start)
            {
                return NodeType.Start;
            }

            return isSlot[tile] ? NodeType.Unassigned : NodeType.Empty;
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

                if (CarriesStair(graph.Tiles, low.Position) && CarriesStair(graph.Tiles, high.Position))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    "Corridor " + corridor + " joins two Empty nodes with no tiles between them, "
                    + "which only a stair may do.");
            }
        }

        static bool CarriesStair(TileGrid grid, TilePosition position)
        {
            foreach (var stair in grid.Stairs)
            {
                if (stair.Lower.Equals(position) || stair.Upper.Equals(position))
                {
                    return true;
                }
            }

            return false;
        }

        static LayoutMetrics Measure(LevelGraph graph, TileDistanceMap distanceFromStart)
        {
            var isGate = new bool[graph.Decisions.Nodes.Count];
            foreach (var nodeId in ArticulationPoints.Of(graph.Decisions))
            {
                isGate[nodeId] = true;
            }

            var slots = new List<DecisionNode>();
            var emptyCount = 0;
            foreach (var node in graph.Decisions.Nodes)
            {
                if (node.Type == NodeType.Unassigned)
                {
                    slots.Add(node);
                }
                else if (node.Type == NodeType.Empty)
                {
                    emptyCount++;
                }
            }

            var gateCount = 0;
            var pocketCount = 0;
            DecisionNode deepest = null;

            foreach (var slot in slots)
            {
                if (isGate[slot.Id])
                {
                    gateCount++;
                }

                if (graph.Tiles.Neighbours(slot.Position).Count == 1)
                {
                    pocketCount++;
                }

                if (deepest == null
                    || distanceFromStart.DistanceTo(slot.Position) > distanceFromStart.DistanceTo(deepest.Position))
                {
                    deepest = slot;
                }
            }

            var bossDepth = 0;
            var offPathSlotCount = 0;

            if (deepest != null)
            {
                bossDepth = distanceFromStart.DistanceTo(deepest.Position);
                var distanceFromDeepest = TileDistanceMap.From(graph.Tiles, deepest.Position);

                foreach (var slot in slots)
                {
                    if (slot.Id == deepest.Id)
                    {
                        continue;
                    }

                    var throughSlot = distanceFromStart.DistanceTo(slot.Position)
                        + distanceFromDeepest.DistanceTo(slot.Position);
                    if (throughSlot != bossDepth)
                    {
                        offPathSlotCount++;
                    }
                }
            }

            return new LayoutMetrics(
                graph.Tiles.Tiles.Count,
                graph.Decisions.Nodes.Count,
                slots.Count,
                emptyCount,
                graph.Decisions.Corridors.Count,
                gateCount,
                pocketCount,
                bossDepth,
                offPathSlotCount);
        }
    }
}
