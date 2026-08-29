using System.Collections.Generic;

namespace Game.Domain.Tests
{
    sealed class LevelSketch
    {
        public const long Seed = 20250824L;

        public const string Preset = "tiny";

        readonly List<TilePosition> order = new List<TilePosition>();
        readonly Dictionary<TilePosition, NodeType> typeByPosition = new Dictionary<TilePosition, NodeType>();
        readonly Dictionary<TilePosition, int> valueByPosition = new Dictionary<TilePosition, int>();
        readonly List<TilePosition> joins = new List<TilePosition>();
        readonly List<List<TilePosition>> paths = new List<List<TilePosition>>();

        public static readonly PowerTuning Tuning = new PowerTuning(2, 200, 0.6, 0.2, 0.8, 0.8, 0.7, 0.0, 1.0, 1, false, 0);

        public const int StartNodeId = 0;

        public const int AdditiveNodeId = 1;

        public const int GateEnemyNodeId = 2;

        public const int MultiplierNodeId = 3;

        public const int DeepEnemyNodeId = 4;

        public const int BossNodeId = 5;

        public static LevelSketch Solvable()
        {
            return Branching(deepEnemy: 24, boss: 39);
        }

        public static LevelSketch StrandingOnlyTheEnemyFirstPolicy()
        {
            return Branching(deepEnemy: 45, boss: 56);
        }

        public static LevelSketch WalkingQuietOver(int tiles, int elevation = 0)
        {
            return new LevelSketch()
                .NodeAt(0, 0, NodeType.Start)
                .NodeAt(1, 0, NodeType.Additive, 20)
                .NodeAt(2, 0, NodeType.Enemy, 1)
                .NodeAt(3, 0, NodeType.Multiplier, 2)
                .NodeAt(4 + tiles, 0, NodeType.Enemy, 24)
                .NodeAt(0, 1, NodeType.Boss, 39)
                .Joined(0, 0, 1, 0)
                .Joined(1, 0, 2, 0)
                .Joined(2, 0, 3, 0)
                .Joined(0, 0, 0, 1)
                .Stretched(3, 4 + tiles, 0, elevation);
        }

        public static LevelSketch Branching(
            int additive = 20,
            int gateEnemy = 1,
            int multiplier = 2,
            int deepEnemy = 24,
            int boss = 39)
        {
            return new LevelSketch()
                .NodeAt(0, 0, NodeType.Start)
                .NodeAt(1, 0, NodeType.Additive, additive)
                .NodeAt(2, 0, NodeType.Enemy, gateEnemy)
                .NodeAt(3, 0, NodeType.Multiplier, multiplier)
                .NodeAt(4, 0, NodeType.Enemy, deepEnemy)
                .NodeAt(0, 1, NodeType.Boss, boss)
                .Joined(0, 0, 1, 0)
                .Joined(1, 0, 2, 0)
                .Joined(2, 0, 3, 0)
                .Joined(3, 0, 4, 0)
                .Joined(0, 0, 0, 1);
        }

        public LevelSketch NodeAt(int x, int y, NodeType type, int value = 0)
        {
            var position = At(x, y);
            if (!typeByPosition.ContainsKey(position))
            {
                order.Add(position);
            }

            typeByPosition[position] = type;
            valueByPosition[position] = value;
            return this;
        }

        public LevelSketch Joined(int firstX, int firstY, int secondX, int secondY)
        {
            joins.Add(At(firstX, firstY));
            joins.Add(At(secondX, secondY));
            paths.Add(new List<TilePosition>());
            return this;
        }

        public LevelSketch Stretched(int fromX, int toX, int y, int elevation = 0)
        {
            joins.Add(At(fromX, y));
            joins.Add(At(toX, y));

            var path = new List<TilePosition>();
            for (var x = fromX + 1; x < toX; x++)
            {
                path.Add(new TilePosition(elevation, x, y));
            }

            paths.Add(path);
            return this;
        }

        public LevelSketch Retyped(int x, int y, NodeType type)
        {
            return NodeAt(x, y, type, valueByPosition[At(x, y)]);
        }

        public LevelSketch WithoutCorridors()
        {
            joins.Clear();
            return this;
        }

        public LevelSketch Revalued(int x, int y, int value)
        {
            return NodeAt(x, y, typeByPosition[At(x, y)], value);
        }

        public LevelGraph Build()
        {
            var builder = new LevelGraphBuilder(Seed, Preset);

            foreach (var position in order)
            {
                builder.AddTile(position, regionId: position.Y);
                builder.AddNode(position, typeByPosition[position], valueByPosition[position]);
            }

            foreach (var path in paths)
            {
                foreach (var position in path)
                {
                    builder.AddTile(position, regionId: position.Y);
                }
            }

            for (var index = 0; index < joins.Count; index += 2)
            {
                builder.Connect(joins[index], joins[index + 1], paths[index / 2]);
            }

            return builder.Build();
        }

        static TilePosition At(int x, int y)
        {
            return new TilePosition(0, x, y);
        }
    }
}
