namespace Game.Domain.Tests
{
    static class LevelMutation
    {
        public static LevelGraph WithNodeInflated(LevelGraph level, int nodeId, int factor)
        {
            var builder = new LevelGraphBuilder(level.Seed, level.Preset);

            foreach (var tile in level.Tiles.Tiles)
            {
                builder.AddTile(tile.Position, tile.RegionId);
            }

            foreach (var stair in level.Tiles.Stairs)
            {
                builder.AddStair(stair.Lower, stair.Upper);
            }

            foreach (var node in level.Decisions.Nodes)
            {
                builder.AddNode(node.Position, node.Type, node.Id == nodeId ? node.Value * factor : node.Value);
            }

            foreach (var corridor in level.Decisions.Corridors)
            {
                builder.Connect(
                    level.Decisions.Node(corridor.LowNodeId).Position,
                    level.Decisions.Node(corridor.HighNodeId).Position,
                    corridor.TilePath);
            }

            return builder.Build();
        }
    }
}
