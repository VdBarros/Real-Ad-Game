using System.Collections.Generic;
using System.Globalization;

namespace Game.Domain
{
    public sealed class StageRandom
    {
        uint state;

        StageRandom(uint state)
        {
            this.state = state;
        }

        public static StageRandom ForStage(long seed, string stage)
        {
            return new StageRandom(Hash(seed.ToString(CultureInfo.InvariantCulture) + "|" + stage));
        }

        public double NextDouble()
        {
            unchecked
            {
                state += 0x6D2B79F5u;
                var mixed = state;
                mixed = (mixed ^ (mixed >> 15)) * (1u | mixed);
                mixed = (mixed + ((mixed ^ (mixed >> 7)) * (61u | mixed))) ^ mixed;
                return (mixed ^ (mixed >> 14)) / 4294967296.0;
            }
        }

        public int Next(int exclusiveBound)
        {
            return (int)(NextDouble() * exclusiveBound);
        }

        public T Pick<T>(IReadOnlyList<T> items)
        {
            return items[Next(items.Count)];
        }

        public List<T> Shuffled<T>(IReadOnlyList<T> items)
        {
            var shuffled = new List<T>(items);
            for (var index = shuffled.Count - 1; index > 0; index--)
            {
                var other = Next(index + 1);
                var held = shuffled[index];
                shuffled[index] = shuffled[other];
                shuffled[other] = held;
            }

            return shuffled;
        }

        static uint Hash(string text)
        {
            unchecked
            {
                var hash = 0x811c9dc5u;
                for (var index = 0; index < text.Length; index++)
                {
                    hash ^= text[index];
                    hash *= 0x01000193u;
                }

                return hash;
            }
        }
    }
}
