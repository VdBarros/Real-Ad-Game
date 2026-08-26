using System;
using System.Collections.Generic;

namespace Game.Domain
{
    static class MazeCarver
    {
        static readonly IReadOnlyList<LatticeCell> Steps = new[]
        {
            new LatticeCell(1, 0),
            new LatticeCell(-1, 0),
            new LatticeCell(0, 1),
            new LatticeCell(0, -1)
        };

        public static CarvedMaze Carve(long seed, MazePreset preset)
        {
            var walkable = new HashSet<TilePosition>();
            for (var terrace = 0; terrace < preset.Terraces; terrace++)
            {
                CarveTerrace(StageRandom.ForStage(seed, "carve:" + terrace), preset, terrace, walkable);
                Braid(StageRandom.ForStage(seed, "braid:" + terrace), preset, terrace, walkable);
            }

            var staircases = Climbs(StageRandom.ForStage(seed, "stairs"), preset);
            walkable.UnionWith(staircases);

            var staircaseTiles = new List<TilePosition>(staircases);
            staircaseTiles.Sort();

            var tiles = new List<TilePosition>(walkable);
            tiles.Sort();

            return new CarvedMaze(tiles, Array.Empty<StairLink>(), staircaseTiles);
        }

        static void CarveTerrace(StageRandom random, MazePreset preset, int terrace, HashSet<TilePosition> walkable)
        {
            var visited = new bool[preset.LatticeWidth * preset.LatticeHeight];
            var trail = new List<LatticeCell> { new LatticeCell(0, 0) };
            visited[0] = true;
            walkable.Add(TileOfCell(preset, terrace, new LatticeCell(0, 0)));

            while (trail.Count > 0)
            {
                var cell = trail[trail.Count - 1];
                var stepped = false;

                foreach (var step in random.Shuffled(Steps))
                {
                    var next = cell.Shifted(step);
                    if (!InsideLattice(preset, next) || visited[IndexOfCell(preset, next)])
                    {
                        continue;
                    }

                    visited[IndexOfCell(preset, next)] = true;
                    walkable.Add(TileOfCell(preset, terrace, next));
                    walkable.Add(TileBetweenCells(preset, terrace, cell, next));
                    trail.Add(next);
                    stepped = true;
                    break;
                }

                if (!stepped)
                {
                    trail.RemoveAt(trail.Count - 1);
                }
            }
        }

        static void Braid(StageRandom random, MazePreset preset, int terrace, HashSet<TilePosition> walkable)
        {
            var deadEnds = new List<LatticeCell>();
            for (var y = 0; y < preset.LatticeHeight; y++)
            {
                for (var x = 0; x < preset.LatticeWidth; x++)
                {
                    var cell = new LatticeCell(x, y);
                    if (OpenSideCount(preset, terrace, walkable, cell) == 1)
                    {
                        deadEnds.Add(cell);
                    }
                }
            }

            var reopened = (int)Math.Floor(deadEnds.Count * preset.BraidFactor + 0.5);
            var chosen = random.Shuffled(deadEnds);

            for (var index = 0; index < reopened; index++)
            {
                var cell = chosen[index];
                var blocked = new List<TilePosition>();
                foreach (var step in Steps)
                {
                    var beyond = cell.Shifted(step);
                    if (!InsideLattice(preset, beyond))
                    {
                        continue;
                    }

                    var wall = TileBetweenCells(preset, terrace, cell, beyond);
                    if (!walkable.Contains(wall))
                    {
                        blocked.Add(wall);
                    }
                }

                if (blocked.Count == 0)
                {
                    continue;
                }

                walkable.Add(random.Pick(blocked));
            }
        }

        static IReadOnlyList<TilePosition> Climbs(StageRandom random, MazePreset preset)
        {
            var staircases = new HashSet<TilePosition>();
            if (preset.Terraces < 2 || preset.Stairs < 1)
            {
                return new List<TilePosition>();
            }

            for (var gap = 0; gap < preset.Terraces - 1; gap++)
            {
                var landing = preset.LatticeHeight - 1;
                foreach (var column in FootColumns(random, preset, WaysUpOverGap(preset, gap)))
                {
                    Climb(preset, gap, column, landing, staircases);
                    if (column < preset.LatticeHeight)
                    {
                        landing--;
                    }
                }
            }

            var ordered = new List<TilePosition>(staircases);
            ordered.Sort();
            return ordered;
        }

        static int WaysUpOverGap(MazePreset preset, int gap)
        {
            var wanted = 0;
            for (var index = 0; index < preset.Stairs; index++)
            {
                if (index % (preset.Terraces - 1) == gap)
                {
                    wanted++;
                }
            }

            return wanted;
        }

        static IReadOnlyList<int> FootColumns(StageRandom random, MazePreset preset, int wanted)
        {
            var offered = new List<int>();
            for (var column = 0; column < preset.LatticeWidth; column++)
            {
                offered.Add(column);
            }

            var chosen = new List<int>();
            foreach (var column in random.Shuffled(offered))
            {
                if (chosen.Count >= wanted)
                {
                    break;
                }

                if (FarEnoughFromAll(chosen, column))
                {
                    chosen.Add(column);
                }
            }

            chosen.Sort();
            return chosen;
        }

        static bool FarEnoughFromAll(IReadOnlyList<int> chosen, int column)
        {
            foreach (var other in chosen)
            {
                if (Math.Abs(other - column) < 3)
                {
                    return false;
                }
            }

            return true;
        }

        static void Climb(
            MazePreset preset, int gap, int column, int landing, HashSet<TilePosition> staircases)
        {
            var offset = preset.TerraceOffset * gap;
            var step = offset + 2 * column;
            var gapRow = offset + 2 * preset.LatticeHeight - 1;
            var climbing = Terraces.ElevationOf(gap) + 1;

            staircases.Add(new TilePosition(climbing, step, gapRow));

            if (column >= preset.LatticeHeight)
            {
                return;
            }

            var landingRow = gapRow + 1 + 2 * Math.Max(landing, 0);

            for (var row = gapRow + 1; row <= landingRow; row++)
            {
                staircases.Add(new TilePosition(climbing, step, row));
            }

            for (var across = step + 1; across < offset + preset.TerraceOffset; across++)
            {
                staircases.Add(new TilePosition(climbing, across, landingRow));
            }
        }

        static int OpenSideCount(MazePreset preset, int terrace, HashSet<TilePosition> walkable, LatticeCell cell)
        {
            var open = 0;
            foreach (var step in Steps)
            {
                var beyond = cell.Shifted(step);
                if (InsideLattice(preset, beyond) && walkable.Contains(TileBetweenCells(preset, terrace, cell, beyond)))
                {
                    open++;
                }
            }

            return open;
        }

        static bool InsideLattice(MazePreset preset, LatticeCell cell)
        {
            return cell.X >= 0 && cell.Y >= 0 && cell.X < preset.LatticeWidth && cell.Y < preset.LatticeHeight;
        }

        static int IndexOfCell(MazePreset preset, LatticeCell cell)
        {
            return cell.Y * preset.LatticeWidth + cell.X;
        }

        static TilePosition TileOfCell(MazePreset preset, int terrace, LatticeCell cell)
        {
            var offset = preset.TerraceOffset * terrace;
            return new TilePosition(Terraces.ElevationOf(terrace), offset + 2 * cell.X, offset + 2 * cell.Y);
        }

        static TilePosition TileBetweenCells(MazePreset preset, int terrace, LatticeCell from, LatticeCell to)
        {
            var offset = preset.TerraceOffset * terrace;
            return new TilePosition(
                Terraces.ElevationOf(terrace), offset + from.X + to.X, offset + from.Y + to.Y);
        }

        readonly struct LatticeCell
        {
            public LatticeCell(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }

            public int Y { get; }

            public LatticeCell Shifted(LatticeCell step)
            {
                return new LatticeCell(X + step.X, Y + step.Y);
            }
        }
    }
}
