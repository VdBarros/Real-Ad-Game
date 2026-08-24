using System;

namespace Game.Domain
{
    public sealed class MazeGenerationException : Exception
    {
        public MazeGenerationException(MazeGenerationReport report)
            : base("No layout survived generation — " + report + ".")
        {
            Report = report;
        }

        public MazeGenerationReport Report { get; }

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
    }
}
