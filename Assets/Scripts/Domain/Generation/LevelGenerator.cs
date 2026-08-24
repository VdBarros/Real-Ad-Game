using System;

namespace Game.Domain
{
    public static class LevelGenerator
    {
        public const int MaximumAttempts = MazeLayoutGenerator.MaximumAttempts;

        const int LayoutReasonCount = (int)LayoutRejection.TooFewOffPathSlots + 1;

        const int ContentReasonCount = (int)ContentRejection.PanelStalled + 1;

        public static PlacedLevel Generate(long seed, MazePreset preset)
        {
            LevelGenerationReport report;
            return Generate(seed, preset, out report);
        }

        public static PlacedLevel Generate(long seed, MazePreset preset, out LevelGenerationReport report)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            return Generate(seed, preset, ContentRecipe.For(preset), PowerTuning.For(preset), out report);
        }

        public static PlacedLevel Generate(
            long seed,
            MazePreset preset,
            ContentRecipe recipe,
            PowerTuning tuning,
            out LevelGenerationReport report)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            var layoutCounts = new int[LayoutReasonCount];
            var contentCounts = new int[ContentReasonCount];

            for (var attempt = 0; attempt < MaximumAttempts; attempt++)
            {
                PlacedLevel level;
                LayoutRejection layoutRejection;
                ContentRejection contentRejection;
                var attemptSeed = MazeLayoutGenerator.SeedOfAttempt(seed, attempt);

                if (TryGenerate(attemptSeed, preset, recipe, tuning, out level, out layoutRejection, out contentRejection))
                {
                    report = new LevelGenerationReport(preset, attempt + 1, layoutCounts, contentCounts);
                    return level;
                }

                if (layoutRejection != LayoutRejection.None)
                {
                    layoutCounts[(int)layoutRejection]++;
                }
                else
                {
                    contentCounts[(int)contentRejection]++;
                }
            }

            throw new LevelGenerationException(
                new LevelGenerationReport(preset, MaximumAttempts, layoutCounts, contentCounts));
        }

        public static bool TryGenerate(
            long attemptSeed,
            MazePreset preset,
            ContentRecipe recipe,
            PowerTuning tuning,
            out PlacedLevel level,
            out LayoutRejection layoutRejection,
            out ContentRejection contentRejection)
        {
            level = null;
            contentRejection = ContentRejection.None;

            MazeLayout layout;
            if (!MazeLayoutGenerator.TryGenerate(attemptSeed, preset, out layout, out layoutRejection))
            {
                return false;
            }

            return ContentPlacer.TryPlace(layout, recipe, tuning, out level, out contentRejection);
        }
    }
}
