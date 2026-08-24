using System.Collections.Generic;

namespace Game.Domain
{
    sealed class CorridorRun
    {
        CorridorRun(int first, int second, IReadOnlyList<int> path)
        {
            First = first;
            Second = second;
            Path = path;
        }

        public int First { get; }

        public int Second { get; }

        public IReadOnlyList<int> Path { get; }

        public bool IsLoop
        {
            get { return First == Second; }
        }

        public static CorridorRun Canonical(int first, int second, IReadOnlyList<int> path)
        {
            var reversed = new List<int>(path);
            reversed.Reverse();

            if (first > second)
            {
                return new CorridorRun(second, first, reversed);
            }

            if (first == second && ComparePaths(reversed, path) < 0)
            {
                return new CorridorRun(first, second, reversed);
            }

            return new CorridorRun(first, second, new List<int>(path));
        }

        public static int Compare(CorridorRun left, CorridorRun right)
        {
            if (left.First != right.First)
            {
                return left.First < right.First ? -1 : 1;
            }

            if (left.Second != right.Second)
            {
                return left.Second < right.Second ? -1 : 1;
            }

            return ComparePaths(left.Path, right.Path);
        }

        public static bool JoinTheSamePair(CorridorRun left, CorridorRun right)
        {
            return left.First == right.First && left.Second == right.Second;
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
