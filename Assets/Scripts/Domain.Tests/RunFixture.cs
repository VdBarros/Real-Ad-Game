using System.Collections.Generic;

namespace Game.Domain.Tests
{
    static class RunFixture
    {
        public const long Seed = 20260824L;

        public const string Preset = "tiny";

        public const int Additive = 0;

        public const int AdditiveValue = 5;

        public const int GateEnemy = 1;

        public const int GateEnemyValue = 6;

        public const int Boss = 2;

        public const int BossValue = 30;

        public const int Multiplier = 3;

        public const int MultiplierValue = 3;

        public const int Start = 4;

        public const int DoorstepEnemy = 5;

        public const int DoorstepEnemyValue = 2;

        public const int AdditiveBeyondTheMultiplier = 6;

        public const int AdditiveBeyondTheMultiplierValue = 4;

        public static LevelGraph Level()
        {
            var builder = new LevelGraphBuilder(Seed, Preset);

            foreach (var x in new[] { 3, 4, 5, 6, 7 })
            {
                builder.AddTile(At(x, 0), regionId: 1);
            }

            builder.AddTile(At(3, 1), regionId: 0);

            foreach (var x in new[] { 1, 2, 3, 4, 5 })
            {
                builder.AddTile(At(x, 2), regionId: 0);
            }

            builder.AddTile(At(1, 3), regionId: 0);
            builder.AddTile(At(1, 4), regionId: 0);

            builder.AddNode(At(3, 0), NodeType.Additive, AdditiveValue);
            builder.AddNode(At(5, 0), NodeType.Enemy, GateEnemyValue);
            builder.AddNode(At(7, 0), NodeType.Boss, BossValue);
            builder.AddNode(At(1, 2), NodeType.Multiplier, MultiplierValue);
            builder.AddNode(At(3, 2), NodeType.Start);
            builder.AddNode(At(5, 2), NodeType.Enemy, DoorstepEnemyValue);
            builder.AddNode(At(1, 4), NodeType.Additive, AdditiveBeyondTheMultiplierValue);

            builder.Connect(At(1, 2), At(1, 4), Path(At(1, 3)));
            builder.Connect(At(3, 0), At(5, 0), Path(At(4, 0)));
            builder.Connect(At(5, 0), At(7, 0), Path(At(6, 0)));
            builder.Connect(At(3, 2), At(3, 0), Path(At(3, 1)));
            builder.Connect(At(3, 2), At(1, 2), Path(At(2, 2)));
            builder.Connect(At(3, 2), At(5, 2), Path(At(4, 2)));

            return builder.Build();
        }

        public static RunState Begin(int startingPower)
        {
            return RunState.Begin(Level(), startingPower);
        }

        static IReadOnlyList<TilePosition> Path(params TilePosition[] tiles)
        {
            return tiles;
        }

        static TilePosition At(int x, int y)
        {
            return new TilePosition(floor: 0, x: x, y: y);
        }
    }
}
