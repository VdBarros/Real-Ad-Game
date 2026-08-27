using System;

namespace Game.Domain
{
    public sealed class LevelPlan
    {
        public const int PlateauLevel = 13;

        public const int PlateauStartingPower = 16;

        public const double PlateauEliteFraction = 1.0;

        public const double PlateauSpreadFloor = 4.0;

        public const int PlateauOpeningChoices = 2;

        const int OpeningStartingPower = 2;

        const double OpeningEliteFraction = 0.0;

        const double OpeningSpreadFloor = 1.0;

        const int OpeningPlanChoices = 1;

        public LevelPlan(MazePreset preset, ContentRecipe recipe, PowerTuning tuning)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            Preset = preset;
            Recipe = recipe;
            Tuning = tuning;
        }

        public MazePreset Preset { get; }

        public ContentRecipe Recipe { get; }

        public PowerTuning Tuning { get; }

        public int StartingPower
        {
            get { return Tuning.StartingPower; }
        }

        public double EliteFraction
        {
            get { return Tuning.EliteFraction; }
        }

        public double SpreadFloor
        {
            get { return Tuning.SpreadFloor; }
        }

        public int OpeningChoices
        {
            get { return Tuning.OpeningChoices; }
        }

        public static LevelPlan For(int levelNumber)
        {
            return For(MazePreset.Ship, levelNumber);
        }

        public static LevelPlan For(MazePreset preset, int levelNumber)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            return new LevelPlan(
                preset,
                ContentRecipe.For(preset),
                PowerTuning.For(preset)
                    .Rebased(StartingPowerAt(levelNumber))
                    .Locking(EliteFractionAt(levelNumber))
                    .Routing(SpreadFloorAt(levelNumber))
                    .Opening(OpeningChoicesAt(levelNumber)));
        }

        public static double EliteFractionAt(int levelNumber)
        {
            RequireLevel(levelNumber);

            if (levelNumber >= PlateauLevel)
            {
                return PlateauEliteFraction;
            }

            var climb = PlateauEliteFraction - OpeningEliteFraction;
            var steps = PlateauLevel - 1;

            return OpeningEliteFraction + climb * (levelNumber - 1) / steps;
        }

        public static int OpeningChoicesAt(int levelNumber)
        {
            RequireLevel(levelNumber);

            return levelNumber == 1 ? OpeningPlanChoices : PlateauOpeningChoices;
        }

        public static double SpreadFloorAt(int levelNumber)
        {
            RequireLevel(levelNumber);

            if (levelNumber >= PlateauLevel)
            {
                return PlateauSpreadFloor;
            }

            var climb = PlateauSpreadFloor - OpeningSpreadFloor;
            var steps = PlateauLevel - 1;

            return OpeningSpreadFloor + climb * (levelNumber - 1) / steps;
        }

        public static int StartingPowerAt(int levelNumber)
        {
            RequireLevel(levelNumber);

            if (levelNumber >= PlateauLevel)
            {
                return PlateauStartingPower;
            }

            var climb = PlateauStartingPower - OpeningStartingPower;
            var steps = PlateauLevel - 1;

            return OpeningStartingPower + (climb * (levelNumber - 1) + steps / 2) / steps;
        }

        static void RequireLevel(int levelNumber)
        {
            if (levelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelNumber), levelNumber, "Levels are counted from the first one.");
            }
        }

        public override string ToString()
        {
            return Preset.Name + " opening on " + StartingPower + ", " + Recipe
                + ", " + (int)(EliteFraction * 100.0 + 0.5) + "% of off-Spine enemies minted rich";
        }
    }
}
