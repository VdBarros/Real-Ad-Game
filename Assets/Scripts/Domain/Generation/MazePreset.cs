using System;

namespace Game.Domain
{
    public sealed class MazePreset
    {
        public static readonly MazePreset Tiny = new MazePreset("tiny", 4, 3, 1, 2, 0.25, 0, 11, 8, 3);

        public static readonly MazePreset Ship = new MazePreset("ship", 5, 3, 2, 4, 0.25, 2, 24, 16, 8);

        public static readonly MazePreset Stress = new MazePreset("stress", 9, 7, 3, 9, 0.25, 4, 90, 40, 20);

        static readonly MazePreset[] Filed = { Tiny, Ship, Stress };

        public MazePreset(
            string name,
            int latticeWidth,
            int latticeHeight,
            int terraces,
            int regions,
            double braidFactor,
            int stairs,
            int contentSlots,
            int minimumBossDepth,
            int minimumOffPathSlots)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            RequirePositive(latticeWidth, nameof(latticeWidth));
            RequirePositive(latticeHeight, nameof(latticeHeight));
            RequirePositive(terraces, nameof(terraces));
            RequirePositive(regions, nameof(regions));
            RequirePositive(contentSlots, nameof(contentSlots));

            if (braidFactor < 0.0 || braidFactor > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(braidFactor), braidFactor, "Braiding re-joins a fraction of the dead ends.");
            }

            if (stairs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stairs), stairs, "A preset cannot ask for fewer than no stairs.");
            }

            if (terraces > 1 && stairs < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stairs), stairs, "A terrace above the ground is reachable only by stair.");
            }

            if (minimumBossDepth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumBossDepth), minimumBossDepth, "Depth is a tile count.");
            }

            if (minimumOffPathSlots < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumOffPathSlots), minimumOffPathSlots, "Off-path slots are a slot count.");
            }

            Name = name;
            LatticeWidth = latticeWidth;
            LatticeHeight = latticeHeight;
            Terraces = terraces;
            Regions = regions;
            BraidFactor = braidFactor;
            Stairs = stairs;
            ContentSlots = contentSlots;
            MinimumBossDepth = minimumBossDepth;
            MinimumOffPathSlots = minimumOffPathSlots;
        }

        public string Name { get; }

        public int LatticeWidth { get; }

        public int LatticeHeight { get; }

        public int Terraces { get; }

        public int Regions { get; }

        public double BraidFactor { get; }

        public int Stairs { get; }

        public int ContentSlots { get; }

        public int MinimumBossDepth { get; }

        public int MinimumOffPathSlots { get; }

        public int TerraceOffset
        {
            get { return 2 * LatticeHeight; }
        }

        public int RegionsPerTerrace
        {
            get { return Math.Max(1, (int)Math.Floor((double)Regions / Terraces + 0.5)); }
        }

        public static MazePreset Named(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            foreach (var preset in Filed)
            {
                if (string.Equals(preset.Name, name, StringComparison.Ordinal))
                {
                    return preset;
                }
            }

            throw new ArgumentException("No preset is filed under the name " + name + ".", nameof(name));
        }

        public override string ToString()
        {
            return Name;
        }

        static void RequirePositive(int value, string parameter)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(parameter, value, "A preset counts at least one.");
            }
        }
    }
}
