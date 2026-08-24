using System.Collections.Generic;

namespace Game.Domain
{
    static class CorridorExtractor
    {
        public static List<CorridorRun> Extract(IReadOnlyList<int[]> adjacency, bool[] isNode)
        {
            var runs = new List<CorridorRun>();

            for (var origin = 0; origin < isNode.Length; origin++)
            {
                if (!isNode[origin])
                {
                    continue;
                }

                foreach (var firstStep in adjacency[origin])
                {
                    var path = new List<int>();
                    var previous = origin;
                    var current = firstStep;

                    while (!isNode[current])
                    {
                        path.Add(current);
                        var onwards = StepPast(adjacency[current], previous);
                        if (onwards < 0)
                        {
                            break;
                        }

                        previous = current;
                        current = onwards;
                    }

                    if (!isNode[current])
                    {
                        continue;
                    }

                    runs.Add(CorridorRun.Canonical(origin, current, path));
                }
            }

            runs.Sort(CorridorRun.Compare);
            DropTheSecondSightingOfEachRun(runs);
            return runs;
        }

        public static CorridorRun FirstThatBreaksTheGraphModel(IReadOnlyList<CorridorRun> runs)
        {
            foreach (var run in runs)
            {
                if (run.IsLoop)
                {
                    return run;
                }
            }

            for (var index = 1; index < runs.Count; index++)
            {
                if (!CorridorRun.JoinTheSamePair(runs[index - 1], runs[index]))
                {
                    continue;
                }

                return runs[index - 1].Path.Count >= runs[index].Path.Count ? runs[index - 1] : runs[index];
            }

            return null;
        }

        static void DropTheSecondSightingOfEachRun(List<CorridorRun> runs)
        {
            var kept = 0;
            for (var index = 0; index < runs.Count; index++)
            {
                if (kept > 0 && CorridorRun.Compare(runs[kept - 1], runs[index]) == 0)
                {
                    continue;
                }

                runs[kept] = runs[index];
                kept++;
            }

            runs.RemoveRange(kept, runs.Count - kept);
        }

        static int StepPast(IReadOnlyList<int> neighbours, int previous)
        {
            foreach (var neighbour in neighbours)
            {
                if (neighbour != previous)
                {
                    return neighbour;
                }
            }

            return -1;
        }
    }
}
