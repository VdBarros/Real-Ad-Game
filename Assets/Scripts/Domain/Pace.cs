using System;

namespace Game.Domain
{
    public static class Pace
    {
        public const float StepsPerSecond = 4f;

        public const double DeadWalkBudgetSeconds = 2.0;

        public static int DeadWalkBudgetSteps
        {
            get { return StepsIn(DeadWalkBudgetSeconds); }
        }

        public static int StepsIn(double seconds)
        {
            if (seconds < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "A walk only ever takes a stretch of time forwards.");
            }

            return (int)Math.Floor(seconds * StepsPerSecond);
        }

        public static double SecondsOf(int steps)
        {
            if (steps < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steps), steps, "A walk is counted in steps taken, never in steps untaken.");
            }

            return steps / (double)StepsPerSecond;
        }
    }
}
