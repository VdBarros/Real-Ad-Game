using System;

namespace Game.Domain
{
    public sealed class LevelGenerationException : Exception
    {
        public LevelGenerationException(LevelGenerationReport report)
            : base("No level survived generation — " + report + ".")
        {
            Report = report;
        }

        public LevelGenerationReport Report { get; }

        public MazePreset Preset
        {
            get { return Report.Preset; }
        }

        public int Attempts
        {
            get { return Report.Attempts; }
        }

        public int CountOf(LayoutRejection rejection)
        {
            return Report.CountOf(rejection);
        }

        public int CountOf(ContentRejection rejection)
        {
            return Report.CountOf(rejection);
        }
    }
}
