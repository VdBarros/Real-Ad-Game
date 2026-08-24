using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Domain
{
    public sealed class MazeGenerationException : Exception
    {
        readonly int[] countByRejection;

        public MazeGenerationException(MazePreset preset, int attempts, IReadOnlyList<int> countByRejection)
            : base(Describe(preset, attempts, countByRejection))
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

        public int CountOf(LayoutRejection rejection)
        {
            var index = (int)rejection;
            return index < 0 || index >= countByRejection.Length ? 0 : countByRejection[index];
        }

        static string Describe(MazePreset preset, int attempts, IReadOnlyList<int> countByRejection)
        {
            var message = new StringBuilder();
            message.Append("Layout for preset ");
            message.Append(preset.Name);
            message.Append(" was rejected on all ");
            message.Append(attempts);
            message.Append(" attempts:");

            for (var index = 0; index < countByRejection.Count; index++)
            {
                if (countByRejection[index] == 0)
                {
                    continue;
                }

                message.Append(' ');
                message.Append((LayoutRejection)index);
                message.Append('=');
                message.Append(countByRejection[index]);
            }

            message.Append('.');
            return message.ToString();
        }
    }
}
