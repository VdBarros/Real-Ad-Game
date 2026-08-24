namespace Game.Domain
{
    public sealed class LayoutMetrics
    {
        public LayoutMetrics(
            int tileCount,
            int nodeCount,
            int slotCount,
            int emptyCount,
            int corridorCount,
            int gateCount,
            int pocketCount,
            int bossDepth,
            int offPathSlotCount)
        {
            TileCount = tileCount;
            NodeCount = nodeCount;
            SlotCount = slotCount;
            EmptyCount = emptyCount;
            CorridorCount = corridorCount;
            GateCount = gateCount;
            PocketCount = pocketCount;
            BossDepth = bossDepth;
            OffPathSlotCount = offPathSlotCount;
        }

        public int TileCount { get; }

        public int NodeCount { get; }

        public int SlotCount { get; }

        public int EmptyCount { get; }

        public int CorridorCount { get; }

        public int GateCount { get; }

        public int PocketCount { get; }

        public int BossDepth { get; }

        public int OffPathSlotCount { get; }

        public double GateRatio
        {
            get { return SlotCount == 0 ? 0.0 : (double)GateCount / SlotCount; }
        }
    }
}
