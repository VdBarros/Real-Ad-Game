using System;
using System.Globalization;

namespace Game.Presentation.Pure
{
    public readonly struct GameCycle : IEquatable<GameCycle>
    {
        readonly GamePhase phase;
        readonly int levelNumber;
        readonly int finalPower;

        GameCycle(GamePhase phase, int levelNumber, int finalPower)
        {
            this.phase = phase;
            this.levelNumber = levelNumber;
            this.finalPower = finalPower;
        }

        public static GameCycle Booting
        {
            get { return default(GameCycle); }
        }

        public GamePhase Phase
        {
            get { return phase; }
        }

        public int LevelNumber
        {
            get { return levelNumber; }
        }

        public int FinalPower
        {
            get { return finalPower; }
        }

        public GameCycle Watching()
        {
            if (phase != GamePhase.Boot)
            {
                throw Refuses("The cutscene opens the game and nothing else");
            }

            return new GameCycle(GamePhase.Cutscene, levelNumber, finalPower);
        }

        public GameCycle Generating()
        {
            if (phase != GamePhase.Cutscene && phase != GamePhase.Result)
            {
                throw Refuses("A level is generated after the cutscene or after a result");
            }

            return new GameCycle(GamePhase.Generating, levelNumber + 1, 0);
        }

        public GameCycle Previewing()
        {
            if (phase != GamePhase.Generating)
            {
                throw Refuses("The fly-through is over a level that has just been generated");
            }

            return new GameCycle(GamePhase.Preview, levelNumber, finalPower);
        }

        public GameCycle Playing()
        {
            if (phase != GamePhase.Preview)
            {
                throw Refuses("Play begins where the fly-through lands");
            }

            return new GameCycle(GamePhase.Play, levelNumber, finalPower);
        }

        public GameCycle Finished(int power)
        {
            if (phase != GamePhase.Play)
            {
                throw Refuses("A result is what beating the boss leaves behind");
            }

            if (power < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(power), power, "A run that beat the boss ended holding power.");
            }

            return new GameCycle(GamePhase.Result, levelNumber, power);
        }

        InvalidOperationException Refuses(string rule)
        {
            return new InvalidOperationException(rule + ", and the cycle sits in " + phase + ".");
        }

        public bool Equals(GameCycle other)
        {
            return phase == other.phase
                && levelNumber == other.levelNumber
                && finalPower == other.finalPower;
        }

        public override bool Equals(object obj)
        {
            return obj is GameCycle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)phase;
                hash = (hash * 397) ^ levelNumber;
                hash = (hash * 397) ^ finalPower;
                return hash;
            }
        }

        public override string ToString()
        {
            var described = phase.ToString();
            if (levelNumber > 0)
            {
                described += " of level " + levelNumber.ToString(CultureInfo.InvariantCulture);
            }

            if (phase == GamePhase.Result)
            {
                described += " at power " + finalPower.ToString(CultureInfo.InvariantCulture);
            }

            return described;
        }
    }
}
