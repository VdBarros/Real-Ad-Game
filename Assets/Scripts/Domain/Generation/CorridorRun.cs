using System.Collections.Generic;

namespace Game.Domain
{
    sealed class CorridorRun
    {
        CorridorRun(int lowTile, int highTile, IReadOnlyList<int> path)
        {
            LowTile = lowTile;
            HighTile = highTile;
            Path = path;
        }

        public int LowTile { get; }

        public int HighTile { get; }

        public IReadOnlyList<int> Path { get; }

        public bool IsSelfLoop
        {
            get { return LowTile == HighTile; }
        }

        public static CorridorRun Canonical(int oneEnd, int otherEnd, IReadOnlyList<int> path)
        {
            var reversed = new List<int>(path);
            reversed.Reverse();

            if (oneEnd > otherEnd)
            {
                return new CorridorRun(otherEnd, oneEnd, reversed);
            }

            if (oneEnd == otherEnd && ComparePaths(reversed, path) < 0)
            {
                return new CorridorRun(oneEnd, otherEnd, reversed);
            }

            return new CorridorRun(oneEnd, otherEnd, new List<int>(path));
        }

        public static int Compare(CorridorRun left, CorridorRun right)
        {
            if (left.LowTile != right.LowTile)
            {
                return left.LowTile < right.LowTile ? -1 : 1;
            }

            if (left.HighTile != right.HighTile)
            {
                return left.HighTile < right.HighTile ? -1 : 1;
            }

            return ComparePaths(left.Path, right.Path);
        }

        public static bool JoinTheSamePair(CorridorRun left, CorridorRun right)
        {
            return left.LowTile == right.LowTile && left.HighTile == right.HighTile;
        }

        static int ComparePaths(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            var shared = left.Count < right.Count ? left.Count : right.Count;
            for (var index = 0; index < shared; index++)
            {
                if (left[index] != right[index])
                {
                    return left[index] < right[index] ? -1 : 1;
                }
            }

            if (left.Count == right.Count)
            {
                return 0;
            }

            return left.Count < right.Count ? -1 : 1;
        }
    }
}
