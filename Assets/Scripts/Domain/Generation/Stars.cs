using System;

namespace Game.Domain
{
    public static class Stars
    {
        public const int Fewest = 1;

        public const int Most = 3;

        public static int For(Par par, int finalPower, int levelNumber)
        {
            if (par == null)
            {
                throw new ArgumentNullException(nameof(par));
            }

            var position = par.PositionOf(finalPower);

            if (position >= LevelPlan.ThirdStarAt(levelNumber))
            {
                return Most;
            }

            return position >= LevelPlan.SecondStarAt(levelNumber) ? Fewest + 1 : Fewest;
        }
    }
}
