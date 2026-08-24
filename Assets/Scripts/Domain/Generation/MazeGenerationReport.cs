using System.Collections.Generic;
using System.Text;

namespace Game.Domain
{
    public sealed class MazeGenerationReport
    {
        readonly int[] countByRejection;

        public MazeGenerationReport(MazePreset preset, int attempts, IReadOnlyList<int> countByRejection)
        {
            Preset = preset;
            Attempts = attempts;
            this.countByRejection = new int[countByRejection.Count];
            for (var index = 0; index < countByRejection.Count; index++)
            {
                this.countByRejection[index] = countByRejection[index];
            }
        }

        public MazePreset Preset { get; }

        public int Attempts { get; }

        public int Rejections
        {
            get
            {
                var total = 0;
                foreach (var count in countByRejection)
                {
                    total += count;
                }

                return total;
            }
        }

        public int CountOf(LayoutRejection rejection)
        {
            var index = (int)rejection;
            return index < 0 || index >= countByRejection.Length ? 0 : countByRejection[index];
        }

        public override string ToString()
        {
            var description = new StringBuilder();
            description.Append(Preset.Name);
            description.Append(": ");
            description.Append(Attempts);
            description.Append(Attempts == 1 ? " attempt" : " attempts");

            for (var index = 0; index < countByRejection.Length; index++)
            {
                if (countByRejection[index] == 0)
                {
                    continue;
                }

                description.Append(", ");
                description.Append((LayoutRejection)index);
                description.Append('=');
                description.Append(countByRejection[index]);
            }

            return description.ToString();
        }
    }
}
