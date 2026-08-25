using System;

namespace Game.Domain
{
    public sealed class LevelSupply
    {
        readonly long openingSeed;
        readonly MazePreset preset;

        public LevelSupply(long openingSeed, MazePreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            this.openingSeed = openingSeed;
            this.preset = preset;
        }

        public long OpeningSeed
        {
            get { return openingSeed; }
        }

        public MazePreset Preset
        {
            get { return preset; }
        }

        public int LevelsDrawn { get; private set; }

        public int SeedsSpent { get; private set; }

        public int RetriesAbsorbed { get; private set; }

        public LevelGenerationReport LastReport { get; private set; }

        public int DrawsFailed
        {
            get { return SeedsSpent - LevelsDrawn; }
        }

        public long SeedOf(int levelNumber)
        {
            if (levelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelNumber), levelNumber, "Levels are counted from the first one.");
            }

            return Scattered(openingSeed, levelNumber);
        }

        public PlacedLevel Draw()
        {
            SeedsSpent++;

            LevelGenerationReport report;
            var level = LevelGenerator.Generate(SeedOf(SeedsSpent), preset, out report);

            LevelsDrawn++;
            LastReport = report;
            RetriesAbsorbed += report.Attempts - 1;
            return level;
        }

        public static long Scattered(long openingSeed, int levelNumber)
        {
            unchecked
            {
                var mixed = (ulong)openingSeed + (ulong)levelNumber * 0x9E3779B97F4A7C15UL;
                mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL;
                mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL;
                return (long)(mixed ^ (mixed >> 31));
            }
        }
    }
}
