using System;

namespace Game.Domain
{
    public sealed class ContentRecipe
    {
        public static readonly ContentRecipe Tiny = new ContentRecipe(3, 5, 2);

        public static readonly ContentRecipe Ship = new ContentRecipe(2, 14, 7);

        public static readonly ContentRecipe Stress = new ContentRecipe(4, 56, 29);

        public ContentRecipe(int multipliers, int enemies, int additives)
        {
            RequireCount(multipliers, nameof(multipliers));
            RequireCount(additives, nameof(additives));

            if (enemies < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemies), enemies, "A level with nothing to fight has no floor rule to honour.");
            }

            Multipliers = multipliers;
            Enemies = enemies;
            Additives = additives;
        }

        public int Bosses
        {
            get { return 1; }
        }

        public int Multipliers { get; }

        public int Enemies { get; }

        public int Additives { get; }

        public int Slots
        {
            get { return Bosses + Multipliers + Enemies + Additives; }
        }

        public static ContentRecipe For(MazePreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (preset.Name == MazePreset.Tiny.Name)
            {
                return Tiny;
            }

            if (preset.Name == MazePreset.Ship.Name)
            {
                return Ship;
            }

            if (preset.Name == MazePreset.Stress.Name)
            {
                return Stress;
            }

            throw new ArgumentException(
                "No content recipe is filed for the preset named " + preset.Name + ".", nameof(preset));
        }

        public override string ToString()
        {
            return Slots + " slots: 1 boss, " + Multipliers + " multipliers, "
                + Enemies + " enemies, " + Additives + " additives";
        }

        static void RequireCount(int value, string parameter)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameter, value, "A recipe counts at least none.");
            }
        }
    }
}
