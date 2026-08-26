using System.Collections.Generic;

namespace Game.Domain
{
    sealed class LayoutPlan
    {
        readonly int[] regionOfTile;
        readonly bool[] nodeTiles;
        readonly bool[] slotTiles;
        readonly bool[] staircaseTiles;

        public LayoutPlan(TileTopology topology, int startTile, IReadOnlyList<TilePosition> staircase)
        {
            Topology = topology;
            StartTile = startTile;
            regionOfTile = new int[topology.Count];
            nodeTiles = new bool[topology.Count];
            slotTiles = new bool[topology.Count];
            staircaseTiles = new bool[topology.Count];

            for (var tile = 0; tile < topology.Count; tile++)
            {
                regionOfTile[tile] = -1;
                nodeTiles[tile] = topology.Degree(tile) != 2 || tile == startTile;
            }

            foreach (var step in staircase)
            {
                staircaseTiles[topology.Of(step)] = true;
            }
        }

        public TileTopology Topology { get; }

        public int StartTile { get; }

        public bool[] NodeTiles
        {
            get { return nodeTiles; }
        }

        public int RegionOf(int tile)
        {
            return regionOfTile[tile];
        }

        public void PaintRegion(int tile, int regionId)
        {
            regionOfTile[tile] = regionId;
        }

        public bool IsNode(int tile)
        {
            return nodeTiles[tile];
        }

        public void Promote(int tile)
        {
            nodeTiles[tile] = true;
        }

        public bool IsStaircase(int tile)
        {
            return staircaseTiles[tile];
        }

        public bool IsSlot(int tile)
        {
            return slotTiles[tile];
        }

        public void MakeSlot(int tile)
        {
            slotTiles[tile] = true;
            nodeTiles[tile] = true;
        }

        public NodeType TypeOf(int tile)
        {
            if (tile == StartTile)
            {
                return NodeType.Start;
            }

            return slotTiles[tile] ? NodeType.Unassigned : NodeType.Empty;
        }
    }
}
