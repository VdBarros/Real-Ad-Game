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
            for (var floor = 0; floor < preset.Floors; floor++)
            {
                CarveFloor(StageRandom.ForStage(seed, "carve:" + floor), preset, floor, walkable);
                Braid(StageRandom.ForStage(seed, "braid:" + floor), preset, floor, walkable);
            }

            var stairs = LinkFloors(StageRandom.ForStage(seed, "stairs"), preset);

            var stairTiles = new List<TilePosition>();
            foreach (var stair in stairs)
            {
                stairTiles.Add(stair.Lower);
                stairTiles.Add(stair.Upper);
            }

            stairTiles.Sort();

            var tiles = new List<TilePosition>(walkable);
            tiles.Sort();

            return new CarvedMaze(tiles, stairs, stairTiles);
        }

        static void CarveFloor(StageRandom random, MazePreset preset, int floor, HashSet<TilePosition> walkable)
        {
            var visited = new bool[preset.LatticeWidth * preset.LatticeHeight];
            var trail = new List<LatticeCell> { new LatticeCell(0, 0) };
            visited[0] = true;
            walkable.Add(TileOfCell(floor, new LatticeCell(0, 0)));

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
                    walkable.Add(TileOfCell(floor, next));
                    walkable.Add(TileBetweenCells(floor, cell, next));
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

        static void Braid(StageRandom random, MazePreset preset, int floor, HashSet<TilePosition> walkable)
        {
            var deadEnds = new List<LatticeCell>();
            for (var y = 0; y < preset.LatticeHeight; y++)
            {
                for (var x = 0; x < preset.LatticeWidth; x++)
                {
                    var cell = new LatticeCell(x, y);
                    if (OpenSideCount(preset, floor, walkable, cell) == 1)
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

                    var wall = TileBetweenCells(floor, cell, beyond);
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

        static IReadOnlyList<StairLink> LinkFloors(StageRandom random, MazePreset preset)
        {
            var links = new List<StairLink>();
            if (preset.Floors < 2 || preset.Stairs < 1)
            {
                return links;
            }

            var cells = new List<LatticeCell>();
            for (var y = 0; y < preset.LatticeHeight; y++)
            {
                for (var x = 0; x < preset.LatticeWidth; x++)
                {
                    cells.Add(new LatticeCell(x, y));
                }
            }

            var chosen = new List<LatticeCell>();
            foreach (var cell in random.Shuffled(cells))
            {
                if (chosen.Count >= preset.Stairs)
                {
                    break;
                }

                if (FarEnoughFromAll(chosen, cell))
                {
                    chosen.Add(cell);
                }
            }

            for (var index = 0; index < chosen.Count; index++)
            {
                var lowerFloor = index % (preset.Floors - 1);
                links.Add(new StairLink(TileOfCell(lowerFloor, chosen[index])));
            }

            links.Sort(CompareStairs);
            return links;
        }

        static bool FarEnoughFromAll(IReadOnlyList<LatticeCell> chosen, LatticeCell cell)
        {
            foreach (var other in chosen)
            {
                if (Math.Abs(other.X - cell.X) + Math.Abs(other.Y - cell.Y) < 3)
                {
                    return false;
                }
            }

            return true;
        }

        static int OpenSideCount(MazePreset preset, int floor, HashSet<TilePosition> walkable, LatticeCell cell)
        {
            var open = 0;
            foreach (var step in Steps)
            {
                var beyond = cell.Shifted(step);
                if (InsideLattice(preset, beyond) && walkable.Contains(TileBetweenCells(floor, cell, beyond)))
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

        static TilePosition TileOfCell(int floor, LatticeCell cell)
        {
            return new TilePosition(floor, 2 * cell.X, 2 * cell.Y);
        }

        static TilePosition TileBetweenCells(int floor, LatticeCell from, LatticeCell to)
        {
            return new TilePosition(floor, from.X + to.X, from.Y + to.Y);
        }

        static int CompareStairs(StairLink left, StairLink right)
        {
            return left.Lower.CompareTo(right.Lower);
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
