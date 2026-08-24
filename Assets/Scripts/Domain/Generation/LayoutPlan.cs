namespace Game.Domain
{
    sealed class LayoutPlan
    {
        readonly int[] regionOfTile;
        readonly bool[] nodeTiles;
        readonly bool[] slotTiles;

        public LayoutPlan(TileTopology topology, int startTile)
        {
            Topology = topology;
            StartTile = startTile;
            regionOfTile = new int[topology.Count];
            nodeTiles = new bool[topology.Count];
            slotTiles = new bool[topology.Count];

            for (var tile = 0; tile < topology.Count; tile++)
            {
                regionOfTile[tile] = -1;
                nodeTiles[tile] = topology.Degree(tile) != 2 || topology.Stairs[tile] || tile == startTile;
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
