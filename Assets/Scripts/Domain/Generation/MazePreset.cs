using System;

namespace Game.Domain
{
    public sealed class MazePreset
    {
        public static readonly MazePreset Tiny = new MazePreset("tiny", 4, 3, 1, 2, 0.25, 0, 12, 8, 3);

        public static readonly MazePreset Ship = new MazePreset("ship", 5, 3, 2, 4, 0.25, 2, 24, 16, 8);

        public static readonly MazePreset Stress = new MazePreset("stress", 9, 7, 3, 9, 0.25, 4, 90, 40, 20);

        public MazePreset(
            string name,
            int latticeWidth,
            int latticeHeight,
            int floors,
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
            RequirePositive(floors, nameof(floors));
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

            if (floors > 1 && stairs < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stairs), stairs, "Floors above the ground are reachable only by stair.");
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
            Floors = floors;
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

        public int Floors { get; }

        public int Regions { get; }

        public double BraidFactor { get; }

        public int Stairs { get; }

        public int ContentSlots { get; }

        public int MinimumBossDepth { get; }

        public int MinimumOffPathSlots { get; }

        public int RegionsPerFloor
        {
            get { return Math.Max(1, (int)Math.Floor((double)Regions / Floors + 0.5)); }
        }

        public static MazePreset Named(string name)
        {
            if (name == Tiny.Name)
            {
                return Tiny;
            }

            if (name == Ship.Name)
            {
                return Ship;
            }

            if (name == Stress.Name)
            {
                return Stress;
            }

            throw new ArgumentException("\"" + name + "\" is not a preset.", nameof(name));
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
