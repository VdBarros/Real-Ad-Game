using System.Collections.Generic;
using System.Text;

namespace Game.Domain
{
    public sealed class LevelGenerationReport
    {
        readonly int[] countByLayoutRejection;
        readonly int[] countByContentRejection;

        public LevelGenerationReport(
            MazePreset preset,
            int attempts,
            IReadOnlyList<int> countByLayoutRejection,
            IReadOnlyList<int> countByContentRejection)
        {
            Preset = preset;
            Attempts = attempts;
            this.countByLayoutRejection = Copied(countByLayoutRejection);
            this.countByContentRejection = Copied(countByContentRejection);
        }

        public MazePreset Preset { get; }

        public int Attempts { get; }

        public int Rejections
        {
            get { return Total(countByLayoutRejection) + Total(countByContentRejection); }
        }

        public int LayoutRejections
        {
            get { return Total(countByLayoutRejection); }
        }

        public int ContentRejections
        {
            get { return Total(countByContentRejection); }
        }

        public int CountOf(LayoutRejection rejection)
        {
            return At(countByLayoutRejection, (int)rejection);
        }

        public int CountOf(ContentRejection rejection)
        {
            return At(countByContentRejection, (int)rejection);
        }

        public override string ToString()
        {
            var description = new StringBuilder();
            description.Append(Preset.Name);
            description.Append(": ");
            description.Append(Attempts);
            description.Append(Attempts == 1 ? " attempt" : " attempts");

            for (var index = 1; index < countByLayoutRejection.Length; index++)
            {
                Append(description, ((LayoutRejection)index).ToString(), countByLayoutRejection[index]);
            }

            for (var index = 1; index < countByContentRejection.Length; index++)
            {
                Append(description, ((ContentRejection)index).ToString(), countByContentRejection[index]);
            }

            return description.ToString();
        }

        static void Append(StringBuilder description, string reason, int count)
        {
            if (count == 0)
            {
                return;
            }

            description.Append(", ");
            description.Append(reason);
            description.Append('=');
            description.Append(count);
        }

        static int[] Copied(IReadOnlyList<int> counts)
        {
            var copy = new int[counts.Count];
            for (var index = 0; index < counts.Count; index++)
            {
                copy[index] = counts[index];
            }

            return copy;
        }

        static int Total(int[] counts)
        {
            var total = 0;
            foreach (var count in counts)
            {
                total += count;
            }

            return total;
        }

        static int At(int[] counts, int index)
        {
            return index < 0 || index >= counts.Length ? 0 : counts[index];
        }
    }
}
