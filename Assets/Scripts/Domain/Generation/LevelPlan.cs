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

        public const int PlateauAdditiveDrift = 3;

        public const int PlateauOffPathDemand = 4;

        public const int ThirdMultiplierLevel = 11;

        const int OpeningStartingPower = 2;

        const double OpeningEliteFraction = 0.0;

        const double OpeningSpreadFloor = 1.0;

        const int OpeningPlanChoices = 1;

        const int OpeningAdditiveDrift = 0;

        const int OpeningOffPathDemand = 0;

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

        public bool PickupsAskForADetour
        {
            get { return Tuning.PickupsAskForADetour; }
        }

        public int MinimumOffPathSlots
        {
            get { return Preset.MinimumOffPathSlots + Tuning.OffPathDemand; }
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
                RecipeAt(preset, levelNumber),
                PowerTuning.For(preset)
                    .Rebased(StartingPowerAt(levelNumber))
                    .Locking(EliteFractionAt(levelNumber))
                    .Routing(SpreadFloorAt(levelNumber))
                    .Opening(OpeningChoicesAt(levelNumber))
                    .Detouring(levelNumber > 1)
                    .Demanding(OffPathDemandAt(levelNumber)));
        }

        public static ContentRecipe RecipeAt(MazePreset preset, int levelNumber)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            RequireLevel(levelNumber);

            var opening = ContentRecipe.For(preset);
            var thinner = Math.Min(AdditiveDriftAt(levelNumber), opening.Additives - 1);
            var extra = MultiplierDriftAt(levelNumber);

            if (thinner == 0 && extra == 0)
            {
                return opening;
            }

            return new ContentRecipe(
                opening.Multipliers + extra,
                opening.Enemies + thinner - extra,
                opening.Additives - thinner);
        }

        public static int AdditiveDriftAt(int levelNumber)
        {
            RequireLevel(levelNumber);

            if (levelNumber >= PlateauLevel)
            {
                return PlateauAdditiveDrift;
            }

            var climb = PlateauAdditiveDrift - OpeningAdditiveDrift;
            var steps = PlateauLevel - 1;

            return OpeningAdditiveDrift + (climb * (levelNumber - 1) + steps / 2) / steps;
        }

        public static int MultiplierDriftAt(int levelNumber)
        {
            RequireLevel(levelNumber);

            return levelNumber >= ThirdMultiplierLevel ? 1 : 0;
        }

        public static int OffPathDemandAt(int levelNumber)
        {
            RequireLevel(levelNumber);

            if (levelNumber >= PlateauLevel)
            {
                return PlateauOffPathDemand;
            }

            var climb = PlateauOffPathDemand - OpeningOffPathDemand;
            var steps = PlateauLevel - 1;

            return OpeningOffPathDemand + (climb * (levelNumber - 1) + steps / 2) / steps;
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
                + ", " + (int)(EliteFraction * 100.0 + 0.5) + "% of off-Spine enemies minted rich, "
                + MinimumOffPathSlots + " off-path slots"
                + (PickupsAskForADetour ? ", pickups on detours only" : ", pickups anywhere");
        }
    }
}
